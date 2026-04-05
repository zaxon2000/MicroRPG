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
    [Tooltip("Deceleration (units/s²) applied during the ledge slide. "
           + "Brings the boulder to a stop unless it enters freefall first.")]
    [SerializeField] private float slideFriction = 6f;

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
    private bool _isFreefalling;
    private float _fallTimer;
    private Vector2 _slideVelocity;

    // Cached references.
    private int _playerLayer;
    private int _enemyLayer;
    private int _groundLayer;
    private int _obstacleLayer;
    private LayerMask _groundMask;

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
        _groundMask    = LayerMask.GetMask("Ground");
    }

    private void Update()
    {
        if (!_isFalling) return;

        _fallTimer += Time.deltaTime;
        if (_fallTimer >= maxFallTime)
        {
            Destroy(gameObject);
            return;
        }

        if (_isFreefalling) return;

        // MountainFace_Tilemap covers the entire background including under the
        // ledge, so detecting Climbable overlap fires immediately — wrong signal.
        // Instead, detect when the boulder's center LEAVES the Ground layer
        // (MountainLedge_Tilemap). While on the ledge there is always a Ground
        // tile under the center. The moment it slides off the edge, OverlapPoint
        // returns null → begin freefall.
        if (Physics2D.OverlapPoint(_rig.position, _groundMask) == null)
            BeginFreefall();
    }

    private void FixedUpdate()
    {
        if (!_isFalling || _isFreefalling) return;

        // Apply friction to decelerate the slide.
        float speed = _slideVelocity.magnitude;
        if (speed < 0.01f)
        {
            // Boulder has come to rest on the ledge — cancel the fall state
            // so it can be pushed again.
            _slideVelocity = Vector2.zero;
            _isFalling     = false;
            _collider.excludeLayers = 0;
            return;
        }

        float newSpeed = Mathf.Max(0f, speed - slideFriction * Time.fixedDeltaTime);
        _slideVelocity = _slideVelocity.normalized * newSpeed;
        _rig.MovePosition(_rig.position + _slideVelocity * Time.fixedDeltaTime);
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
            Launch(intendedVelocity.normalized);
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

        // Ground or obstacle: only destroy once the boulder is in freefall
        // (center has crossed into the MountainFace_Tilemap / Climbable layer).
        // During the ledge-slide phase, ground contacts are ignored.
        if ((layer == _groundLayer || layer == _obstacleLayer) && _isFreefalling)
            Destroy(gameObject);
    }

    // ── Launch ──────────────────────────────────────────────────────────────

    private void Launch(Vector2 pushDirection)
    {
        if (_isFalling) return;
        _isFalling    = true;
        _slideVelocity = pushDirection * initialPushSpeed;

        // Stay Kinematic so gravity never acts during the ledge slide.
        // _rig.bodyType remains Kinematic; movement is driven by MovePosition
        // in FixedUpdate. BeginFreefall() switches to Dynamic when the boulder's
        // center enters the MountainFace_Tilemap (Climbable layer).
        _collider.excludeLayers = LayerMask.GetMask("Player");
    }

    private void BeginFreefall()
    {
        _isFreefalling      = true;
        _collider.isTrigger = true;

        _rig.bodyType       = RigidbodyType2D.Dynamic;
        _rig.gravityScale   = fallGravityScale;
        _rig.freezeRotation = true;
        _rig.linearVelocity = _slideVelocity;
    }
}
