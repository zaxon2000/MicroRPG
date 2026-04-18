using UnityEngine;

/// <summary>
/// Scene-placed configuration object that holds the prefab references and tuning
/// values used by <see cref="BridgeBuilder"/>. Drop this on any GameObject in the
/// Game scene and assign the three prefabs once — the builder picks it up via
/// <see cref="Instance"/> at runtime.
///
/// Keeping prefab refs on a serialized MonoBehaviour (instead of in
/// <c>Resources/</c> or addressables) means the user can swap prefabs from the
/// Inspector without touching code, and the editor will warn at edit time if a
/// reference is missing.
/// </summary>
[DisallowMultipleComponent]
public class BridgeBuilderConfig : MonoBehaviour
{
    private static BridgeBuilderConfig _instance;

    public static BridgeBuilderConfig Instance
    {
        get
        {
            if (_instance == null)
                _instance = FindFirstObjectByType<BridgeBuilderConfig>(FindObjectsInactive.Include);
            return _instance;
        }
    }

    [Header("Prefabs")]
    [Tooltip("Prefab placed when the player picks 'IndependentTack' in the bridge popup. " +
             "Should be Assets/EasyRopes2D/.../Blocks/IndependentTack.prefab.")]
    public GameObject independentTackPrefab;

    [Tooltip("Prefab placed when the player picks 'Bridge Block' in the bridge popup. " +
             "Should be Assets/EasyRopes2D/.../Blocks/Wood/08_Wood_Box100x100.prefab.")]
    public GameObject bridgeBlockPrefab;

    [Tooltip("Rope prefab instantiated between consecutive endpoints once the bridge " +
             "is finalized. Should be Assets/EasyRopes2D/.../Ropes/02_BridgeRope.prefab.")]
    public GameObject bridgeRopePrefab;

    [Header("Placement")]
    [Tooltip("Maximum world-space distance allowed between a new bridge block and the " +
             "previous endpoint. Ghost previews are clamped to this radius so the rope " +
             "never has to span more than its rest length.")]
    public float maxBlockDistance = 0.6f;

    [Tooltip("Local scale applied to the runtime-built Bridge root. The EasyRopes 2D " +
             "demo prefab uses 0.0025 to bring its 80x80-unit blocks down to 0.2 world " +
             "units; that's the right default for instantiating raw demo prefabs.")]
    public float bridgeRootScale = 0.0025f;

    [Header("UI")]
    [Tooltip("Initial size of the bridge popup window in canvas units. " +
             "Width is wide enough for the four button labels; height fits a title " +
             "bar plus four 32px-tall buttons.")]
    public Vector2 popupSize = new Vector2(220f, 220f);

    private void Awake()
    {
        if (_instance != null && _instance != this)
        {
            // Two configs in the scene is almost certainly a mistake — keep the
            // first one and warn so the duplicate is easy to find.
            Debug.LogWarning(
                $"[BridgeBuilderConfig] Multiple instances found; keeping " +
                $"'{_instance.gameObject.name}' and ignoring '{gameObject.name}'.");
            return;
        }
        _instance = this;
    }

    private void OnValidate()
    {
        if (maxBlockDistance < 0.05f) maxBlockDistance = 0.05f;
        if (bridgeRootScale <= 0f)    bridgeRootScale = 0.0025f;
        if (popupSize.x < 120f)       popupSize.x = 120f;
        if (popupSize.y < 120f)       popupSize.y = 120f;
    }
}
