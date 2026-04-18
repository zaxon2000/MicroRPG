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
    [Tooltip("Target maximum world-space rope length between consecutive endpoints. " +
             "BridgeBuilder uses this to decide how many intermediate blocks to spawn " +
             "between TackA and TackB: segments = ceil(distance / maxBlockDistance), " +
             "blocks = segments - 1. " +
             "\n\nIMPORTANT: this should be at least the rope prefab's natural per-segment " +
             "length (~0.77 world units for the default 02_BridgeRope prefab at " +
             "bridgeRootScale 0.0025). Going below it makes each chain link slack and the " +
             "blocks sag through the gap; going far below, with spring-mode joints, used to " +
             "make the bridge explode (see ropeMaxDistanceOnly comment). With " +
             "ropeMaxDistanceOnly enabled, smaller values are now safe but produce a saggier " +
             "bridge.")]
    public float maxBlockDistance = 0.8f;

    [Tooltip("Local scale applied to the runtime-built Bridge root. The EasyRopes 2D " +
             "demo prefab uses 0.0025 to bring its 80x80-unit blocks down to 0.2 world " +
             "units; that's the right default for instantiating raw demo prefabs.")]
    public float bridgeRootScale = 0.0025f;

    [Header("Rope Physics (per-segment override)")]
    [Tooltip("Disable break-on-stretch on each runtime rope. The BridgeRope prefab " +
             "ships with break enabled, which makes a chain of blocks easily snap " +
             "under their own weight. Off by default → bridges hold up.")]
    public bool ropeCanBreak = false;

    [Tooltip("Force every per-node joint to a DistanceJoint2D with maxDistanceOnly=true " +
             "(chain-link behaviour) instead of the prefab's stiff SpringJoint2D. " +
             "\n\nWhy this matters: each spring's target length is the rope's natural " +
             "per-link distance (~0.26 world units for our scale). When the actual block " +
             "spacing is shorter than that, every spring along the bridge pushes its " +
             "attached blocks apart simultaneously — with N segments and 20 Hz springs, the " +
             "combined outward impulse is enough to fragment the wood blocks on frame 1 " +
             "(they shatter into fragments via their own damage system). " +
             "\n\nWith maxDistanceOnly, joints only resist stretching and go slack when " +
             "compressed — bridges stay assembled at any segment length. Tradeoff: chain " +
             "links can't push slack out, so going below maxBlockDistance ~0.77 produces a " +
             "saggier bridge. On by default.")]
    public bool ropeMaxDistanceOnly = true;

    [Tooltip("Stiffness of each rope's internal springs. Only matters when " +
             "ropeMaxDistanceOnly is OFF (otherwise joints are DistanceJoint2D, not springs). " +
             "The prefab default (5 Hz) is bouncy; bumping this makes the bridge less floppy. " +
             "Setting to 0 uses the prefab value unchanged.")]
    public float ropeSpringFrequency = 20f;

    [Tooltip("Spring damping (0..1). Only matters when ropeMaxDistanceOnly is OFF. " +
             "Higher values settle bridge oscillation faster. " +
             "Setting to a negative number uses the prefab value unchanged.")]
    [Range(-0.01f, 1f)]
    public float ropeSpringDamping = 0.3f;

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
    }
}
