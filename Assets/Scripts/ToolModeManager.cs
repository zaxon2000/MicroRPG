using System;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

/// <summary>
/// Singleton that owns the player's "active tool" mode (None / Grapple Hook / Ropes)
/// and exposes a runtime-built retropixel toolbar for switching between them.
///
/// Other systems subscribe to <see cref="OnModeChanged"/> (or read <see cref="Mode"/>)
/// to gate their behavior. The grapple hook and the rope drag/cut tools are
/// mutually exclusive — only the active mode's system listens to mouse input.
///
/// Auto-bootstrapped on scene load, so no scene editing is required: it finds
/// (or creates) a Canvas, builds a horizontal panel with 3 buttons, and wires
/// them to mode switches. All sprites are loaded from Resources/ at runtime.
/// </summary>
public class ToolModeManager : MonoBehaviour
{
    public enum ToolMode
    {
        None,
        GrappleHook,
        Ropes,
    }

    // ----- Singleton -----------------------------------------------------------
    private static ToolModeManager _instance;
    public static ToolModeManager Instance
    {
        get
        {
            if (_instance == null)
                _instance = FindFirstObjectByType<ToolModeManager>();
            return _instance;
        }
    }

    // ----- State / API ---------------------------------------------------------
    [SerializeField] private ToolMode _mode = ToolMode.None;
    public ToolMode Mode
    {
        get => _mode;
        set => SetMode(value);
    }

    /// <summary>Fires whenever the mode changes. Subscribers should react idempotently.</summary>
    public static event Action<ToolMode> OnModeChanged;

    // ----- Runtime UI references ----------------------------------------------
    private Button _btnNone;
    private Button _btnHook;
    private Button _btnRopes;

    // Tint applied to the active button so the user can see which mode is on.
    private static readonly Color ActiveTint = new Color(1f, 0.85f, 0.4f, 1f);
    private static readonly Color InactiveTint = Color.white;

    // ----- Auto-bootstrap ------------------------------------------------------
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void Bootstrap()
    {
        if (_instance != null) return;
        var existing = FindFirstObjectByType<ToolModeManager>();
        if (existing != null)
        {
            _instance = existing;
            return;
        }
        var go = new GameObject("ToolModeManager");
        _instance = go.AddComponent<ToolModeManager>();
        DontDestroyOnLoad(go);
    }

    // ----- Unity lifecycle -----------------------------------------------------
    private void Awake()
    {
        if (_instance != null && _instance != this)
        {
            Destroy(gameObject);
            return;
        }
        _instance = this;
    }

    private void Start()
    {
        EnsureEventSystem();
        BuildToolbar();
        // Push initial state so listeners (HumanMovement, RopeToolController) sync up.
        SetMode(_mode, force: true);
    }

    /// <summary>
    /// Make sure there's an <see cref="EventSystem"/> in the scene; without one,
    /// the buttons we just built would be non-interactive. The Game scene already
    /// ships with one so this is a defensive fallback for stripped-down scenes.
    /// </summary>
    private static void EnsureEventSystem()
    {
        if (EventSystem.current != null) return;
        if (FindFirstObjectByType<EventSystem>() != null) return;

        var go = new GameObject("EventSystem", typeof(EventSystem));
        DontDestroyOnLoad(go);

        // Pick whichever input module type is available. The project pulls in
        // com.unity.inputsystem, so prefer InputSystemUIInputModule; fall back to
        // StandaloneInputModule if the Input System isn't loaded for some reason.
        var inputSystemModule = Type.GetType(
            "UnityEngine.InputSystem.UI.InputSystemUIInputModule, Unity.InputSystem");
        if (inputSystemModule != null)
            go.AddComponent(inputSystemModule);
        else
            go.AddComponent<StandaloneInputModule>();
    }

    // ----- Mode switching ------------------------------------------------------
    public void SetMode(ToolMode newMode, bool force = false)
    {
        if (!force && newMode == _mode) return;
        _mode = newMode;
        UpdateButtonHighlights();
        OnModeChanged?.Invoke(_mode);
    }

    private void UpdateButtonHighlights()
    {
        SetButtonActive(_btnNone, _mode == ToolMode.None);
        SetButtonActive(_btnHook, _mode == ToolMode.GrappleHook);
        SetButtonActive(_btnRopes, _mode == ToolMode.Ropes);
    }

    private static void SetButtonActive(Button btn, bool active)
    {
        if (btn == null) return;
        var img = btn.GetComponent<Image>();
        if (img != null) img.color = active ? ActiveTint : InactiveTint;
    }

    // ----- UI construction -----------------------------------------------------
    private void BuildToolbar()
    {
        Canvas canvas = FindOrCreatePlayerCanvas();
        if (canvas == null)
        {
            Debug.LogWarning("[ToolModeManager] No Canvas available — toolbar not built.");
            return;
        }

        // Outer panel ----------------------------------------------------------
        var panelGO = new GameObject("ToolModePanel",
            typeof(RectTransform), typeof(Image), typeof(HorizontalLayoutGroup));
        panelGO.transform.SetParent(canvas.transform, false);

        var panelRT = panelGO.GetComponent<RectTransform>();
        panelRT.anchorMin = new Vector2(0.5f, 1f);
        panelRT.anchorMax = new Vector2(0.5f, 1f);
        panelRT.pivot = new Vector2(0.5f, 1f);
        panelRT.anchoredPosition = new Vector2(0f, -10f);
        panelRT.sizeDelta = new Vector2(240f, 80f);

        var panelImg = panelGO.GetComponent<Image>();
        panelImg.sprite = LoadSprite("LiberateUI/PixelArtRetro/Buttons/SecondaryButtonSurfaceFilled");
        panelImg.type = Image.Type.Sliced;
        panelImg.color = new Color(0f, 0f, 0f, 0.55f);

        var hlg = panelGO.GetComponent<HorizontalLayoutGroup>();
        hlg.spacing = 8f;
        hlg.padding = new RectOffset(8, 8, 8, 8);
        hlg.childAlignment = TextAnchor.MiddleCenter;
        hlg.childControlWidth = false;
        hlg.childControlHeight = false;
        hlg.childForceExpandWidth = false;
        hlg.childForceExpandHeight = false;

        // Buttons --------------------------------------------------------------
        Sprite btnSprite = LoadSprite("LiberateUI/PixelArtRetro/Buttons/PrimaryButton");
        Sprite crossIcon = LoadSprite("LiberateUI/PixelArtRetro/Icons/Cross");
        Sprite hookIcon = LoadSprite("ToolIcons/Hook1");
        Sprite ropeIcon = LoadSprite("ToolIcons/rope2d_icon");

        _btnNone = CreateButton(panelGO.transform, "BtnNone", btnSprite, crossIcon,
            () => SetMode(ToolMode.None));
        _btnHook = CreateButton(panelGO.transform, "BtnGrappleHook", btnSprite, hookIcon,
            () => SetMode(ToolMode.GrappleHook));
        _btnRopes = CreateButton(panelGO.transform, "BtnRopes", btnSprite, ropeIcon,
            () => SetMode(ToolMode.Ropes));

        UpdateButtonHighlights();
    }

    private static Button CreateButton(Transform parent, string name, Sprite background,
                                       Sprite icon, Action onClick)
    {
        var btnGO = new GameObject(name,
            typeof(RectTransform), typeof(Image), typeof(Button));
        btnGO.transform.SetParent(parent, false);

        var rt = btnGO.GetComponent<RectTransform>();
        rt.sizeDelta = new Vector2(64f, 64f);

        var bgImg = btnGO.GetComponent<Image>();
        bgImg.sprite = background;
        bgImg.type = Image.Type.Sliced;
        bgImg.color = InactiveTint;

        var btn = btnGO.GetComponent<Button>();
        btn.targetGraphic = bgImg;
        btn.onClick.AddListener(() => onClick?.Invoke());

        // Icon child: keep margin so the button frame is still visible around it.
        if (icon != null)
        {
            var iconGO = new GameObject("Icon", typeof(RectTransform), typeof(Image));
            iconGO.transform.SetParent(btnGO.transform, false);
            var iconRT = iconGO.GetComponent<RectTransform>();
            iconRT.anchorMin = new Vector2(0f, 0f);
            iconRT.anchorMax = new Vector2(1f, 1f);
            iconRT.offsetMin = new Vector2(10f, 10f);
            iconRT.offsetMax = new Vector2(-10f, -10f);
            var iconImg = iconGO.GetComponent<Image>();
            iconImg.sprite = icon;
            iconImg.preserveAspect = true;
            iconImg.raycastTarget = false;
        }

        return btn;
    }

    // ----- Helpers -------------------------------------------------------------
    private static Canvas FindOrCreatePlayerCanvas()
    {
        // Prefer the named PlayerUICanvas if the project ships one.
        var named = GameObject.Find("PlayerUICanvas");
        if (named != null)
        {
            var c = named.GetComponent<Canvas>();
            if (c != null) return c;
        }

        // Otherwise pick any visible overlay canvas.
        var any = FindFirstObjectByType<Canvas>();
        if (any != null) return any;

        // Last resort: spin one up so the toolbar still appears.
        var go = new GameObject("ToolModeCanvas",
            typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        var canvas = go.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        var scaler = go.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(800f, 600f);
        return canvas;
    }

    private static Sprite LoadSprite(string resourcesPath)
    {
        var s = Resources.Load<Sprite>(resourcesPath);
        if (s == null)
            Debug.LogWarning($"[ToolModeManager] Missing sprite at Resources/{resourcesPath}");
        return s;
    }
}
