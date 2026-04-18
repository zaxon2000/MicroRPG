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
/// 1. Right-click on empty space (raycast misses everything on
///    <see cref="RopeToolController.RopeInteractableLayerName"/>) → a small
///    popup opens at the cursor with a single "IndependentTack" action and an
///    "X" close button.
/// 2. Click "IndependentTack" → popup hides; an
///    <see cref="BridgeBuilderConfig.independentTackPrefab"/> ghost arms and
///    starts following the cursor.
/// 3. Left-click → place TackA. The ghost re-arms IMMEDIATELY for TackB
///    (no popup, no menu — the player just keeps clicking).
/// 4. Left-click again → place TackB. The bridge auto-completes:
///    intermediate blocks are spawned evenly along the line A→B
///    (count derived from distance ÷ <see cref="BridgeBuilderConfig.maxBlockDistance"/>),
///    and ropes are wired between every consecutive pair so the chain reads
///    TackA → block₁ → block₂ → … → blockₙ → TackB. Each runtime rope has
///    its break/spring properties overridden from
///    <see cref="BridgeBuilderConfig"/> so the bridge doesn't snap or sag
///    excessively under its own weight.
/// 5. ESC at any time cancels the in-progress bridge (popup + ghost +
///    any placed TackA). RMB on a RopeInteractable still goes to MouseDragger
///    — the builder only intercepts RMB on EMPTY space.
///
/// Hierarchy produced (mirrors the existing 2_MainMembers_Bridge prefab):
///     Bridge_N
///     └── Container        (scale = <see cref="BridgeBuilderConfig.bridgeRootScale"/>)
///         ├── BlocksContainer  (TackA, intermediate blocks, TackB)
///         └── RopesContainer   (rope instances wired in CompleteBridge)
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
    // PopupOpen → user picks the tack action; GhostA/GhostB drive the
    // cursor-follow placement. PopupOpen only appears for the first tack of a
    // new bridge — after TackA is placed, we re-arm GhostB directly.
    private enum State { Idle, PopupOpen, GhostA, GhostB }

    private State _state = State.Idle;
    private bool _ropesModeActive;

    // In-progress bridge (null until the first tack ghost is armed).
    private GameObject _bridgeRoot;
    private Transform _blocksContainer;
    private Transform _ropesContainer;
    private GameObject _placedTackA;
    private int _bridgeCounter;

    // Ghost preview (active only while State == GhostA/GhostB).
    private GameObject _ghost;
    private readonly List<(Behaviour comp, bool wasEnabled)> _ghostDisabledBehaviours = new();
    private readonly List<Collider2D> _ghostDisabledColliders = new();

    // Popup UI handles (built lazily on first use, then SetActive-toggled).
    private GameObject _popupGO;
    private RectTransform _popupRT;

    // ----- Lifecycle ----------------------------------------------------------
    private void OnEnable()  { ToolModeManager.OnModeChanged += HandleModeChanged; }
    private void OnDisable() { ToolModeManager.OnModeChanged -= HandleModeChanged; }

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
        // Leaving Ropes mode mid-build wipes any in-progress hierarchy so the
        // player can't accidentally complete a bridge from another tool's input.
        if (!nowActive) AbortInProgress();
    }

    // ----- Update: input dispatch --------------------------------------------
    private void Update()
    {
        if (!_ropesModeActive) return;

        // ESC cancels whatever is in flight (popup + ghost + bridge root).
        if (Input.GetKeyDown(KeyCode.Escape) && _state != State.Idle)
        {
            AbortInProgress();
            return;
        }

        switch (_state)
        {
            case State.Idle:        UpdateIdle(); break;
            case State.PopupOpen:   /* event-driven (button onClick) */ break;
            case State.GhostA:
            case State.GhostB:      UpdateGhost(); break;
        }
    }

    private void UpdateIdle()
    {
        if (!Input.GetMouseButtonDown(1)) return;
        if (IsPointerOverUI()) return; // RMB on the toolbar/canvas shouldn't pop the menu

        // RMB on a RopeInteractable belongs to MouseDragger — let it drag and
        // stay out of its way. Only open the popup on EMPTY space.
        if (RaycastHitsRopeInteractable()) return;

        OpenPopupAtCursor();
    }

    private void UpdateGhost()
    {
        if (_ghost == null)
        {
            // Ghost was destroyed externally (e.g. mode change race) — bail.
            _state = State.Idle;
            return;
        }

        Vector3 worldPos = MouseWorldPosition();
        worldPos.z = 0f;
        _ghost.transform.position = worldPos;

        if (Input.GetMouseButtonDown(0) && !IsPointerOverUI())
            PlaceTack();
    }

    // ===========================================================================
    // Popup UI
    // ===========================================================================

    private void OpenPopupAtCursor()
    {
        if (_popupGO == null) BuildPopup();
        if (_popupGO == null) return; // BuildPopup logged its own warning

        // Anchor at bottom-left of canvas, pivot at top-left of popup, so
        // anchoredPosition == screen-space cursor places the popup with its
        // top-left corner at the click point (ScreenSpaceOverlay = 1:1).
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

        // Compact panel: title bar (28h) + one full-width button (32h) + padding.
        Vector2 size = new Vector2(180f, 80f);

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
        bg.raycastTarget = true; // panel clicks shouldn't fall through to gameplay

        // Title bar -----------------------------------------------------------
        const float titleH = 24f;
        var titleGO = new GameObject("TitleBar", typeof(RectTransform), typeof(Image));
        titleGO.transform.SetParent(_popupGO.transform, false);
        var titleRT = titleGO.GetComponent<RectTransform>();
        titleRT.anchorMin = new Vector2(0f, 1f);
        titleRT.anchorMax = new Vector2(1f, 1f);
        titleRT.pivot = new Vector2(0.5f, 1f);
        titleRT.sizeDelta = new Vector2(0f, titleH);
        titleRT.anchoredPosition = Vector2.zero;
        titleGO.GetComponent<Image>().color = new Color(0.18f, 0.18f, 0.22f, 1f);

        var titleTextGO = new GameObject("TitleText", typeof(RectTransform), typeof(Text));
        titleTextGO.transform.SetParent(titleGO.transform, false);
        var ttRT = titleTextGO.GetComponent<RectTransform>();
        ttRT.anchorMin = Vector2.zero; ttRT.anchorMax = Vector2.one;
        ttRT.offsetMin = new Vector2(8f, 0f);
        ttRT.offsetMax = new Vector2(-28f, 0f);
        var titleText = titleTextGO.GetComponent<Text>();
        titleText.text = "Bridge";
        titleText.alignment = TextAnchor.MiddleLeft;
        titleText.color = Color.white;
        titleText.font = LegacyFont();
        titleText.fontSize = 13;
        titleText.raycastTarget = false;

        // X close button ------------------------------------------------------
        CreateButton(titleGO.transform, "CloseX", "X",
            new Vector2(1f, 0.5f), new Vector2(1f, 0.5f),
            new Vector2(-2f, 0f), new Vector2(24f, titleH - 4f),
            ClosePopup);

        // IndependentTack action ---------------------------------------------
        const float pad = 8f;
        const float btnH = 32f;
        var btn = CreateButton(_popupGO.transform, "BtnTack", "IndependentTack",
            new Vector2(0f, 1f), new Vector2(1f, 1f),
            new Vector2(0f, -(titleH + pad)), new Vector2(-(2 * pad), btnH),
            () => StartTackGhost(State.GhostA),
            stretchTopHorizontal: true, pad: pad);
        // No targetGraphic tweak needed — base CreateButton already wires it.
        _ = btn;

        _popupGO.SetActive(false);
    }

    private static Button CreateButton(Transform parent, string name, string label,
                                       Vector2 anchorMin, Vector2 anchorMax,
                                       Vector2 anchoredPos, Vector2 sizeDelta,
                                       System.Action onClick,
                                       bool stretchTopHorizontal = false,
                                       float pad = 0f)
    {
        var go = new GameObject(name, typeof(RectTransform), typeof(Image), typeof(Button));
        go.transform.SetParent(parent, false);

        var rt = go.GetComponent<RectTransform>();
        rt.anchorMin = anchorMin;
        rt.anchorMax = anchorMax;

        if (stretchTopHorizontal)
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
        // Bundled with Unity 2022+; replaces the old Arial dynamic font.
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

    private void StartTackGhost(State newState)
    {
        var cfg = BridgeBuilderConfig.Instance;
        if (cfg == null || cfg.independentTackPrefab == null)
        {
            Debug.LogWarning("[BridgeBuilder] BridgeBuilderConfig missing or " +
                             "independentTackPrefab not assigned — bridge build aborted.");
            AbortInProgress();
            return;
        }

        // The popup must hide before the ghost arms — otherwise the popup's
        // raycaster eats the LMB-place click. ClosePopup is a no-op when not open.
        ClosePopup();

        // Make sure the bridge root exists so the ghost previews under the
        // correct world-scale (BlocksContainer inherits the bridge root's scale).
        EnsureBridgeRoot();

        _ghost = Instantiate(cfg.independentTackPrefab, _blocksContainer);
        _ghost.name = cfg.independentTackPrefab.name + "_Ghost";
        _ghost.transform.position = MouseWorldPosition();

        // Disable physics + gameplay scripts so the ghost doesn't fall, collide,
        // or react to clicks while it's tracking the cursor.
        SnapshotAndDisableForGhost(_ghost);

        // Translucent tint signals "this is a preview."
        ApplyGhostTint(_ghost, new Color(1f, 1f, 1f, 0.45f));

        _state = newState;
    }

    private void PlaceTack()
    {
        if (_ghost == null) { _state = State.Idle; return; }

        // Restore appearance + behaviour — the ghost becomes a real tack.
        ApplyGhostTint(_ghost, Color.white);
        foreach (var (comp, wasEnabled) in _ghostDisabledBehaviours)
            if (comp != null) comp.enabled = wasEnabled;
        foreach (var col in _ghostDisabledColliders)
            if (col != null) col.enabled = true;
        _ghostDisabledBehaviours.Clear();
        _ghostDisabledColliders.Clear();

        // Strip the "_Ghost" suffix so the placed object reads cleanly.
        _ghost.name = _ghost.name.Replace("_Ghost", string.Empty);

        var placed = _ghost;
        _ghost = null;

        if (_state == State.GhostA)
        {
            // Stash TackA and immediately arm a ghost for TackB without any
            // popup/menu — the player just keeps clicking to place B.
            _placedTackA = placed;
            placed.name = placed.name + "_A";
            StartTackGhost(State.GhostB);
        }
        else // GhostB
        {
            placed.name = placed.name + "_B";
            _state = State.Idle;
            CompleteBridge(_placedTackA, placed);
        }
    }

    private void SnapshotAndDisableForGhost(GameObject root)
    {
        _ghostDisabledBehaviours.Clear();
        _ghostDisabledColliders.Clear();

        // Disable ALL Behaviours so gameplay scripts (Tack, etc.) don't run
        // during preview. SpriteRenderer is a Renderer, not a Behaviour, so
        // the ghost stays visible.
        foreach (var b in root.GetComponentsInChildren<Behaviour>(true))
        {
            if (b == null) continue;
            _ghostDisabledBehaviours.Add((b, b.enabled));
            b.enabled = false;
        }
        foreach (var c in root.GetComponentsInChildren<Collider2D>(true))
        {
            if (c == null || !c.enabled) continue;
            _ghostDisabledColliders.Add(c);
            c.enabled = false;
        }
        // Force any rigidbody to kinematic + zero velocity for the duration of
        // the preview. The Behaviour list above doesn't cover RB2D (it's a
        // Component, not a Behaviour), but kinematic sticks until something
        // flips it back — which CompleteBridge will via the rope joint chain.
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
    }

    private void AbortInProgress()
    {
        if (_ghost != null)        Destroy(_ghost);
        if (_bridgeRoot != null)   Destroy(_bridgeRoot);

        _ghost = null;
        _bridgeRoot = null;
        _blocksContainer = null;
        _ropesContainer = null;
        _placedTackA = null;
        _ghostDisabledBehaviours.Clear();
        _ghostDisabledColliders.Clear();

        ClosePopup();
        _state = State.Idle;
    }

    /// <summary>
    /// Spawns the intermediate blocks evenly along TackA→TackB, then wires
    /// ropes between every consecutive endpoint. Block count is derived from
    /// distance so longer spans get more blocks automatically — each rope is
    /// kept at most <see cref="BridgeBuilderConfig.maxBlockDistance"/> long.
    /// Each runtime rope has its break/spring properties tuned per
    /// <see cref="BridgeBuilderConfig"/> so the bridge doesn't snap or sag
    /// excessively under its own weight.
    /// </summary>
    private void CompleteBridge(GameObject tackA, GameObject tackB)
    {
        var cfg = BridgeBuilderConfig.Instance;
        if (cfg == null || cfg.bridgeBlockPrefab == null || cfg.bridgeRopePrefab == null)
        {
            Debug.LogWarning("[BridgeBuilder] Missing block/rope prefab on " +
                             "BridgeBuilderConfig — bridge left without intermediate blocks.");
            // Fall through and at least connect A↔B with whatever we have so
            // the user sees something rather than a silent failure.
        }

        Vector3 a = tackA.transform.position;
        Vector3 b = tackB.transform.position;
        float distance = Vector3.Distance(a, b);

        // Number of segments such that each rope is at most maxBlockDistance.
        // Minimum 2 segments (= 1 intermediate block) so the player gets the
        // "rope+blocks" visual even for very short spans.
        float maxDist = cfg != null ? cfg.maxBlockDistance : 0.8f;
        int segments = Mathf.Max(2, Mathf.CeilToInt(distance / Mathf.Max(0.05f, maxDist)));

        var chain = new List<GameObject> { tackA };

        if (cfg != null && cfg.bridgeBlockPrefab != null)
        {
            for (int i = 1; i < segments; i++)
            {
                float t = (float)i / segments;
                Vector3 pos = Vector3.Lerp(a, b, t);

                var block = Instantiate(cfg.bridgeBlockPrefab, _blocksContainer);
                block.name = $"{cfg.bridgeBlockPrefab.name}_{i}";
                block.transform.position = pos;
                chain.Add(block);
            }
        }
        chain.Add(tackB);

        // Wire ropes between every consecutive pair. Setting ObjectA/ObjectB
        // flags Rope2D._needUpdateRope; the next Rope2D.Update tick rebuilds
        // the rope's nodes, joints, and renderers from scratch.
        if (cfg != null && cfg.bridgeRopePrefab != null)
        {
            for (int i = 0; i < chain.Count - 1; i++)
            {
                var ropeGO = Instantiate(cfg.bridgeRopePrefab, _ropesContainer);
                ropeGO.name = $"Rope_{i}";

                var rope = ropeGO.GetComponent<Rope2D>();
                if (rope == null)
                {
                    Debug.LogWarning("[BridgeBuilder] Rope prefab missing Rope2D — skipping segment.");
                    Destroy(ropeGO);
                    continue;
                }
                ApplyRopeTunings(rope, cfg);
                rope.ObjectA = chain[i];
                rope.ObjectB = chain[i + 1];
            }
        }

        // Hand off the finished bridge — keep it parented in the scene as a
        // standalone structure and clear our build-state references so the
        // next RMB starts a fresh bridge.
        _bridgeRoot = null;
        _blocksContainer = null;
        _ropesContainer = null;
        _placedTackA = null;
    }

    /// <summary>
    /// Apply the BridgeBuilderConfig's per-rope physics overrides BEFORE wiring
    /// ObjectA/ObjectB. Setting these later still works (they flip
    /// _needUpdateRope), but applying first means the rope is built with the
    /// final values from the start instead of being rebuilt.
    /// </summary>
    private static void ApplyRopeTunings(Rope2D rope, BridgeBuilderConfig cfg)
    {
        if (rope == null || cfg == null) return;

        rope.RopeCanBreak = cfg.ropeCanBreak;

        // CRITICAL for bridge stability. The prefab's SpringJoint2Ds have a
        // target distance equal to the rope's natural per-link length (~0.26
        // world units at our scale). When the actual block-to-block spacing is
        // shorter than that, the springs PUSH the blocks apart — N stiff springs
        // pushing simultaneously is enough to fragment Wood blocks on frame 1
        // (they have their own damage/fragment system at m_maxLife: 4.9).
        //
        // MaxDistanceOnlyMode = true converts every per-node joint to a
        // DistanceJoint2D with maxDistanceOnly=true: chain-link behaviour that
        // ONLY resists stretching, never pushes when compressed. This keeps
        // bridges intact at any maxBlockDistance, at the cost of more sag for
        // very short segment lengths (no spring force to lift slack out).
        rope.MaxDistanceOnlyMode = cfg.ropeMaxDistanceOnly;

        // ropeSpringFrequency = 0 means "leave the prefab value unchanged."
        // Anything > 0 overrides. Higher = stiffer (less sag). Has no effect
        // when MaxDistanceOnlyMode is true, since joints are then DistanceJoint2D.
        if (cfg.ropeSpringFrequency > 0f)
            rope.SpringFrequency = cfg.ropeSpringFrequency;

        // ropeSpringDamping < 0 (we use the slider's lower bound -0.01) means
        // "leave the prefab value unchanged." Anything in [0, 1] overrides.
        // Same caveat: ignored when MaxDistanceOnlyMode is true.
        if (cfg.ropeSpringDamping >= 0f)
            rope.SpringDampingValue = cfg.ropeSpringDamping;
    }

    // ===========================================================================
    // Helpers
    // ===========================================================================

    private static Vector3 MouseWorldPosition()
    {
        var cam = Camera.main;
        if (cam == null) cam = FindFirstObjectByType<Camera>();
        if (cam == null) return Vector3.zero;

        // For an orthographic 2D camera, mouse z is the camera-to-z=0 distance.
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
        return Physics2D.OverlapPoint(worldPos, mask) != null;
    }

    private static bool IsPointerOverUI()
    {
        var es = EventSystem.current;
        return es != null && es.IsPointerOverGameObject();
    }
}
