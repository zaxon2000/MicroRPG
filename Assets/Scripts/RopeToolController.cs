using UnityEngine;
using Kilt.EasyRopes2D;
using Kilt.EasyRopes2D.Examples;

/// <summary>
/// Bridges <see cref="ToolModeManager"/> with the Easy Ropes 2D interaction
/// systems (<see cref="MouseDragger"/> for right-click drag, <see cref="RopeCutterTrail"/>
/// for left-swipe cutting). Both are gated on <see cref="ToolModeManager.ToolMode.Ropes"/>
/// so they only activate when the player has selected the "Ropes" tool.
///
/// Easy Ropes 2D's whole input chain depends on a <see cref="CameraInputController"/>
/// component being attached to a Camera (it owns <c>currentTouch</c> via raycasts).
/// The MicroRPG game scene's camera doesn't ship with one, so we attach it (and
/// <see cref="ClickEffectController"/>) at runtime to whichever Camera is active.
/// This avoids forcing the user to swap in the demo's <c>0_Cameras</c> prefab,
/// which is preconfigured for the demo's tiny world and breaks the gameplay view.
///
/// MouseDragger and RopeCutterTrail are both auto-instantiated if missing — the
/// cutter is built from scratch (TrailRenderer + trigger collider) using
/// Sprites/Default so we don't need any prefab/material from the Easy Ropes 2D
/// package present in the scene.
///
/// Auto-bootstrapped — no scene placement required.
/// </summary>
public class RopeToolController : MonoBehaviour
{
    private static RopeToolController _instance;

    private MouseDragger _mouseDragger;
    private CameraInputController _cameraInput;
    private ClickEffectController _clickEffect;
    private RopeCutterTrail _cutter;

    // Name of the dedicated layer for rope-tool targets (cuttable ropes,
    // draggable boxes, etc.). Defined in ProjectSettings/TagManager.asset.
    // Anything NOT on this layer is invisible to the rope tool's raycast,
    // which keeps RMB-drag from accidentally grabbing tilemaps or scenery.
    public const string RopeInteractableLayerName = "RopeInteractable";

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void Bootstrap()
    {
        if (_instance != null) return;
        var existing = FindFirstObjectByType<RopeToolController>();
        if (existing != null)
        {
            _instance = existing;
            return;
        }
        var go = new GameObject("RopeToolController");
        _instance = go.AddComponent<RopeToolController>();
        DontDestroyOnLoad(go);
    }

    private void Awake()
    {
        if (_instance != null && _instance != this)
        {
            Destroy(gameObject);
            return;
        }
        _instance = this;
    }

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
        EnsureCameraInputDriver();
        EnsureMouseDragger();
        EnsureCutter();
        // Sync to whatever the manager currently has — covers the case where the
        // controller spawns after the manager has already pushed its initial state.
        if (ToolModeManager.Instance != null)
            HandleModeChanged(ToolModeManager.Instance.Mode);
        else
            ApplyMode(ToolModeManager.ToolMode.None);
    }

    private void HandleModeChanged(ToolModeManager.ToolMode mode)
    {
        ApplyMode(mode);
    }

    private void ApplyMode(ToolModeManager.ToolMode mode)
    {
        bool ropesActive = mode == ToolModeManager.ToolMode.Ropes;

        // Driver components — keep CameraInputController/ClickEffectController
        // alive across mode changes so currentTouch is populated the moment we
        // flip back into Ropes mode. They're idle-cheap when nothing listens.
        if (_cameraInput == null || _clickEffect == null)
            EnsureCameraInputDriver();

        // Right-click drag --------------------------------------------------
        if (_mouseDragger == null)
            EnsureMouseDragger();
        if (_mouseDragger != null)
            _mouseDragger.enabled = ropesActive;

        // Left-swipe cut ----------------------------------------------------
        // Make sure a cutter exists, then toggle it (plus any others in scene).
        if (_cutter == null)
            EnsureCutter();
        var cutters = FindObjectsByType<RopeCutterTrail>(
            FindObjectsInactive.Include, FindObjectsSortMode.None);
        foreach (var cutter in cutters)
        {
            if (cutter == null) continue;
            cutter.enabled = ropesActive;
            // Disabling the script alone leaves the trail collider live; deactivate
            // the GameObject so the trail visual disappears too when not in use.
            if (cutter.gameObject != gameObject)
                cutter.gameObject.SetActive(ropesActive);
        }
    }

    private void EnsureMouseDragger()
    {
        _mouseDragger = MouseDragger.Instance;
        if (_mouseDragger != null) return;

        // No dragger in the scene — host one on a dedicated GameObject so we
        // don't clutter this controller with the dragger's serialized fields.
        var dragGO = new GameObject("MouseDragger");
        DontDestroyOnLoad(dragGO);
        _mouseDragger = dragGO.AddComponent<MouseDragger>();
        // MouseDragger defaults its trigger button to right-click via its own
        // SerializeField; nothing else needed here.
    }

    /// <summary>
    /// The rope cutter is what converts an LMB swipe into actual rope cuts.
    /// The Easy Ropes 2D <c>RopeCutterTrail</c> prefab isn't in the Game scene,
    /// so we synthesize one at runtime: a TrailRenderer for the visible swipe
    /// streak, a small CircleCollider2D trigger so dragging across a rope's
    /// node collider also cuts it, and the cutter script itself (which uses
    /// per-frame linecasts to catch fast swipes that skip past the collider).
    /// </summary>
    private void EnsureCutter()
    {
        if (_cutter == null)
            _cutter = FindFirstObjectByType<RopeCutterTrail>(FindObjectsInactive.Include);
        if (_cutter != null) return;

        var go = new GameObject("RopeCutterTrail");
        DontDestroyOnLoad(go);

        // TrailRenderer: thin white streak that fades quickly. Sprites/Default
        // is bundled with Unity and works in URP without a custom shader graph.
        var trail = go.AddComponent<TrailRenderer>();
        trail.time = 0.15f;
        trail.startWidth = 0.08f;
        trail.endWidth = 0.0f;
        trail.minVertexDistance = 0.05f;
        trail.autodestruct = false;
        trail.emitting = true;
        var trailShader = Shader.Find("Sprites/Default");
        if (trailShader != null)
            trail.material = new Material(trailShader);
        trail.startColor = new Color(1f, 1f, 1f, 1f);
        trail.endColor = new Color(1f, 1f, 1f, 0f);

        // Trigger collider so OnTriggerEnter2D also cuts ropes the trail
        // physically intersects (complements the linecast path). The cutter
        // script enables/disables this collider via its Cutting setter.
        var col = go.AddComponent<CircleCollider2D>();
        col.isTrigger = true;
        col.radius = 0.05f;
        col.enabled = false;

        _cutter = go.AddComponent<RopeCutterTrail>();
        // CollisionMask defaults to -1 (all layers) which is what we want —
        // rope nodes can live on Default or RopeInteractable and still get
        // cut. The cutter only acts on objects that actually have Node2D.
    }

    /// <summary>
    /// Easy Ropes 2D's input pipeline (<see cref="CameraInputController"/> →
    /// <see cref="ClickEffectController"/> → MouseDragger / RopeCutterTrail) needs
    /// at least one CameraInputController bolted onto a Camera in the scene to
    /// drive <c>currentTouch</c>. The MicroRPG game camera ships without one,
    /// so we add it here. Both components are cheap when nothing listens, so we
    /// leave them attached even outside Ropes mode.
    /// </summary>
    private void EnsureCameraInputDriver()
    {
        // CameraInputController already in scene? Reuse it.
        if (_cameraInput == null)
            _cameraInput = FindFirstObjectByType<CameraInputController>(FindObjectsInactive.Include);

        if (_cameraInput == null)
        {
            var cam = PickInputCamera();
            if (cam == null)
            {
                Debug.LogWarning("[RopeToolController] No active Camera found — " +
                                 "rope click/drag/cut will be inert until one exists.");
            }
            else
            {
                // [RequireComponent(typeof(Camera))] guarantees this attaches cleanly.
                // AddComponent fires Awake immediately, which initializes
                // eventReceiverMask to the camera's full culling mask — we
                // override it below to scope rope-tool input to one layer.
                _cameraInput = cam.gameObject.AddComponent<CameraInputController>();
            }
        }

        ApplyRopeInteractableMask();

        // ClickEffectController is the bridge that fans CameraInputController's
        // press events out to MouseDragger / RopeCutterTrail. It's a Singleton,
        // so one anywhere in the scene is enough.
        if (_clickEffect == null)
            _clickEffect = FindFirstObjectByType<ClickEffectController>(FindObjectsInactive.Include);

        if (_clickEffect == null)
        {
            var go = new GameObject("ClickEffectController");
            DontDestroyOnLoad(go);
            _clickEffect = go.AddComponent<ClickEffectController>();
            // Click/trail visual prefabs stay null — the gameplay path (press
            // event → MouseDragger / RopeCutterTrail) doesn't need them.
        }
    }

    /// <summary>
    /// Constrain CameraInputController's press-time raycast to the
    /// <see cref="RopeInteractableLayerName"/> layer so RMB-drag only grabs
    /// rope-tool targets (cuttable ropes, draggable boxes) and never sweeps
    /// up huge geometry like the village tilemap. The cutter still works on
    /// empty space — it self-assigns as the press target when the raycast
    /// finds nothing on this layer.
    /// </summary>
    private void ApplyRopeInteractableMask()
    {
        if (_cameraInput == null) return;

        int layer = LayerMask.NameToLayer(RopeInteractableLayerName);
        if (layer < 0)
        {
            Debug.LogWarning($"[RopeToolController] Layer '{RopeInteractableLayerName}' " +
                             "not found — rope tool will fall back to the camera's full " +
                             "culling mask and may grab scenery.");
            return;
        }
        _cameraInput.eventReceiverMask = 1 << layer;
    }

    /// <summary>
    /// Pick the camera most likely to "see" gameplay objects: the tagged
    /// MainCamera if there is one, else the first enabled scene camera.
    /// We deliberately skip <c>Camera.allCameras</c> filters that depend on
    /// rendering order so this works in single-camera scenes.
    /// </summary>
    private static Camera PickInputCamera()
    {
        var main = Camera.main;
        if (main != null && main.isActiveAndEnabled) return main;

        var any = FindFirstObjectByType<Camera>();
        return any;
    }
}
