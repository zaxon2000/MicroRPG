using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
public class FallingRock : MonoBehaviour
{
    [Header("Impact")]
    public float damage = 15f;
    public bool forceFallOnHit = true;
    public float overrideFallThreshold = -1f;

    [Header("Physics")]
    public Vector2 gravityScaleRange = new(_rockGravitymin, _rockGravitymax); // min and max gravity scale applied to rock on spawn

    [Tooltip("Maximum downward speed (negative Y).")]
    public float maxFallSpeed = -5f;   // e.g. -5 means don’t fall faster than 5 units/sec down

    [Header("Lifetime")]
    public float maxLifetime = 10f;

    [Header("Layers")]
    [Tooltip("Rocks self-destruct when touching these layers (e.g., Ground).")]
    public LayerMask groundLayers;

    private Rigidbody2D _fallingRockRigidbody;
    private float _life;
    private bool _destroyed;
    [SerializeField] private static float _rockGravitymax;
    [SerializeField] private static float _rockGravitymin;

    private void Awake()
    {
        _fallingRockRigidbody = GetComponent<Rigidbody2D>();
    }

    private void OnEnable()
    {
        _life = 0f;
        _destroyed = false;

        if (gravityScaleRange.x > 0f && gravityScaleRange.y > 0f)
            _fallingRockRigidbody.gravityScale = Random.Range(gravityScaleRange.x, gravityScaleRange.y);

        _fallingRockRigidbody.velocity = Vector2.zero;
        _fallingRockRigidbody.angularVelocity = 0f;
    }

    // Clamp velocity every physics step
    private void FixedUpdate()
    {
        if (_fallingRockRigidbody == null) return;

        var rigidbodyVelocity = _fallingRockRigidbody.velocity;
        if (rigidbodyVelocity.y < maxFallSpeed)
            rigidbodyVelocity.y = maxFallSpeed;

        _fallingRockRigidbody.velocity = rigidbodyVelocity;
    }
    
    private void Update()
    {
        if (maxLifetime > 0f)
        {
            _life += Time.deltaTime;
            if (_life >= maxLifetime && !_destroyed)
            {
                _destroyed = true;
                Destroy(gameObject);
            }
        }
    }

    // --- Trigger path (use if rock collider is IsTrigger = true) ---
    private void OnTriggerEnter2D(Collider2D other)
    {
        if (_destroyed) return;
        HandleHit(other.attachedRigidbody ? other.attachedRigidbody.gameObject : other.gameObject, other.gameObject.layer);
    }

    // --- Collision path (use if rock collider is not a trigger) ---
    private void OnCollisionEnter2D(Collision2D collision)
    {
        if (_destroyed) return;
        HandleHit(collision.rigidbody ? collision.rigidbody.gameObject : collision.gameObject, collision.gameObject.layer);
    }

    private void HandleHit(GameObject hitObj, int hitLayer)
    {
        // If we hit ground layers, destroy immediately
        if (IsInLayerMask(hitLayer, groundLayers))
        {
            _destroyed = true;
            Destroy(gameObject);
            return;
        }

        // Player hit?
        var hm = hitObj ? hitObj.GetComponentInParent<HumanMovement>() : null;
        if (hm != null)
        {
            float? thresholdOverride = (overrideFallThreshold > 0f) ? overrideFallThreshold : (float?)null;
            hm.ApplyRockHit(damage, thresholdOverride);

            _destroyed = true;
            Destroy(gameObject);
        }
    }

    private static bool IsInLayerMask(int layer, LayerMask mask)
    {
        return (mask.value & (1 << layer)) != 0;
    }
}
