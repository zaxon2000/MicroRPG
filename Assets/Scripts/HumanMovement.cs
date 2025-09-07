using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
[RequireComponent(typeof(SpriteRenderer))]
public class HumanMovement : MonoBehaviour
{
    [Header("Movement")]
    public float moveSpeed = 5f;
    [Tooltip("Speed while climbing on the MountainFace_Tilemap (Climbable layer). Must be < moveSpeed.")]
    public float climbSpeed = 2.6f;

    [Header("Sprites")]
    public Sprite downSprite;
    public Sprite upSprite;
    public Sprite leftSprite;
    public Sprite rightSprite;

    [Header("Climb State")]
    public bool isClimbing;

    [Header("Stamina")]
    public float curStamina;                // our current level
    public float maxStamina;                   // our current experience points
    
    
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
    }

    private void Update()
    {
        Move();
    }

    public void Move()
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
}