using UnityEngine;

/// <summary>
/// A pushable boulder the player knocks off a cliff by walking into it.
/// Uses direct distance checks in FixedUpdate for reliable blocking (immune to
/// velocity overrides in HumanMovement.Move). Trigger callbacks handle impacts
/// while the boulder is falling.
/// </summary>
[RequireComponent(typeof(Rigidbody2D))]
[RequireComponent(typeof(CircleCollider2D))]
public class Boulder : MonoBehaviour
{
    [Header("Fall Physics")]
    [Tooltip("Gravity scale applied once the boulder is pushed off the ledge.")]
    [SerializeField] private float fallGravityScale = 6f;
    [Tooltip("Initial speed applied in the push direction the moment the boulder launches.")]
    [SerializeField] private float initialPushSpeed = 3f;

    [Header("Blocking")]
    [Tooltip("Minimum centre-to-centre distance that keeps the player outside the boulder.")]
    [SerializeField] private float blockingRadius = 0.55f;
    [Tooltip("Player speed toward the boulder required to count as a push.")]
    [SerializeField] private float pushSpeedThreshold = 0.5f;

    [Header("Enemy Impact")]
    [Tooltip("Damage dealt to any enemy the falling boulder strikes.")]
    [SerializeField] private int enemyDamage = 200;
    [Tooltip("Downward speed (world units/s) applied to the enemy on impact.")]
    [SerializeField] private float enemyKnockbackSpeed = 12f;
    [Tooltip("How long the enemy stays in the knocked-back state (seconds).")]
    [SerializeField] private float enemyKnockbackDuration = 1.2f;

    private Rigidbody2D _rig;
    private bool _isFalling;

    // Cached player refs
    private Transform _playerTransform;
    private Rigidbody2D _playerRig;

    private int _playerLayer;
    private int _enemyLayer;
    private int _groundLayer;

    private void Awake()
    {
        _rig = GetComponent<Rigidbody2D>();
        _rig.bodyType     = RigidbodyType2D.Kinematic;
        _rig.gravityScale = 0f;

        _playerLayer = LayerMask.NameToLayer("Player");
        _enemyLayer  = LayerMask.NameToLayer("Enemy");
        _groundLayer = LayerMask.NameToLayer("Ground");
    }

    private void Start()
    {
        Player player = FindFirstObjectByType<Player>();
        if (player != null)
        {
            _playerTransform = player.transform;
            _playerRig       = player.GetComponent<Rigidbody2D>();
        }
    }

    // ── Blocking + push detection (runs every physics step) ──────────────
    private void FixedUpdate()
    {
        if (_isFalling || _playerTransform == null || _playerRig == null) return;

        Vector2 boulderPos = (Vector2)transform.position;
        Vector2 playerPos  = (Vector2)_playerTransform.position;
        Vector2 delta      = playerPos - boulderPos;
        float   dist       = delta.magnitude;

        if (dist >= blockingRadius || dist < 0.001f) return;

        Vector2 outward = delta.normalized;

        // Sample velocity BEFORE any modification.
        float velToward = Vector2.Dot(_playerRig.linearVelocity, -outward);

        // Teleport the player to the blocking edge.
        _playerTransform.position = (Vector3)(boulderPos + outward * blockingRadius);

        // Cancel the velocity component that points into the boulder.
        if (velToward > 0f)
            _playerRig.linearVelocity += outward * velToward;

        // If the player is pressing hard enough, launch the boulder.
        if (velToward > pushSpeedThreshold)
        {
            Vector2 pushDir = -outward;
            // Allow sideways and downward pushes; reject upward.
            if (pushDir.y <= 0.1f)
                Launch(pushDir);
        }
    }

    // ── While falling: detect ground and enemy impacts via triggers ──────
    private void OnTriggerEnter2D(Collider2D other)
    {
        if (!_isFalling) return;

        int otherLayer = other.gameObject.layer;

        if (otherLayer == _playerLayer) return;

        if (otherLayer == _enemyLayer)
        {
            Enemy enemy = other.GetComponentInParent<Enemy>();
            if (enemy != null)
                enemy.BoulderHit(enemyDamage,
                                 Vector2.down * enemyKnockbackSpeed,
                                 enemyKnockbackDuration);
            Destroy(gameObject);
            return;
        }

        if (otherLayer == _groundLayer)
            Destroy(gameObject);
    }

    /// <summary>Switches the boulder to Dynamic and launches it.</summary>
    private void Launch(Vector2 pushDirection)
    {
        if (_isFalling) return;
        _isFalling = true;

        _rig.bodyType       = RigidbodyType2D.Dynamic;
        _rig.gravityScale   = fallGravityScale;
        _rig.freezeRotation = true;
        _rig.linearVelocity = pushDirection * initialPushSpeed;
    }
}
