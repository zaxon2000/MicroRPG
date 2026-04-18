using System.Collections.Generic;
using Kilt.EasyRopes2D;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

/// <summary>
/// Player-driven bridge construction tool. Active only while
/// <see cref="ToolModeManager.ToolMode.Ropes"/> is selected.
///
/// Workflow:
/// 1. Right-click on empty space (raycast misses anything on
///    <see cref="RopeToolController.RopeInteractableLayerName"/>) → popup opens
///    at the cursor.
/// 2. Click "IndependentTack" or "Bridge Block" → popup hides, that prefab
///    becomes a ghost following the mouse.
/// 3. Left-click → place at cursor; first placement starts a new in-progress
///    Bridge GameObject, subsequent placements parent into the same Bridge.
///    Bridge blocks placed before completion are kinematic + position-frozen so
///    they hang in space until the bridge is finalized.
/// 4. Place the second IndependentTack (or click "Finish bridge") → all ropes
///    are wired between consecutive endpoints in a single pass, blocks are
///    unfrozen, and the bridge becomes a normal physics object.
///
/// "Cancel current bridge" destroys the in-progress hierarchy. ESC also closes
/// the popup or aborts a ghost preview.
///
/// Hierarchy produced (mirrors the existing 2_MainMembers_Bridge prefab):
///     Bridge_N
///     └── Container        (scale = <see cref="BridgeBuilderConfig.bridgeRootScale"/>)
///         ├── BlocksContainer  (tacks + blocks live here)
///         └── RopesContainer   (rope instances live here, wired on completion)
///
/// Auto-bootstraps after scene load — no scene placement required for the
/// builder itself, but the scene MUST contain a <see cref="BridgeBuilderConfig"/>
/// with the three prefab references assigned.
/// </summary>
public class BridgeBuilder : MonoBehaviour
{
    // ----- Singleton + bootstrap ----------------------------------------------
    private static BridgeBuilder _instance;
    public static BridgeBuilder Instance => _instance;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void Bootstrap()
    {
        if (_instance != null) return;
        var existing = FindFirstObjectByType<BridgeBuilder>();
        if (existing != null) { _instance = existing; return; }

        var go = new GameObject("BridgeBuilder");
        _instance = go.AddComponent<BridgeBuilder>();
        DontDestroyOnLoad(go);
    }

    private void Awake()
    {
        if (_instance != null && _instance != this) { Destroy(gameObject); return; }
        _instance = this;
    }

    // ----- State machine ------------------------------------------------------
    private enum State { Idle, PopupOpen, Ghost }
    private enum GhostKind { None, Tack, Block }

    private State _state = State.Idle;
    private bool _ropesModeActive;

    // Active in-progress bridge (null until the first prefab is placed).
    private GameObject _bridgeRoot;
    private Transform _blocksContainer;
    private Transform _ropesContainer;
    // Endpoint chain in placement order: TackA → block1 → block2 → … → TackB.
    // Each entry is the GameObject that will be wired as a rope's ObjectA/ObjectB.
    private readonly List<GameObject> _chain = new();
    private bool _chainStartedWithTack;
    private int _bridgeCounter;

    // Ghost preview (active only while State == Ghost).
    private GameObject _ghost;
    private GhostKind _ghostKind;
    // Original component snapshots so we can restore them on placement.
    private readonly List<(Behaviour comp, bool wasEnabled)> _ghostDisabledBehaviours = new();
    private readonly List<Collider2D> _ghostDisabledColliders = new();

    // Popup UI handles.
    private GameObject _popupGO;
    private Canvas _popupCanvas;
    private RectTransform _popupRT;

    // ----- Lifecycle ----------------------------------------------------------
    private void OnEnable()
    {
        ToolModeManager.OnModeChanged += HandleModeChanged;
    }

    private void OnDisable()
    {
        ToolModeManager.OnModeChanged -= HandleModeChanged;
    }

    private void Start()
    {
        // Sync to whatever the manager currently has — covers the case where the
        // builder spawns after the manager has already pushed its initial mode.
        if (ToolModeManager.Instance != null)
            HandleModeChanged(ToolModeManager.Instance.Mode);
    }

    private void HandleModeChanged(ToolModeManager.ToolMode mode)
    {
        bool nowActive = mode == ToolModeManager.ToolMode.Ropes;
        if (nowActive == _ropesModeActive) return;

        _ropesModeActive = nowActive;
        if (!nowActive)
        {
            // Leaving Ropes mode tears down transient UI/ghost so the player
            // can't accidentally finalize a bridge from another tool's input.
            ClosePopup();
            CancelGhost();
        }
    }

    // ----- Update: input dispatch --------------------------------------------
    private void Update()
    {
        if (!_ropesModeActive) return;

        // ESC always cancels the most-foreground action first.
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            if (_state == State.Ghost)        { CancelGhost(); return; }
            if (_state == State.PopupOpen)    { ClosePopup();  return; }
        }

        switch (_state)
        {
            case State.Idle:        UpdateIdle(); break;
            case State.PopupOpen:   /* popup is event-driven, no per-frame work */ break;
            case State.Ghost:       UpdateGhost(); break;
        }
    }

    // ----- Idle: wait for RMB on empty space ---------------------------------
    private void UpdateIdle()
    {
        if (!Input.GetMouseButtonDown(1)) return;
        if (IsPointerOverUI()) return; // don't open popup if RMB hits another UI element

        // RMB hit on a RopeInteractable belongs to MouseDragger — let it handle
        // the drag and stay out of its way. Only open the popup when the
        // raycast misses the rope-interactable layer.
        if (RaycastHitsRopeInteractable()) return;

        OpenPopupAtCursor();
    }

    // ----- Ghost: preview prefab follows cursor ------------------------------
    private void UpdateGhost()
    {
        if (_ghost == null) { _state = State.Idle; return; }

        Vector3 worldPos = MouseWorldPosition();

        // Bridge blocks are constrained to within maxBlockDistance of the
        // previous endpoint so the bridge rope can always reach. Tacks are
        // free-placed (they're typically anchors against scenery).
        if (_ghostKind == GhostKind.Block && _chain.Count > 0)
        {
            Vector3 anchor = _chain[_chain.Count - 1].transform.position;
            float maxDist = BridgeBuilderConfig.Instance != null
                ? BridgeBuilderConfig.Instance.maxBlockDistance
                : 0.6f;
            Vector3 delta = worldPos - anchor;
            if (delta.sqrMagnitude > maxDist * maxDist)
                worldPos = anchor + delta.normalized * maxDist;
        }
        worldPos.z = 0f;
        _ghost.transform.position = worldPos;

        // LMB confirms placement. We swallow the click by reading it before any
        // other input.GetMouseButton check this frame; nothing else listens for
        // LMB in Ropes mode except RopeCutterTrail, which works on the trail
        // collider (re-pressing LMB after placement starts a swipe-cut).
        if (Input.GetMouseButtonDown(0) && !IsPointerOverUI())
        {
            PlaceGhost();
        }
    }

    // ===========================================================================
    // Popup
    // ===========================================================================

    private void OpenPopupAtCursor()
    {
        if (_popupGO == null) BuildPopup();
        if (_popupGO == null) return; // BuildPopup logs its own warning on failure

        // Position the panel so its top-left corner sits at the mouse. The
        // RectTransform is anchored bottom-left of the canvas (0,0) so we just
        // assign anchoredPosition in screen units (ScreenSpaceOverlay = 1:1).
        _popupRT.pivot = new Vector2(0f, 1f);
        _popupRT.anchoredPosition = new Vector2(Input.mousePosition.x, Input.mousePosition.y);

        _popupGO.SetActive(true);
        _state = State.PopupOpen;
    }

    private void ClosePopup()
    {
        if (_popupGO != null) _popupGO.SetActive(false);
        if (_state == State.PopupOpen) _state = State.Idle;
    }

    private void BuildPopup()
    {
        var canvas = ResolveCanvas();
        if (canvas == null)
        {
            Debug.LogWarning("[BridgeBuilder] No Canvas available — popup not built.");
            return;
        }
        _popupCanvas = canvas;

        var size = BridgeBuilderConfig.Instance != null
            ? BridgeBuilderConfig.Instance.popupSize
            : new Vector2(220f, 220f);

        // Root panel ----------------------------------------------------------
        _popupGO = new GameObject("BridgeBuilderPopup",
            typeof(RectTransform), typeof(Image));
        _popupGO.transform.SetParent(canvas.transform, false);

        _popupRT = _popupGO.GetComponent<RectTransform>();
        _popupRT.anchorMin = new Vector2(0f, 0f);
        _popupRT.anchorMax = new Vector2(0f, 0f);
        _popupRT.pivot = new Vector2(0f, 1f);
        _popupRT.sizeDelta = size;

        var bg = _popupGO.GetComponent<Image>();
        bg.color = new Color(0f, 0f, 0f, 0.85f);
        bg.raycastTarget = true; // so clicks on the panel don't fall through to gameplay

        // Title bar (drag handle + label + X) --------------------------------
        const float titleH = 28f;
        var titleGO = new GameObject("TitleBar",
            typeof(RectTransform), typeof(Image), typeof(PopupDragHandle));
        titleGO.transform.SetParent(_popupGO.transform, false);
        var titleRT = titleGO.GetComponent<RectTransform>();
        titleRT.anchorMin = new Vector2(0f, 1f);
        titleRT.anchorMax = new Vector2(1f, 1f);
        titleRT.pivot = new Vector2(0.5f, 1f);
        titleRT.sizeDelta = new Vector2(0f, titleH);
        titleRT.anchoredPosition = Vector2.zero;
        titleGO.GetComponent<Image>().color = new Color(0.18f, 0.18f, 0.22f, 1f);
        titleGO.GetComponent<PopupDragHandle>().Target = _popupRT;

        var titleTextGO = new GameObject("TitleText", typeof(RectTransform), typeof(Text));
        titleTextGO.transform.SetParent(titleGO.transform, false);
        var ttRT = titleTextGO.GetComponent<RectTransform>();
        ttRT.anchorMin = Vector2.zero; ttRT.anchorMax = Vector2.one;
        ttRT.offsetMin = new Vector2(8f, 0f);
        ttRT.offsetMax = new Vector2(-32f, 0f);
        var titleText = titleTextGO.GetComponent<Text>();
        titleText.text = "Bridge Builder";
        titleText.alignment = TextAnchor.MiddleLeft;
        titleText.color = Color.white;
        titleText.font = LegacyFont();
        titleText.fontSize = 14;
        titleText.raycastTarget = false;

        CreateButton(titleGO.transform, "CloseX", "X",
            new Vector2(1f, 0.5f), new Vector2(1f, 0.5f),
            new Vector2(-2f, 0f), new Vector2(28f, titleH - 4f),
            ClosePopup);

        // Action buttons (vertical stack under the title bar) -----------------
        const float pad = 8f;
        const float btnH = 32f;
        const float btnSpacing = 6f;
        float topOffset = titleH + pad;

        CreateActionButton("BtnTack",   "IndependentTack",      topOffset + 0 * (btnH + btnSpacing), btnH, pad, () => StartGhost(GhostKind.Tack));
        CreateActionButton("BtnBlock",  "Bridge Block",         topOffset + 1 * (btnH + btnSpacing), btnH, pad, () => StartGhost(GhostKind.Block));
        CreateActionButton("BtnFinish", "Finish bridge",        topOffset + 2 * (btnH + btnSpacing), btnH, pad, FinishBridge);
        CreateActionButton("BtnCancel", "Cancel current bridge", topOffset + 3 * (btnH + btnSpacing), btnH, pad, CancelBridge);

        _popupGO.SetActive(false);
    }

    private void CreateActionButton(string name, string label, float topOffset, float h, float pad, System.Action onClick)
    {
        // Anchored to top-stretched: full panel width minus padding, fixed height,
        // positioned by topOffset measured from the top edge.
        CreateButton(_popupGO.transform, name, label,
            new Vector2(0f, 1f), new Vector2(1f, 1f),
            new Vector2(0f, -topOffset), new Vector2(-(2 * pad), h),
            onClick,
            offsetMode: ButtonOffsetMode.TopStretched, pad: pad);
    }

    private enum ButtonOffsetMode { Manual, TopStretched }

    private Button CreateButton(Transform parent, string name, string label,
                                Vector2 anchorMin, Vector2 anchorMax,
                                Vector2 anchoredPos, Vector2 sizeDelta,
                                System.Action onClick,
                                ButtonOffsetMode offsetMode = ButtonOffsetMode.Manual,
                                float pad = 0f)
    {
        var go = new GameObject(name, typeof(RectTransform), typeof(Image), typeof(Button));
        go.transform.SetParent(parent, false);

        var rt = go.GetComponent<RectTransform>();
        rt.anchorMin = anchorMin;
        rt.anchorMax = anchorMax;

        if (offsetMode == ButtonOffsetMode.TopStretched)
        {
            // Stretch horizontally with padding, fixed height, positioned from top.
            rt.pivot = new Vector2(0.5f, 1f);
            rt.offsetMin = new Vector2(pad, 0f);
            rt.offsetMax = new Vector2(-pad, 0f);
            rt.sizeDelta = new Vector2(rt.sizeDelta.x, sizeDelta.y);
            rt.anchoredPosition = anchoredPos;
        }
        else
        {
            rt.pivot = new Vector2(1f, 0.5f);
            rt.sizeDelta = sizeDelta;
            rt.anchoredPosition = anchoredPos;
        }

        var img = go.GetComponent<Image>();
        img.color = new Color(0.28f, 0.28f, 0.32f, 1f);

        var btn = go.GetComponent<Button>();
        btn.targetGraphic = img;
        btn.onClick.AddListener(() => onClick?.Invoke());

        var labelGO = new GameObject("Label", typeof(RectTransform), typeof(Text));
        labelGO.transform.SetParent(go.transform, false);
        var lrt = labelGO.GetComponent<RectTransform>();
        lrt.anchorMin = Vector2.zero; lrt.anchorMax = Vector2.one;
        lrt.offsetMin = Vector2.zero; lrt.offsetMax = Vector2.zero;
        var txt = labelGO.GetComponent<Text>();
        txt.text = label;
        txt.alignment = TextAnchor.MiddleCenter;
        txt.color = Color.white;
        txt.font = LegacyFont();
        txt.fontSize = 13;
        txt.raycastTarget = false;
        return btn;
    }

    private static Font LegacyFont()
    {
        // LegacyRuntime.ttf ships with Unity 2022+ and replaces the old Arial
        // dynamic font reference. Falls back to the OS default if missing.
        var f = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        return f != null ? f : Font.CreateDynamicFontFromOSFont("Arial", 14);
    }

    private static Canvas ResolveCanvas()
    {
        var named = GameObject.Find("PlayerUICanvas");
        if (named != null)
        {
            var c = named.GetComponent<Canvas>();
            if (c != null) return c;
        }
        return FindFirstObjectByType<Canvas>();
    }

    // ===========================================================================
    // Ghost preview
    // ===========================================================================

    private void StartGhost(GhostKind kind)
    {
        var cfg = BridgeBuilderConfig.Instance;
        if (cfg == null)
        {
            Debug.LogWarning("[BridgeBuilder] No BridgeBuilderConfig in scene — " +
                             "drop one and assign the three prefabs.");
            ClosePopup();
            return;
        }

        GameObject prefab = kind == GhostKind.Tack ? cfg.independentTackPrefab : cfg.bridgeBlockPrefab;
        if (prefab == null)
        {
            Debug.LogWarning($"[BridgeBuilder] {kind} prefab not assigned on BridgeBuilderConfig.");
            ClosePopup();
            return;
        }

        // Hide the popup but keep it built — re-opening just toggles SetActive.
        ClosePopup();

        // Make sure the bridge root exists before we instantiate the ghost so
        // we can immediately preview the correct world-scale (ghost lives under
        // BlocksContainer, inheriting the bridge root's scale).
        EnsureBridgeRoot();

        Transform parent = _blocksContainer;
        _ghost = Instantiate(prefab, parent);
        _ghost.name = prefab.name + "_Ghost";
        _ghost.transform.position = MouseWorldPosition();

        // Disable physics + gameplay scripts so the ghost doesn't fall, collide,
        // or react to clicks while it's tracking the cursor. We snapshot what
        // we touched so PlaceGhost can faithfully restore it.
        SnapshotAndDisableForGhost(_ghost);

        // Translucent tint hints "this is a preview." The IndependentTack has
        // a child SpriteRenderer too; tint everything under the root.
        ApplyGhostTint(_ghost, new Color(1f, 1f, 1f, 0.45f));

        _ghostKind = kind;
        _state = State.Ghost;
    }

    private void SnapshotAndDisableForGhost(GameObject root)
    {
        _ghostDisabledBehaviours.Clear();
        _ghostDisabledColliders.Clear();

        // Disable ALL Behaviours except renderers so gameplay scripts (Tack,
        // DamageableBlock, etc.) don't run during preview. Renderers stay live
        // so the ghost is still visible. We re-enable on placement.
        foreach (var b in root.GetComponentsInChildren<Behaviour>(true))
        {
            if (b == null) continue;
            // SpriteRenderer is a Renderer (not a Behaviour), so it's untouched.
            // We need to keep nothing else live — even RB2D is a Component, not Behaviour.
            _ghostDisabledBehaviours.Add((b, b.enabled));
            b.enabled = false;
        }
        foreach (var c in root.GetComponentsInChildren<Collider2D>(true))
        {
            if (c == null || !c.enabled) continue;
            _ghostDisabledColliders.Add(c);
            c.enabled = false;
        }
        // Force any rigidbody to kinematic + zero velocity for the duration
        // of the preview. Restored on placement.
        foreach (var rb in root.GetComponentsInChildren<Rigidbody2D>(true))
        {
            rb.bodyType = RigidbodyType2D.Kinematic;
            rb.linearVelocity = Vector2.zero;
            rb.angularVelocity = 0f;
        }
    }

    private static void ApplyGhostTint(GameObject root, Color tint)
    {
        foreach (var sr in root.GetComponentsInChildren<SpriteRenderer>(true))
            sr.color = tint;
    }

    private void CancelGhost()
    {
        if (_ghost != null)
        {
            Destroy(_ghost);
            _ghost = null;
        }
        _ghostKind = GhostKind.None;
        _ghostDisabledBehaviours.Clear();
        _ghostDisabledColliders.Clear();
        if (_state == State.Ghost) _state = State.Idle;
    }

    private void PlaceGhost()
    {
        if (_ghost == null) { _state = State.Idle; return; }

        // Restore opacity, re-enable scripts/colliders, then convert to the
        // appropriate physics state for the lifecycle stage we're in.
        ApplyGhostTint(_ghost, Color.white);
        foreach (var (comp, wasEnabled) in _ghostDisabledBehaviours)
            if (comp != null) comp.enabled = wasEnabled;
        foreach (var col in _ghostDisabledColliders)
            if (col != null) col.enabled = true;
        _ghostDisabledBehaviours.Clear();
        _ghostDisabledColliders.Clear();

        // Bridge blocks placed before completion stay frozen (kinematic + all
        // constraints) so the player can build the chain in mid-air. Tacks
        // remain in their prefab default (Tack.IsKinematic = true) — they are
        // anchor points by definition.
        if (_ghostKind == GhostKind.Block)
        {
            FreezeBlockUntilFinalized(_ghost);
        }

        // Strip the "_Ghost" suffix so the placed object reads cleanly in the
        // hierarchy panel.
        _ghost.name = _ghost.name.Replace("_Ghost", string.Empty);

        // Track endpoint in the chain. The first placement establishes whether
        // the chain starts with a tack — required by FinishBridge to decide
        // whether to also seal the far end.
        if (_chain.Count == 0)
            _chainStartedWithTack = (_ghostKind == GhostKind.Tack);

        _chain.Add(_ghost);

        // If this placement is the second tack (chain started with tack AND
        // current placement is a tack AND we have at least one block in
        // between), the bridge auto-finalizes per the user's spec.
        bool autoFinish = _ghostKind == GhostKind.Tack
                          && _chainStartedWithTack
                          && _chain.Count >= 3; // tackA + ≥1 block + tackB

        _ghost = null;
        _ghostKind = GhostKind.None;
        _state = State.Idle;

        if (autoFinish)
            FinalizeBridge();
    }

    private static void FreezeBlockUntilFinalized(GameObject block)
    {
        foreach (var rb in block.GetComponentsInChildren<Rigidbody2D>(true))
        {
            rb.bodyType = RigidbodyType2D.Kinematic;
            rb.linearVelocity = Vector2.zero;
            rb.angularVelocity = 0f;
            rb.constraints = RigidbodyConstraints2D.FreezeAll;
        }
    }

    // ===========================================================================
    // Bridge lifecycle
    // ===========================================================================

    private void EnsureBridgeRoot()
    {
        if (_bridgeRoot != null) return;

        var cfg = BridgeBuilderConfig.Instance;
        float scale = cfg != null ? cfg.bridgeRootScale : 0.0025f;

        _bridgeRoot = new GameObject($"Bridge_{++_bridgeCounter}");
        _bridgeRoot.transform.localScale = new Vector3(scale, scale, 1f);

        var container = new GameObject("Container").transform;
        container.SetParent(_bridgeRoot.transform, false);

        _blocksContainer = new GameObject("BlocksContainer").transform;
        _blocksContainer.SetParent(container, false);

        _ropesContainer = new GameObject("RopesContainer").transform;
        _ropesContainer.SetParent(container, false);

        _chain.Clear();
        _chainStartedWithTack = false;
    }

    private void CancelBridge()
    {
        // Drop the popup and any in-flight ghost first so we don't leave
        // dangling preview objects after the root is destroyed.
        CancelGhost();
        ClosePopup();

        if (_bridgeRoot != null)
        {
            Destroy(_bridgeRoot);
            _bridgeRoot = null;
        }
        _blocksContainer = null;
        _ropesContainer = null;
        _chain.Clear();
        _chainStartedWithTack = false;
    }

    private void FinishBridge()
    {
        ClosePopup();
        // Need at least 2 endpoints to wire any ropes at all. A tack alone or
        // a block alone is meaningless and we silently no-op.
        if (_chain.Count < 2)
        {
            Debug.Log("[BridgeBuilder] Finish requested but chain has < 2 endpoints — nothing to wire.");
            return;
        }
        FinalizeBridge();
    }

    private void FinalizeBridge()
    {
        var cfg = BridgeBuilderConfig.Instance;
        if (cfg == null || cfg.bridgeRopePrefab == null)
        {
            Debug.LogWarning("[BridgeBuilder] No bridge rope prefab assigned — cannot wire bridge.");
            return;
        }

        // Wire one rope between every consecutive pair: chain[i] → chain[i+1].
        // Each rope is parented under RopesContainer so the hierarchy mirrors
        // the 2_MainMembers_Bridge prefab's layout.
        for (int i = 0; i < _chain.Count - 1; i++)
        {
            var a = _chain[i];
            var b = _chain[i + 1];
            if (a == null || b == null) continue;

            var ropeGO = Instantiate(cfg.bridgeRopePrefab, _ropesContainer);
            ropeGO.name = $"Rope_{i}";

            var rope = ropeGO.GetComponent<Rope2D>();
            if (rope == null)
            {
                Debug.LogWarning($"[BridgeBuilder] Rope prefab is missing a Rope2D component.");
                Destroy(ropeGO);
                continue;
            }
            // Setting both endpoints flags NeedUpdateRope; the next
            // Rope2D.Update tick will run CreateRope() and spawn the nodes.
            rope.ObjectA = a;
            rope.ObjectB = b;
        }

        // Unfreeze all blocks so they can swing on their newly-attached ropes.
        // Tacks stay kinematic — they're the bridge's anchor points.
        foreach (var endpoint in _chain)
        {
            if (endpoint == null) continue;
            // Tacks have a Tack component; bridge blocks don't. Use that as the
            // discriminator instead of tracking placement type per-entry.
            if (endpoint.GetComponent<Tack>() != null) continue;
            UnfreezeBlock(endpoint);
        }

        // Reset for the next bridge — keep the previous bridge root in the
        // scene as a finished structure.
        _bridgeRoot = null;
        _blocksContainer = null;
        _ropesContainer = null;
        _chain.Clear();
        _chainStartedWithTack = false;
    }

    private static void UnfreezeBlock(GameObject block)
    {
        foreach (var rb in block.GetComponentsInChildren<Rigidbody2D>(true))
        {
            rb.constraints = RigidbodyConstraints2D.None;
            rb.bodyType = RigidbodyType2D.Dynamic;
        }
    }

    // ===========================================================================
    // Helpers
    // ===========================================================================

    private static Vector3 MouseWorldPosition()
    {
        var cam = Camera.main;
        if (cam == null) cam = FindFirstObjectByType<Camera>();
        if (cam == null) return Vector3.zero;

        // For an orthographic 2D camera, we just need the mouse's z to equal
        // the distance from the camera to the z=0 plane.
        Vector3 screen = Input.mousePosition;
        screen.z = -cam.transform.position.z;
        Vector3 world = cam.ScreenToWorldPoint(screen);
        world.z = 0f;
        return world;
    }

    private static bool RaycastHitsRopeInteractable()
    {
        int layerIdx = LayerMask.NameToLayer(RopeToolController.RopeInteractableLayerName);
        if (layerIdx < 0) return false; // layer missing → fall through to popup
        int mask = 1 << layerIdx;

        Vector3 worldPos = MouseWorldPosition();
        var hit = Physics2D.OverlapPoint(worldPos, mask);
        return hit != null;
    }

    private static bool IsPointerOverUI()
    {
        var es = EventSystem.current;
        return es != null && es.IsPointerOverGameObject();
    }

    // ===========================================================================
    // Nested helper: drag-the-popup-by-its-titlebar handler
    // ===========================================================================

    /// <summary>
    /// Tiny IDragHandler that moves a target RectTransform by the pointer delta.
    /// Attached to the popup's title bar so the player can drag the window.
    /// </summary>
    private sealed class PopupDragHandle : MonoBehaviour, IDragHandler
    {
        public RectTransform Target;

        public void OnDrag(PointerEventData e)
        {
            if (Target == null) return;
            Target.anchoredPosition += e.delta;
        }
    }
}
