using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Player : MonoBehaviour
{
    [Header("Debug Logging")] [SerializeField]
    private bool runDebugs;
    
    [Header("Stats")]
    public float curHp;                   // our current health
    public float maxHp;                   // our maximum health
    public float interactRange;         // range at which we can interact
    public List<string> inventory = new();

    [Header("Experience")]
    public int curLevel;                // our current level
    public int curXp;                   // our current experience points
    public int xpToNextLevel;           // xp needed to level up
    public float levelXpModifier;       // modifier applied to 'xpToNextLevel' when we level up
    
    private HumanMovement _movement;

    // components
    private PlayerUI _playerUI;
    
    void Awake ()
    {
        // get components
        _movement = GetComponent<HumanMovement>();
        if (_movement == null) _movement = gameObject.AddComponent<HumanMovement>();

        _playerUI = FindObjectOfType<PlayerUI>();
    }

    void Start ()
    {
        // initialize the UI
        _playerUI.UpdateHealthBar();
        _playerUI.UpdateLevelText();
        _playerUI.UpdateXpBar();
    }

    void Update ()
    {
        CheckInteract();
    }

    // manage interacting with objects
    void CheckInteract ()
    {
        // shoot a raycast in the direction of where we're facing.
        Vector2 facing = _movement != null ? _movement.FacingDirection : Vector2.down;
        RaycastHit2D hit = Physics2D.Raycast(transform.position, facing, interactRange, 1 << 9);

        if(hit.collider != null)
        {
            Interactable interactable = hit.collider.GetComponent<Interactable>();
            
            if(interactable == null) return;
            
            _playerUI.SetInteractText(hit.collider.transform.position, interactable.interactDescription);

            if(Input.GetKeyDown(KeyCode.E))
                interactable.Interact();
        }
        else
        {
            _playerUI.DisableInteractText();
        }
    }

    // called when we gain xp
    public void AddXp (int xp)
    {
        curXp += xp;

        if(curXp >= xpToNextLevel)
            LevelUp();

        // Update xp bar UI
        _playerUI.UpdateXpBar();
    }

    // called when our xp reaches the max for this level
    void LevelUp ()
    {
        curXp = 0;
        curLevel++;

        xpToNextLevel = (int)((float)xpToNextLevel * levelXpModifier);

        // update level UI
        _playerUI.UpdateLevelText();
    }

    // called when an enemy attacks us
    public void TakeDamage (float damageTaken)
    {
        curHp -= damageTaken;

        if (runDebugs)
            Debug.Log($"[Player] TakeDamage: damageTaken={damageTaken}, curHp={curHp}");
        
        if(curHp <= 0)
            Die();

        // update health bar UI
        _playerUI.UpdateHealthBar();
        
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
        _playerUI.UpdateInventoryText();
    }

}
