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

    // called when out hp reaches 0
    void Die ()
    {
        player.AddXp(xpToGive);
        Destroy(gameObject);
    }
}