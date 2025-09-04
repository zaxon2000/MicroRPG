using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Player : MonoBehaviour
{
    [Header("Stats")]
    public int curHp;                   // our current health
    public int maxHp;                   // our maximum health
    public float moveSpeed;             // how fast we move
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

    private Vector2 facingDirection;    // direction we're facing

    [Header("Sprites")]
    public Sprite downSprite;
    public Sprite upSprite;
    public Sprite leftSprite;
    public Sprite rightSprite;

    // components
    private Rigidbody2D rig;
    private SpriteRenderer sr;
    private PlayerUI ui;
    private ParticleSystem hitEffect;

    void Awake ()
    {
        // get components
        rig = GetComponent<Rigidbody2D>();
        sr = GetComponent<SpriteRenderer>();
        ui = FindObjectOfType<PlayerUI>();
        hitEffect = gameObject.GetComponentInChildren<ParticleSystem>();
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
        // get horizontal and vertical keyboard inputs
        float x = Input.GetAxis("Horizontal");
        float y = Input.GetAxis("Vertical");

        // calculate the velocity we're going to move at
        Vector2 vel = new Vector2(x, y);

        // calculate the facing direction
        if(vel.magnitude != 0)
            facingDirection = vel;

        UpdateSpriteDirection();

        // set the velocity
        rig.velocity = vel * moveSpeed;
    }

    // change player sprite depending on where we're looking
    void UpdateSpriteDirection ()
    {
        if (facingDirection == Vector2.up)
            sr.sprite = upSprite;
        else if (facingDirection == Vector2.down)
            sr.sprite = downSprite;
        else if (facingDirection == Vector2.left)
            sr.sprite = leftSprite;
        else if (facingDirection == Vector2.right)
            sr.sprite = rightSprite;
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
}