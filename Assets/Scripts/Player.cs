using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Player : MonoBehaviour
{
    [Header("Stats")]
    public int curHp;                   // our current health
    public int maxHp;                   // our maximum health
    public float moveSpeed;             // ground speed (regular)
    public int damage;                  // damage we deal
    public float interactRange;         // range at which we can interact
    public List<string> inventory = new List<string>();

    [Header("Experience")]
    public int curLevel;                // our current level
    public int curXp;                   // our current experience points
    public int xpToNextLevel;           // xp needed to level up
    public float levelXpModifier;       // modifier applied to 'xpToNextLevel' when we level up

    [Header("Combat")]
    public float attackRange;           // range we can deal damage to an enemy
    public float attackRate;            // minimum time between attacks
    private float lastAttackTime;       // last time we attacked

    // Movement / Facing
    private Vector2 facingDirection;    // direction we're facing (used by attack & interact)
    private Vector2 moveInput;          // raw input this frame

    [Header("Sprites")]
    public Sprite downSprite;
    public Sprite upSprite;
    public Sprite leftSprite;
    public Sprite rightSprite;

    [Header("Climbing")]
    [Tooltip("Speed while climbing on the MountainFace_Tilemap (Climbable layer). Must be < moveSpeed.")]
    public float climbSpeed = 2.6f;
    
    public bool isClimbing;

    // components
    private Rigidbody2D rig;
    private SpriteRenderer sr;
    private PlayerUI ui;
    private ParticleSystem hitEffect;
    
    // [SerializeField] private float inputDeadzone = 0.15f;


    void Awake ()
    {
        // get components
        rig = GetComponent<Rigidbody2D>();
        sr = GetComponent<SpriteRenderer>();
        ui = FindObjectOfType<PlayerUI>();
        hitEffect = gameObject.GetComponentInChildren<ParticleSystem>();

        // 2D top-down defaults
        if (rig != null)
        {
            rig.gravityScale = 0f;
            rig.freezeRotation = true;
        }

        // default facing
        facingDirection = Vector2.down;
        sr.sprite = downSprite;
    }

    void Start ()
    {
        // initialize the UI
        ui.UpdateHealthBar();
        ui.UpdateLevelText();
        ui.UpdateXpBar();
    }

    void Update ()
    {
        Move();

        // when we press the attack button
        if(Input.GetKeyDown(KeyCode.Space))
        {
            // can we attack?
            if(Time.time - lastAttackTime >= attackRate)
                Attack();
        }

        CheckInteract();
    }

    void Move ()
    {
        // --- INPUT ---
        float x = Input.GetAxisRaw("Horizontal");
        float y = Input.GetAxisRaw("Vertical");
        moveInput = new Vector2(x, y);

        // normalize diagonals for consistent speed
        if (moveInput.sqrMagnitude > 1f) moveInput.Normalize();

        // DEADZONE: ignore tiny joystick drift (adjust threshold to taste)
        const float deadzone = 0.1f;
        if (Mathf.Abs(moveInput.x) < deadzone) moveInput.x = 0f;
        if (Mathf.Abs(moveInput.y) < deadzone) moveInput.y = 0f;

        // --- FACING + SPRITE ---
        if (isClimbing)
        {
            // While climbing: always face "up" (into wall)
            facingDirection = Vector2.up;
            sr.sprite = upSprite;
        }
        else
        {
            // Ground: 4-way Zelda style facing (snap to dominant axis) while still allowing diagonal movement
            if (moveInput != Vector2.zero)
            {
                if (Mathf.Abs(moveInput.x) > Mathf.Abs(moveInput.y))
                {
                    facingDirection = (moveInput.x > 0f) ? Vector2.right : Vector2.left;
                }
                else
                {
                    facingDirection = (moveInput.y > 0f) ? Vector2.up : Vector2.down;
                }
            }

            UpdateSpriteDirection(); // uses facingDirection to pick one of the 4 sprites
        }

        // --- SPEED ---
        float speed = isClimbing ? Mathf.Min(climbSpeed, moveSpeed) : moveSpeed;

        // --- APPLY VELOCITY ---
        rig.velocity = moveInput * speed;
    }

    // change player sprite depending on where we're looking (strict 4-dir)
    void UpdateSpriteDirection ()
    {
        // Choose sprite by facingDirection (one of 4 unit vectors)
        if      (facingDirection == Vector2.up)    sr.sprite = upSprite;
        else if (facingDirection == Vector2.down)  sr.sprite = downSprite;
        else if (facingDirection == Vector2.left)  sr.sprite = leftSprite;
        else if (facingDirection == Vector2.right) sr.sprite = rightSprite;
    }



    // shoot a raycast and deal damage if we hit an enemy
    void Attack ()
    {
        lastAttackTime = Time.time;

        // shoot a raycast in the direction of where we're facing.
        RaycastHit2D hit = Physics2D.Raycast(transform.position, facingDirection, attackRange, 1 << 8);

        if(hit.collider != null)
        {
            hit.collider.GetComponent<Enemy>().TakeDamage(damage);

            // play hit effect
            hitEffect.transform.position = hit.collider.transform.position;
            hitEffect.Play();
        }
    }

    // manage interacting with objects
    void CheckInteract ()
    {
        // shoot a raycast in the direction of where we're facing.
        RaycastHit2D hit = Physics2D.Raycast(transform.position, facingDirection, interactRange, 1 << 9);

        if(hit.collider != null)
        {
            Interactable interactable = hit.collider.GetComponent<Interactable>();
            ui.SetInteractText(hit.collider.transform.position, interactable.interactDescription);

            if(Input.GetKeyDown(KeyCode.Space))
                interactable.Interact();
        }
        else
        {
            ui.DisableInteractText();
        }
    }

    // called when we gain xp
    public void AddXp (int xp)
    {
        curXp += xp;

        if(curXp >= xpToNextLevel)
            LevelUp();

        // Update xp bar UI
        ui.UpdateXpBar();
    }

    // called when our xp reaches the max for this level
    void LevelUp ()
    {
        curXp = 0;
        curLevel++;

        xpToNextLevel = (int)((float)xpToNextLevel * levelXpModifier);

        // update level UI
        ui.UpdateLevelText();
    }

    // called when an enemy attacks us
    public void TakeDamage (int damageTaken)
    {
        curHp -= damageTaken;

        if(curHp <= 0)
            Die();

        // update health bar UI
        ui.UpdateHealthBar();
    }

    // called when our hp reaches 0
    void Die ()
    {
        UnityEngine.SceneManagement.SceneManager.LoadScene("Game");
    }

    // adds a new item to our inventory
    public void AddItemToInventory (string item)
    {
        inventory.Add(item);
        ui.UpdateInventoryText();
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if(other.gameObject.layer == LayerMask.NameToLayer("Climbable"))
        {
            isClimbing = true;
        }
        
        else if (other.gameObject.layer == LayerMask.NameToLayer("Ground"))
        {
            isClimbing = false;
        }
    }
}
