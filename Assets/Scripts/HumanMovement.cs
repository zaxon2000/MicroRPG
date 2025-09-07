using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
[RequireComponent(typeof(SpriteRenderer))]
public class HumanMovement : MonoBehaviour
{
    [Header("Movement")]
    public float moveSpeed = 5f;
    [Tooltip("Speed while climbing on the MountainFace_Tilemap (Climbable layer). Must be < moveSpeed.")]
    public float climbSpeed = 2.6f;
    
    [Header("Sprint")]
    public float sprintMultiplier = 2f;             // multiplier for sprint speed
    public float staminaThresholdStartSprinting = 20f; // minimum stamina to start sprinting
    public float staminaThresholdStopSprinting = 5f;  // stamina level that stops sprinting
    public float staminaDegenRateSprinting = 15f;      // stamina drain per second while sprinting
    
    [Header("Sprites")]
    public Sprite downSprite;
    public Sprite upSprite;
    public Sprite leftSprite;
    public Sprite rightSprite;

    [Header("Climb State")]
    public bool isClimbing;
    public float staminaDegenRateClimbing;           // how fast we degenerate stamina while climbing
    public float staminaThresholdFallWhileClimbing;          // how low stamina must be to stop sprinting
    public float staminaThresholdStartClimbing;            // how high stamina must be to start sprinting

    [Header("Stamina")]
    public float curStamina;                // our current stamina
    public float maxStamina;                // our maximum stamina
    public float staminaRegenRate = 10f;               // stamina regen per second
    public float staminaRegenDelay = 2f;               // delay before stamina starts regenerating after stopping sprint
    
    // Sprint state
    private bool _isSprinting = false;
    private bool _canSprint = true;
    private float _staminaRegenTimer = 0f;
    
    // Runtime state
    public Vector2 FacingDirection { get; private set; } = Vector2.down;
    public Vector2 MoveInput { get; private set; }

    // components
    private Rigidbody2D _rig;
    private SpriteRenderer _spriteRendererComponent;
    private PlayerUI _playerUI;

    private void Awake()
    {
        _rig = GetComponent<Rigidbody2D>();
        _spriteRendererComponent = GetComponent<SpriteRenderer>();
        _playerUI = FindObjectOfType<PlayerUI>();

        // 2D top-down defaults
        if (_rig != null)
        {
            _rig.gravityScale = 0f;
            _rig.freezeRotation = true;
        }

        // default facing
        FacingDirection = Vector2.down;
        if (_spriteRendererComponent != null && downSprite != null)
            _spriteRendererComponent.sprite = downSprite;
            
        // Initialize stamina
        curStamina = maxStamina;
    }

    private void Update()
    {
        HandleSprint();
        HandleClimbing();
        HandleStaminaRegeneration();
        Move();
        _playerUI.UpdateStaminaBar();
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
        // Handle climbing stamina drain
        if (isClimbing && MoveInput.y > 0f) // Moving upwards while climbing
        {
            curStamina -= staminaDegenRateClimbing * Time.deltaTime;
            curStamina = Mathf.Max(curStamina, 0f);
            _staminaRegenTimer = 0f; // Reset regen timer when climbing upwards
        }
    }
    
    private void HandleStaminaRegeneration()
    {
        // Only regenerate stamina if not currently sprinting
        if (!_isSprinting && !isClimbing)
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
            if (_spriteRendererComponent) _spriteRendererComponent.sprite = upSprite ? upSprite : _spriteRendererComponent.sprite;
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

        // --- APPLY VELOCITY ---
        if (_rig)
            _rig.velocity = MoveInput * speed;
    }

    // change sprite depending on facing
    private void UpdateSpriteDirection()
    {
        if (_spriteRendererComponent == null) return;
        if      (FacingDirection == Vector2.up)    _spriteRendererComponent.sprite = upSprite;
        else if (FacingDirection == Vector2.down)  _spriteRendererComponent.sprite = downSprite;
        else if (FacingDirection == Vector2.left)  _spriteRendererComponent.sprite = leftSprite;
        else if (FacingDirection == Vector2.right) _spriteRendererComponent.sprite = rightSprite;
    }

    private void OnTriggerEnter2D(Collider2D other) { HandleSurface(other); }
    private void OnTriggerStay2D(Collider2D other)  { HandleSurface(other); }

    private void HandleSurface(Collider2D other)
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
    
    // Public properties for other scripts to access sprint state
    public bool IsSprinting => _isSprinting;
    public bool CanSprint => _canSprint;
}