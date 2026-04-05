using UnityEngine;

/// <summary>
/// A pushable boulder that blocks the player and can be knocked off a cliff.
///
/// Blocking is handled by the physics engine via a non-trigger collider.
/// Push detection uses OnCollisionStay2D to read the player's intended
/// velocity (exposed by HumanMovement). On launch the collider switches to
/// trigger so the falling boulder passes through the player and detects
/// impacts via OnTriggerEnter2D / OnCollisionEnter2D.
/// </summary>
[RequireComponent(typeof(Rigidbody2D))]
[RequireComponent(typeof(CircleCollider2D))]
public class Boulder : MonoBehaviour
{
    [Header("Fall Physics")]
    [Tooltip("Gravity scale applied once the boulder is pushed off the ledge.")]
    [SerializeField] private float fallGravityScale = 6f;
    [Tooltip("Initial speed in the push direction the moment the boulder launches.")]
    [SerializeField] private float initialPushSpeed = 3f;
    [Tooltip("Safety timeout — boulder self-destructs after this many seconds of falling.")]
    [SerializeField] private float maxFallTime = 10f;

    [Header("Push")]
    [Tooltip("Player speed toward the boulder (units/s) required to count as a push.")]
    [SerializeField] private float pushSpeedThreshold = 0.5f;

    [Header("Enemy Impact")]
    [Tooltip("Damage dealt to any enemy the falling boulder strikes.")]
    [SerializeField] private int enemyDamage = 200;
    [Tooltip("Downward speed (units/s) applied to the enemy on impact.")]
    [SerializeField] private float enemyKnockbackSpeed = 12f;
    [Tooltip("Duration of the enemy knockback state (seconds).")]
    [SerializeField] private float enemyKnockbackDuration = 1.2f;

    // Runtime
    private Rigidbody2D _rig;
    private CircleCollider2D _collider;
    private bool _isFalling;
    private float _fallTimer;

    // Cached references.
    private int _playerLayer;
    private int _enemyLayer;
    private int _groundLayer;
    private int _obstacleLayer;

    /// <summary>True once the boulder has been pushed and is falling.</summary>
    public bool IsFalling => _isFalling;

    // ── Lifecycle ────────────────────────────────────────────────────────────

    private void Awake()
    {
        _rig      = GetComponent<Rigidbody2D>();
        _collider = GetComponent<CircleCollider2D>();

        // Kinematic with a non-trigger collider — physics handles blocking.
        _rig.bodyType                 = RigidbodyType2D.Kinematic;
        _rig.useFullKinematicContacts = true;
        _rig.gravityScale             = 0f;
        _collider.isTrigger           = false;

        _playerLayer   = LayerMask.NameToLayer("Player");
        _enemyLayer    = LayerMask.NameToLayer("Enemy");
        _groundLayer   = LayerMask.NameToLayer("Ground");
        _obstacleLayer = LayerMask.NameToLayer("Obstacle");
    }

    private void Update()
    {
        if (!_isFalling) return;
        _fallTimer += Time.deltaTime;
        if (_fallTimer >= maxFallTime)
            Destroy(gameObject);
    }

    // ── Push detection ───────────────────────────────────────────────────────

    /// <summary>
    /// Called by HumanMovement.OnCollisionStay2D — fires unconditionally on
    /// the Dynamic player body, so no special Kinematic flags are needed here.
    /// </summary>
    public void TryPush(Vector2 playerPosition, Vector2 intendedVelocity)
    {
        if (_isFalling) return;

        Vector2 towardBoulder = (_rig.position - playerPosition).normalized;
        float pushSpeed = Vector2.Dot(intendedVelocity, towardBoulder);

        if (pushSpeed > pushSpeedThreshold)
            Launch(towardBoulder);
    }

    // ── Impact detection (falling state) ────────────────────────────────────

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (!_isFalling) return;
        HandleImpact(other.gameObject);
    }

    /// <summary>Fallback for non-trigger colliders the boulder meets while falling.</summary>
    private void OnCollisionEnter2D(Collision2D collision)
    {
        if (!_isFalling) return;
        HandleImpact(collision.gameObject);
    }

    private void HandleImpact(GameObject other)
    {
        int layer = other.layer;

        // Ignore the player while falling.
        if (layer == _playerLayer) return;

        // Enemy: heavy damage + downward knockback, then destroy.
        if (layer == _enemyLayer)
        {
            Enemy enemy = other.GetComponentInParent<Enemy>();
            if (enemy != null)
                enemy.BoulderHit(enemyDamage,
                                 Vector2.down * enemyKnockbackSpeed,
                                 enemyKnockbackDuration);
            Destroy(gameObject);
            return;
        }

        // Ground or obstacle (another ledge): shatter on impact.
        if (layer == _groundLayer || layer == _obstacleLayer)
            Destroy(gameObject);
    }

    // ── Launch ──────────────────────────────────────────────────────────────

    private void Launch(Vector2 pushDirection)
    {
        if (_isFalling) return;
        _isFalling = true;

        // Switch to trigger so the falling boulder passes through the player
        // and detects enemy / ground impacts via OnTriggerEnter2D.
        _collider.isTrigger = true;

        _rig.bodyType       = RigidbodyType2D.Dynamic;
        _rig.gravityScale   = fallGravityScale;
        _rig.freezeRotation = true;
        _rig.linearVelocity = pushDirection * initialPushSpeed;
    }
}
