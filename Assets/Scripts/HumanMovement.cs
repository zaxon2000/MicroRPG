using System.Collections;
using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
[RequireComponent(typeof(SpriteRenderer))]
public class HumanMovement : MonoBehaviour
{
    [Header("Debug Logging")] [SerializeField]
    private bool runDebugs;
    
    [Header("Reference Components")]
    [SerializeField] private Rigidbody2D rig;
    [SerializeField] private SpriteRenderer spriteRendererComponent;
    [SerializeField] private PlayerUI playerUI;
    [SerializeField] private Player player;
    
    [Header("Movement")]
    public float moveSpeed = 5f;
    [Tooltip("Speed while climbing on the MountainFace_Tilemap (Climbable layer). Must be < moveSpeed.")]
    public float climbSpeed = 2.6f;
    
    [Header("Sprint")]
    public float sprintMultiplier = 2f;             // multiplier for sprint speed
    
    [Header("Sprites")]
    public Sprite downSprite;
    public Sprite upSprite;
    public Sprite leftSprite;
    public Sprite rightSprite;

    [Header("Climb State")]
    public bool isClimbing;
    public float timeToRecoverGripWhileDropping = 2.0f; 
    [SerializeField] private float layerTransitionDelay = 1f;
    
    // Drop state
    [SerializeField] private bool isDropping;
    private float _dropDurationElapsed;
    private float _dropCooldownTimer = 0f;
    [SerializeField] private const float DropCooldown = 0.5f; // Half second delay before can drop again
    [SerializeField] private float dropDurationDamageThreshold = 0.5f;
    [SerializeField] private float baselineDropDamage = 10.0f;
    [SerializeField] private float dropDamageExponent = 2.0f;
    
    [Header("General Stamina")]
    public float curStamina;                // our current stamina
    public float maxStamina;                // our maximum stamina
    public float staminaRegenRate = 10f;               // stamina regen per second
    public float staminaRegenDelay = 2f;               // delay before stamina starts regenerating after stopping sprint
    
    [Header("Climbing Stamina")]
    public float staminaDegenRateClimbing;           // how fast we degenerate stamina while climbing
    public float staminaThresholdFallWhileClimbing;          // how low stamina must be to stop climbing
    // public float staminaThresholdStartClimbing;            // how high stamina must be to start climbing
    public float staminaRecoveryWhileDropping = 30f;    // how much stamina we recover while dropping
    
    [Header("Sprint Stamina")]
    public float staminaThresholdStartSprinting = 20f; // minimum stamina to start sprinting
    public float staminaThresholdStopSprinting = 5f;  // stamina level that stops sprinting
    public float staminaDegenRateSprinting = 15f;      // stamina drain per second while sprinting
    
    // Sprint state
    private bool _isSprinting = false;
    private bool _canSprint = true;
    private float _staminaRegenTimer = 0f;
    
    // Runtime state
    public Vector2 FacingDirection { get; private set; } = Vector2.down;
    private Vector2 MoveInput { get; set; }
    
    // --- New: Climb Jump ---
    [Header("Climb Jump")]
    [Tooltip("Key used to trigger a climb jump.")]
    public KeyCode climbJumpKey = KeyCode.Space;
    public float climbJumpHeight = 2.5f;      // world units up
    public float climbJumpDuration = 0.25f;   // seconds
    public float climbJumpStaminaCost = 10f;  // consumed on jump
    public float climbJumpRecoveryTime = 0.75f; 
    private bool _isClimbJumping;
    private float _climbJumpTimer = 0f;
    private Vector2 _climbJumpVel;            // computed constant velocity for jump arc
    private float _climbJumpCooldown = 0f; 
    private void Awake()
    {
        // get components
        if(player == null)
            player = FindObjectOfType<Player>();
        
        if(rig == null)
            rig = GetComponent<Rigidbody2D>();
        
        if(spriteRendererComponent == null)
            spriteRendererComponent = GetComponent<SpriteRenderer>();
        
        if(playerUI == null)
            playerUI = FindObjectOfType<PlayerUI>();

        // 2D top-down defaults
        if (rig != null)
        {
            rig.gravityScale = 0f;
            rig.freezeRotation = true;
        }

        // default facing
        FacingDirection = Vector2.down;
        if (spriteRendererComponent != null && downSprite != null)
            spriteRendererComponent.sprite = downSprite;
            
        // Initialize stamina
        curStamina = maxStamina;
    }

    private void Update()
    {
        HandleSprint();
        HandleClimbing();
        HandleDropping();
        
        if (_climbJumpCooldown > 0f)
            _climbJumpCooldown -= Time.deltaTime;
        
        HandleClimbJumpInput();   
        HandleClimbJumpTick();
        HandleStaminaRegeneration();
        HandleStaminaRegeneration();
        Move();
        playerUI.UpdateStaminaBar();
    }

    private void HandleClimbJumpInput()
    {
        // Only from a climb, pressing up, not dropping, enough stamina
        if (isClimbing && !isDropping && !_isClimbJumping && _climbJumpCooldown <= 0f &&  
            (Input.GetKeyDown(climbJumpKey)) && MoveInput.y > 0f &&
            curStamina >= climbJumpStaminaCost)
        {
            // spend stamina
            curStamina -= climbJumpStaminaCost;

            // compute constant velocity needed to travel 'climbJumpHeight' in 'climbJumpDuration'
            float vy = climbJumpHeight / climbJumpDuration; // gravityScale==0 outside drops, so constant
            _climbJumpVel = new Vector2(0f, vy);

            // start state
            _isClimbJumping = true;
            _climbJumpTimer = 0f;

            // ensure physics doesn’t fight us
            if (rig != null)
            {
                rig.gravityScale = 0f;        // keep top-down behaviour
                rig.velocity = Vector2.zero;  // clear old velocity
            }

            if (runDebugs) Debug.Log("[HumanMovement] ClimbJump started");
        }
    }

    private void HandleClimbJumpTick()
    {
        if (!_isClimbJumping) return;

        _climbJumpTimer += Time.deltaTime;

        // drive the body upward at constant speed
        if (rig != null)
            rig.velocity = _climbJumpVel;

        // end after duration
        if (_climbJumpTimer >= climbJumpDuration)
        {
            _isClimbJumping = false;

            // stop vertical motion; remain in climb
            if (rig != null) rig.velocity = Vector2.zero;

            // start cooldown
            _climbJumpCooldown = climbJumpRecoveryTime;
            
            if (runDebugs) Debug.Log("[HumanMovement] ClimbJump ended");
        }
    }

    private void HandleSprint()
    {
        bool sprintInput = Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift);
        bool isMoving = MoveInput != Vector2.zero;
        bool isOnGround = !isClimbing;
        
        // Check if we can start sprinting
        if (sprintInput && isMoving && isOnGround && _canSprint && curStamina >= staminaThresholdStartSprinting)
        {
            if (!_isSprinting)
            {
                _isSprinting = true;
                _staminaRegenTimer = 0f; // Reset regen timer when starting to sprint
            }
        }
        else
        {
            _isSprinting = false;
        }
        
        // Handle stamina drain while sprinting
        if (_isSprinting)
        {
            curStamina -= staminaDegenRateSprinting * Time.deltaTime;
            curStamina = Mathf.Max(curStamina, 0f);
            
            // Stop sprinting if stamina is too low
            if (curStamina <= staminaThresholdStopSprinting)
            {
                _isSprinting = false;
                _canSprint = false;
                _staminaRegenTimer = 0f;
            }
        }
        
        // Re-enable sprint capability when stamina is sufficient
        if (!_canSprint && curStamina >= staminaThresholdStartSprinting)
        {
            _canSprint = true;
        }
    }

    private void HandleClimbing()
    {
        if (_isClimbJumping) return; // skip drain & fall checks during jump
        
        // Handle climbing stamina drain
        if (isClimbing && MoveInput.y > 0f) // Moving upwards while climbing
        {
            curStamina -= staminaDegenRateClimbing * Time.deltaTime;
            curStamina = Mathf.Max(curStamina, 0f);
            _staminaRegenTimer = 0f; // Reset regen timer when climbing upwards
        }
        
        // Check if player should fall while climbing - only when actively moving upwards
        if (isClimbing && MoveInput.y > 0f && curStamina <= staminaThresholdFallWhileClimbing && 
            !isDropping && _dropCooldownTimer <= 0f)
        {
            StartDropping();
        }
    }
    
    private void HandleDropping()
    {
        // Update drop cooldown timer
        if (_dropCooldownTimer > 0f)
        {
            _dropCooldownTimer -= Time.deltaTime;
        }
        
        if (isDropping)
        {
            _dropDurationElapsed += Time.deltaTime;
            
            // Stop dropping when duration passes
            if (_dropDurationElapsed >= timeToRecoverGripWhileDropping)
            {
                StopDropping();
            }
        }
    }
    
    private void StartDropping()
    {
        isDropping = true;
        _dropDurationElapsed = 0f;
        
        // Enable gravity for falling with higher scale for faster drop
        if (rig != null)
        {
            rig.gravityScale = 3f; // Increased from 1f for faster falling
            // Clear any existing velocity to ensure clean fall
            rig.velocity = Vector2.zero;
        }
        
        // Force player out of climbing state
        isClimbing = false;
    }
    
    private void StopDropping()
    {
        isDropping = false;
        _dropDurationElapsed = 0f;
        _dropCooldownTimer = DropCooldown; // Start cooldown period
        
        // Disable gravity back to normal 2D top-down
        if (rig != null)
        {
            rig.gravityScale = 0f;
        }
        
        // Recover some stamina while dropping
        curStamina += staminaRecoveryWhileDropping;
    }

    private void CauseFallDamage()
    {
        // Use actual fall duration instead of the max drop duration
        // The damage increases exponentially based on how long they actually fell
        float fallDamage = baselineDropDamage * Mathf.Pow(_dropDurationElapsed, dropDamageExponent);
        
        if(runDebugs) Debug.Log($"[HumanMovement] CauseFallDamage: fallDamage = {fallDamage}, _dropDurationElapsed = {_dropDurationElapsed}, player = {player}");
        
        if (player != null)
        {
            player.TakeDamage(fallDamage);
            if(runDebugs) Debug.Log($"[HumanMovement] Damage applied successfully. Player HP: {player.curHp}");
        }
        else
        {
            Debug.LogError("[HumanMovement] Player reference is null! Cannot apply fall damage.");
        }
        
        StopDropping();
        isClimbing = false;
    }

    
    private void HandleStaminaRegeneration()
    {
        // Only regenerate stamina if not currently sprinting
        if (!_isSprinting && !isClimbing && !isDropping)
        {
            _staminaRegenTimer += Time.deltaTime;
            
            // Start regenerating after delay
            if (_staminaRegenTimer >= staminaRegenDelay)
            {
                curStamina += staminaRegenRate * Time.deltaTime;
                curStamina = Mathf.Min(curStamina, maxStamina);
            }
        }
        else
        {
            _staminaRegenTimer = 0f;
        }
    }

    private void Move()
    {
        // Don't process movement input while dropping
        if (isDropping) return;
        
        // --- INPUT ---
        float x = Input.GetAxisRaw("Horizontal");
        float y = Input.GetAxisRaw("Vertical");
        MoveInput = new Vector2(x, y);

        // normalize diagonals for consistent speed
        if (MoveInput.sqrMagnitude > 1f) MoveInput.Normalize();

        // DEADZONE: ignore tiny joystick drift
        const float deadzone = 0.1f;
        var mi = MoveInput;
        if (Mathf.Abs(mi.x) < deadzone) mi.x = 0f;
        if (Mathf.Abs(mi.y) < deadzone) mi.y = 0f;
        MoveInput = mi;

        // --- FACING + SPRITE ---
        if (isClimbing)
        {
            // While climbing: always face "up"
            FacingDirection = Vector2.up;
            if (spriteRendererComponent) spriteRendererComponent.sprite = upSprite ? upSprite : spriteRendererComponent.sprite;
        }
        else
        {
            if (MoveInput != Vector2.zero)
            {
                if (Mathf.Abs(MoveInput.x) > Mathf.Abs(MoveInput.y))
                    FacingDirection = (MoveInput.x > 0f) ? Vector2.right : Vector2.left;
                else
                    FacingDirection = (MoveInput.y > 0f) ? Vector2.up : Vector2.down;
            }
            UpdateSpriteDirection();
        }

        // --- SPEED ---
        float speed = isClimbing ? Mathf.Min(climbSpeed, moveSpeed) : moveSpeed;
        
        // Apply sprint multiplier if sprinting
        if (_isSprinting && !isClimbing)
        {
            speed *= sprintMultiplier;
        }

        if (rig)
        {
            if (_isClimbJumping)
            {
                // Do nothing here; jump tick controls velocity this frame.
                return;
            }

            rig.velocity = MoveInput * speed;   // normal path
        }
        
    }

    // change sprite depending on facing
    private void UpdateSpriteDirection()
    {
        if (spriteRendererComponent == null) return;
        if      (FacingDirection == Vector2.up)    spriteRendererComponent.sprite = upSprite;
        else if (FacingDirection == Vector2.down)  spriteRendererComponent.sprite = downSprite;
        else if (FacingDirection == Vector2.left)  spriteRendererComponent.sprite = leftSprite;
        else if (FacingDirection == Vector2.right) spriteRendererComponent.sprite = rightSprite;
    }

    private void OnTriggerEnter2D(Collider2D other) { StartCoroutine(HandleEnterSurface(other)); }
    private void OnTriggerStay2D(Collider2D other)  { HandleContinuousSurface(other); }

    private IEnumerator HandleEnterSurface(Collider2D other)
    {
        if(runDebugs) Debug.Log($"[HumanMovement] HandleEnterSurface: Entering surface with layer: {other.gameObject.layer}, isDropping: {isDropping}, _dropDurationElapsed: {_dropDurationElapsed}");
        
        yield return new WaitForSeconds(layerTransitionDelay);
        
        if(other == null)
            yield break;
        
        int layer = other.gameObject.layer;
        if (layer == LayerMask.NameToLayer("Ground"))
        {
            if(runDebugs) Debug.Log($"[HumanMovement] Ground collision - isDropping: {isDropping}, _dropDurationElapsed: {_dropDurationElapsed}, threshold: {dropDurationDamageThreshold}");
            
            if (isDropping)
            {
                // Damage player if they fall too long - use actual elapsed time, not max duration
                if(_dropDurationElapsed > dropDurationDamageThreshold)
                {
                    if(runDebugs) Debug.Log($"[HumanMovement] Calling CauseFallDamage - fall duration: {_dropDurationElapsed}");
                    CauseFallDamage();
                }
                else
                {
                    if(runDebugs) Debug.Log($"[HumanMovement] Fall too short for damage: {_dropDurationElapsed} <= {dropDurationDamageThreshold}");
                    StopDropping(); // Still need to stop dropping even without damage
                }
            }
            else
            {
                if(runDebugs) Debug.Log("[HumanMovement] Player hit ground but wasn't dropping");
            }
            
            isClimbing = false;
            yield break; // exits the whole coroutine early
        }
        if (layer == LayerMask.NameToLayer("Climbable"))
        {
            isClimbing = true;
        }
    }
    
    private void HandleContinuousSurface(Collider2D other)
    {
        int layer = other.gameObject.layer;
        if (layer == LayerMask.NameToLayer("Ground"))
        {
            isClimbing = false;
            return;
        }
        if (layer == LayerMask.NameToLayer("Climbable"))
        {
            isClimbing = true;
        }
    }
    
    // Add to HumanMovement.cs (public helper to be called by external hazards)
    public void ApplyRockHit(float rockDamage, float? overrideFallThreshold = null)
    {
        // 1) Apply damage to Player
        if (player != null && rockDamage > 0f)
            player.TakeDamage(rockDamage);

        // 2) Drop stamina to (just below) the threshold that causes a fall
        // Use the class threshold unless an override is provided.
        float target = overrideFallThreshold ?? staminaThresholdFallWhileClimbing;

        // Put stamina a hair below the threshold to ensure any threshold checks trigger.
        curStamina = Mathf.Min(curStamina, Mathf.Max(0f, target - 0.1f));

        // 3) If we’re climbing (or in the “wall contact” state), force the drop immediately.
        // Your existing HandleClimbing() only triggers when moving up, so we call StartDropping() directly.
        if (isClimbing && !isDropping && _dropCooldownTimer <= 0f)
        {
            // Reuse the existing fall logic
            // (StartDropping is private; since this method is in the same class, it can call it.)
            StartDropping();
        }
    }
    
}