using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
[RequireComponent(typeof(SpriteRenderer))]
public class Enemy : MonoBehaviour
{
    [Header("Stats")]
    public int curHp;                   // our current health
    public int maxHp;                   // our maximum health
    public float moveSpeed;             // how fast we move
    public int xpToGive;                // xp we give when we die

    [Header("Target")]
    public float chaseRange;            // range at which we move towards the player
    public float attackRange;           // range at which we attack the player
    private Player player;

    [Header("PlayerAttack")]
    public int damage;                  // damage we deal to the player
    public float attackRate;            // minimum time between attacks
    private float lastAttackTime;       // last time we attacked the player

    [Header("Sprites")]
    public Sprite downSprite;
    public Sprite upSprite;
    public Sprite leftSprite;
    public Sprite rightSprite;

    // components
    private Rigidbody2D rig;
    private SpriteRenderer spriteRendererComponent;

    // runtime state
    public Vector2 FacingDirection { get; private set; } = Vector2.down;
    private bool isClimbing;

    // knockback state — set by BoulderHit(); overrides AI while active
    private bool _isKnockedBack;
    private float _knockbackTimer;

    [Header("Bridge Walking")]
    [Tooltip("Gravity scale applied while standing on top of a bridge block " +
             "(see BridgeWalkable). Mirrors the player's bridge behaviour so " +
             "enemies also walk across rope bridges instead of floating.")]
    public float bridgeGravityScale = 3f;

    // Tracks which BridgeWalkable surfaces are currently supporting the enemy.
    // Same multi-contact pattern as HumanMovement — straddling two blocks is
    // common while walking, so we keep a set rather than a single flag.
    private readonly HashSet<BridgeWalkable> _bridgesWalkingOn = new();
    private bool _isOnBridge;

    void Awake ()
    {
        // get the player target
        player = FindObjectOfType<Player>();

        // get the rigidbody component
        rig = GetComponent<Rigidbody2D>();
        spriteRendererComponent = GetComponent<SpriteRenderer>();

        if (spriteRendererComponent != null && downSprite != null)
            spriteRendererComponent.sprite = downSprite;
    }

    void Update ()
    {
        // While knocked back by a boulder, let physics drive the body.
        if (_isKnockedBack)
        {
            _knockbackTimer -= Time.deltaTime;
            if (_knockbackTimer <= 0f)
            {
                _isKnockedBack = false;
                rig.gravityScale    = 0f;
                rig.linearVelocity  = Vector2.zero;
            }
            return;
        }

        // calculate the distance between us and the player
        float playerDist = Vector2.Distance(transform.position, player.transform.position);

        // if we're in attack range, try and attack the player
        if(playerDist <= attackRange)
        {
            if(Time.time - lastAttackTime >= attackRate)
                Attack();

            rig.linearVelocity = Vector2.zero;
        }
        // if we're in the chase range, chase after the player
        else if(playerDist <= chaseRange)
            Chase();
        else
            rig.linearVelocity = Vector2.zero;
    }

    // move towards the player
    void Chase ()
    {
        // calculate direction between us and the player
        Vector2 dir = (player.transform.position - transform.position).normalized;

        UpdateFacingDirection(dir);

        rig.linearVelocity = dir * moveSpeed;
    }

    void UpdateFacingDirection(Vector2 direction)
    {
        if (isClimbing)
        {
            FacingDirection = Vector2.up;

            if (spriteRendererComponent != null && upSprite != null)
                spriteRendererComponent.sprite = upSprite;

            return;
        }

        if (direction.sqrMagnitude <= 0.0001f)
            return;

        if (Mathf.Abs(direction.x) > Mathf.Abs(direction.y))
            FacingDirection = (direction.x > 0f) ? Vector2.right : Vector2.left;
        else
            FacingDirection = (direction.y > 0f) ? Vector2.up : Vector2.down;

        if (spriteRendererComponent == null)
            return;

        if (FacingDirection == Vector2.up && upSprite != null)
            spriteRendererComponent.sprite = upSprite;
        else if (FacingDirection == Vector2.down && downSprite != null)
            spriteRendererComponent.sprite = downSprite;
        else if (FacingDirection == Vector2.left && leftSprite != null)
            spriteRendererComponent.sprite = leftSprite;
        else if (FacingDirection == Vector2.right && rightSprite != null)
            spriteRendererComponent.sprite = rightSprite;
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        if (other.gameObject.layer == LayerMask.NameToLayer("Climbable"))
            isClimbing = true;
    }

    void OnTriggerStay2D(Collider2D other)
    {
        if (other.gameObject.layer == LayerMask.NameToLayer("Climbable"))
            isClimbing = true;
    }

    void OnTriggerExit2D(Collider2D other)
    {
        if (other.gameObject.layer == LayerMask.NameToLayer("Climbable"))
            isClimbing = false;
    }

    // ----- Bridge engagement --------------------------------------------------
    // Same pattern as HumanMovement: contact from above engages gravity so the
    // enemy stands on the planks; exiting the last block restores the top-down
    // floating behaviour. No "press up to release" — enemies don't have an
    // explicit climb-input axis; if they need to climb away, the existing
    // Climbable-layer trigger handlers above will fire as normal.

    void OnCollisionStay2D(Collision2D collision)
    {
        if (_isKnockedBack) return; // physics-driven; don't override

        var bw = collision.gameObject.GetComponent<BridgeWalkable>();
        if (bw == null) return;

        // Only engage when the enemy is on TOP of the block (standing surface).
        if (transform.position.y > bw.transform.position.y)
        {
            _bridgesWalkingOn.Add(bw);
            UpdateBridgeState();
        }
    }

    void OnCollisionExit2D(Collision2D collision)
    {
        var bw = collision.gameObject.GetComponent<BridgeWalkable>();
        if (bw == null) return;
        _bridgesWalkingOn.Remove(bw);
        UpdateBridgeState();
    }

    private void UpdateBridgeState()
    {
        bool wasOn = _isOnBridge;
        _isOnBridge = _bridgesWalkingOn.Count > 0;
        if (_isOnBridge == wasOn) return;

        if (rig == null) return;

        if (_isOnBridge)
        {
            // Engaged: gravity holds the enemy on the planks. Drop climb state
            // since the enemy is now standing on a horizontal surface.
            rig.gravityScale = bridgeGravityScale;
            isClimbing = false;
        }
        else
        {
            // Disengaged: back to top-down (no gravity, no residual fall vel).
            rig.gravityScale = 0f;
            rig.linearVelocity = Vector2.zero;
        }
    }

    // damage the player
    void Attack ()
    {
        lastAttackTime = Time.time;

        player.TakeDamage(damage);
    }

    // called when the player attacks us
    public void TakeDamage (int damageTaken)
    {
        curHp -= damageTaken;

        if(curHp <= 0)
            Die();
    }

    /// <summary>
    /// Called when a falling boulder strikes this enemy.
    /// Applies heavy damage and forces a downward knockback to simulate falling.
    /// </summary>
    public void BoulderHit(int damage, Vector2 knockbackVelocity, float duration)
    {
        TakeDamage(damage);

        // Only apply knockback physics if the enemy survived.
        if (curHp <= 0) return;

        _isKnockedBack      = true;
        _knockbackTimer     = duration;
        rig.gravityScale    = 3f;
        rig.linearVelocity  = knockbackVelocity;
    }

    // called when out hp reaches 0
    void Die ()
    {
        player.AddXp(xpToGive);
        QuestEvents.RaiseEnemyKilled(gameObject.name);
        Destroy(gameObject);
    }
}