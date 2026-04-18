using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

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

    [Header("Push Stamina")]
    [Tooltip("Stamina drain per second while actively pushing a boulder.")]
    public float staminaDegenRatePushing = 12f;
    
    // Sprint state
    private bool _isSprinting = false;
    private bool _canSprint = true;
    private float _staminaRegenTimer = 0f;

    // Push state
    private bool _isPushing = false;
    
    // Runtime state
    public Vector2 FacingDirection { get; private set; } = Vector2.down;

    /// <summary>The velocity the player intends to move at, before physics resolves collisions.</summary>
    public Vector2 IntendedVelocity { get; private set; }
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

    [Header("Grappling Hook")]
    public GameObject hookPrefab;
    private GameObject curHook;
    private bool ropeActive;
    public float swingForce = 10f;

    [Header("Gravity")]
    [Tooltip("Single source of truth for the player's Rigidbody2D gravity. " +
             "When true, the rigidbody runs with gravityScale = " +
             "gravityScaleWhenOn; when false, gravityScale = 0 (top-down). " +
             "Mirrored to the rigidbody every Update, so toggling at runtime " +
             "(Inspector or code) takes effect immediately. Every gravity-on " +
             "context (bridge walking, dropping, grappling) flips this flag " +
             "instead of writing gravityScale directly.")]
    public bool gravityOn;
    [Tooltip("Gravity scale applied when gravityOn is true. Used for bridge " +
             "walking, drop falls, and grappling-hook swings. Single value " +
             "rather than per-context fields so a fall feels consistent " +
             "across every situation.")]
    public float gravityScaleWhenOn = 3f;

    [Header("Bridge Walking")]
    [Tooltip("Stamina recovered per second while standing on a bridge block. " +
             "Bypasses the normal staminaRegenDelay so a bridge doubles as a " +
             "rest spot.")]
    public float bridgeStaminaRegenRate = 20f;
    [Tooltip("On first contact with a bridge from BESIDE/BELOW (e.g. climbing " +
             "the wall and bumping the tack bolted to it), snap the player to " +
             "this many world units above the contacted piece so gravity then " +
             "lands them ON the bridge instead of letting them slide back down " +
             "the wall. Set ~half the player's collider height. Pure walk-on " +
             "from above is unaffected (the snap only fires when player.y <= bw.y).")]
    public float bridgeSnapOffset = 0.5f;

    // Tracks which BridgeWalkable surfaces the player is CURRENTLY in contact
    // with. Used only by the snap-to-top logic on initial engagement (we need
    // to find the closest piece to lift the player onto). The set no longer
    // drives _isOnBridge — engagement is sticky, see DisengageBridge().
    private readonly HashSet<BridgeWalkable> _bridgesWalkingOn = new();
    // Sticky bridge engagement. Once true, stays true (and gravity stays on)
    // until DisengageBridge() is called explicitly — by Ground contact, an
    // UP press, or a drop start. This is the fix for the "floating above the
    // sag" bug: walking off the high tack drops contact for many frames before
    // the player reaches the next sagging plank, but gravity must persist
    // across that gap so the player actually FALLS onto the next plank
    // instead of gliding horizontally past it.
    private bool _isOnBridge;
    // Latched true the moment the player presses UP to release the bridge.
    // Stays true as long as UP is held (cleared in Move() when y returns to 0)
    // and gates re-engagement so the player can climb away cleanly without
    // the next OnCollisionStay tick yanking them back onto the planks.
    // Releasing UP — even momentarily — re-arms engagement, so a brief
    // accidental tap doesn't permanently disable the bridge.
    private bool _recentlyReleasedBridge;
    // Tracks the last gravityOn value so SyncGravityScale can zero residual
    // y-velocity on a true→false transition (otherwise inertia from the fall
    // would drift the player in top-down mode after leaving the bridge).
    private bool _lastGravityOn;
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
            gravityOn = false;
            rig.gravityScale = 0f;
            rig.freezeRotation = true;
        }
        _lastGravityOn = gravityOn;

        // default facing
        FacingDirection = Vector2.down;
        if (spriteRendererComponent != null && downSprite != null)
            spriteRendererComponent.sprite = downSprite;
            
        // Initialize stamina
        curStamina = maxStamina;
    }

    private void Update()
    {
        // Freeze movement while dialogue is active
        if (DialogueManager.Instance != null && DialogueManager.Instance.IsDialogueActive)
        {
            if (rig != null) rig.linearVelocity = Vector2.zero;
            return;
        }

        HandleSprint();
        HandleClimbing();
        HandleDropping();
        HandleGrapple();
        
        if (_climbJumpCooldown > 0f)
            _climbJumpCooldown -= Time.deltaTime;
        
        HandleClimbJumpInput();
        HandleClimbJumpTick();
        HandleStaminaRegeneration();
        HandleBridgeStaminaRefill();
        Move();
        SyncGravityScale();
        playerUI.UpdateStaminaBar();
    }

    /// <summary>
    /// Mirror <see cref="gravityOn"/> to <see cref="Rigidbody2D.gravityScale"/>.
    /// Called once per Update after Move() so any system that toggled gravityOn
    /// this frame (bridge engage/disengage, drop start/stop, grapple
    /// attach/detach, Inspector edit at runtime) takes effect on the next
    /// physics step. On a true→false transition we also zero residual
    /// y-velocity so the player doesn't drift downward in top-down mode after
    /// gravity is removed mid-fall.
    /// </summary>
    private void SyncGravityScale()
    {
        if (rig == null) return;

        float target = gravityOn ? gravityScaleWhenOn : 0f;
        if (!Mathf.Approximately(rig.gravityScale, target))
            rig.gravityScale = target;

        if (_lastGravityOn && !gravityOn)
        {
            Vector2 v = rig.linearVelocity;
            v.y = 0f;
            rig.linearVelocity = v;
        }
        _lastGravityOn = gravityOn;
    }

    /// <summary>
    /// Bridges double as a rest spot — while standing on one, stamina refills
    /// at <see cref="bridgeStaminaRegenRate"/> with no <see cref="staminaRegenDelay"/>
    /// gate. This stacks with the normal regen path (which is also active when
    /// the player is idle on the bridge) so resting on the bridge is the
    /// fastest way to recover.
    /// </summary>
    private void HandleBridgeStaminaRefill()
    {
        if (!_isOnBridge) return;
        curStamina += bridgeStaminaRegenRate * Time.deltaTime;
        curStamina = Mathf.Min(curStamina, maxStamina);
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
                gravityOn = false;            // keep top-down behaviour
                rig.linearVelocity = Vector2.zero;  // clear old velocity
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
            rig.linearVelocity = _climbJumpVel;

        // end after duration
        if (_climbJumpTimer >= climbJumpDuration)
        {
            _isClimbJumping = false;

            // stop vertical motion; remain in climb
            if (rig != null) rig.linearVelocity = Vector2.zero;

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

        // Enable gravity for falling. Drops always disengage the bridge —
        // a falling player has no business still being "on" the planks.
        DisengageBridge();
        gravityOn = true;
        if (rig != null)
        {
            // Clear any existing velocity to ensure clean fall
            rig.linearVelocity = Vector2.zero;
        }

        // Force player out of climbing state
        isClimbing = false;
    }

    private void StopDropping()
    {
        isDropping = false;
        _dropDurationElapsed = 0f;
        _dropCooldownTimer = DropCooldown; // Start cooldown period

        // Disable gravity back to normal 2D top-down, unless rope is active
        // (rope swings keep gravity on for the swing physics).
        gravityOn = ropeActive;

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

    
    private void HandleGrapple()
    {
        // Don't fire the hook when clicking on UI elements (inventory, character panel, etc.)
        if (EventSystem.current != null && EventSystem.current.IsPointerOverGameObject())
            return;

        // Only respond to mouse clicks while the player has the grapple hook tool selected.
        // (Switching modes mid-swing also tears down any active rope — see below.)
        bool grappleToolActive = ToolModeManager.Instance == null
            || ToolModeManager.Instance.Mode == ToolModeManager.ToolMode.GrappleHook;

        // If the tool was switched off mid-swing, retract the active rope so the
        // player isn't left dangling on a hook they can no longer control.
        if (!grappleToolActive && ropeActive)
        {
            DetachGrapple();
            return;
        }

        if (!grappleToolActive)
            return;

        if (Input.GetMouseButtonDown(0))
        {
            if (!ropeActive)
            {
                Vector2 destiny = Camera.main.ScreenToWorldPoint(Input.mousePosition);
                curHook = Instantiate(hookPrefab, transform.position, Quaternion.identity);

                // Link the hook to this player
                var rope = curHook.GetComponent<RopeScript>();
                rope.destiny = destiny;
                rope.player = this.gameObject;

                ropeActive = true;
                isClimbing = false; // Disable wall climbing while rope is active
                // Grappling implies leaving the bridge.
                DisengageBridge();

                // A grappling hook requires gravity to swing.
                gravityOn = true;
            }
            else
            {
                DetachGrapple();
            }
        }
    }

    /// <summary>
    /// Tear down the active grappling hook (if any) and restore top-down physics.
    /// Called both from a normal player click-to-release and when the user switches
    /// the active tool away from "Grapple Hook" mid-swing.
    /// </summary>
    private void DetachGrapple()
    {
        if (!ropeActive) return;

        if (curHook != null)
            Destroy(curHook);
        curHook = null;
        ropeActive = false;

        // Return to top-down physics (no gravity) if not dropping
        if (!isDropping)
        {
            gravityOn = false;
            // Stop the swing instantly so the player doesn't drift after release.
            if (rig != null) rig.linearVelocity = Vector2.zero;
        }
    }

    private void HandleStaminaRegeneration()
    {
        // Only regenerate stamina if not currently exerting
        if (!_isSprinting && !isClimbing && !isDropping && !_isPushing && !ropeActive)
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
        // Don't process normal movement input while dropping
        if (isDropping) return;

        // --- INPUT ---
        float x = Input.GetAxisRaw("Horizontal");
        float y = Input.GetAxisRaw("Vertical");
        MoveInput = new Vector2(x, y);

        // Pressing UP while on the bridge releases it so the existing Climbable
        // climb logic (HandleContinuousSurface → isClimbing = true) can re-engage
        // against an adjacent wall. The latch below also blocks re-engagement
        // (see EngageBridge) for as long as UP is held, so the player can
        // comfortably climb away without gravity yanking them back onto the
        // planks on the next physics tick.
        if (_isOnBridge && y > 0f)
        {
            DisengageBridge();
            _recentlyReleasedBridge = true;
        }
        // Releasing UP — even momentarily — re-arms engagement. A brief tap
        // of UP doesn't permanently disable the bridge; you have to keep
        // climbing for the gate to stay shut.
        else if (y <= 0f)
        {
            _recentlyReleasedBridge = false;
        }

        if (ropeActive)
        {
            // SWING PHYSICS
            if (rig)
            {
                if (Mathf.Abs(x) > 0.1f)
                {
                    // SWINGING: Apply horizontal force, disable climbing state
                    isClimbing = false;
                    rig.AddForce(Vector2.right * x * swingForce);
                    FacingDirection = (x > 0f) ? Vector2.right : Vector2.left;
                }
                else if (Mathf.Abs(y) > 0.1f)
                {
                    // CLIMBING THE ROPE: Move vertically, zero horizontal velocity, enable climbing state for stamina
                    isClimbing = true;
                    rig.linearVelocity = new Vector2(0f, y * climbSpeed);
                    FacingDirection = Vector2.up;
                }
                else
                {
                    // HANGING: Enable climbing state to face the rope, but no vertical velocity override
                    isClimbing = true;
                    FacingDirection = Vector2.up;
                }
                
                UpdateSpriteDirection();
            }
            return;
        }

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

            if (_isOnBridge)
            {
                // Preserve y-velocity so gravity (scale = bridgeGravityScale)
                // can pull the player onto / along the planks. Setting the
                // full vector here would zero the gravity-induced fall every
                // frame, so the player would float just above the bridge
                // instead of standing on it — and a sagging bridge would
                // leave them stranded in mid-air.
                rig.linearVelocity = new Vector2(MoveInput.x * speed, rig.linearVelocity.y);
                IntendedVelocity = new Vector2(MoveInput.x * speed, 0f);
            }
            else
            {
                rig.linearVelocity = MoveInput * speed;   // normal path
                IntendedVelocity = MoveInput * speed;
            }
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

    /// <summary>
    /// Trigger-side bridge contact tracking. Removes the piece from the
    /// "currently in contact" set, but does NOT disengage the bridge — the
    /// engagement is sticky and only ends via <see cref="DisengageBridge"/>
    /// (Ground contact, UP press, or drop start). This is the fix for the
    /// "floating above the sag" bug: the bridge sags away from the player
    /// between planks, contact drops for many frames, but gravity must
    /// persist so the player FALLS to the next plank instead of gliding
    /// past it horizontally.
    /// </summary>
    private void OnTriggerExit2D(Collider2D other)
    {
        var bw = other.GetComponent<BridgeWalkable>();
        if (bw != null) _bridgesWalkingOn.Remove(bw);
    }

    /// <summary>
    /// Fires on the Dynamic player body whenever its non-trigger CapsuleCollider2D
    /// is in contact with any non-trigger collider. Used to drive boulder pushes —
    /// Dynamic bodies always receive OnCollisionStay2D regardless of the other
    /// body's Kinematic contact flags.
    /// </summary>
    private void OnCollisionStay2D(Collision2D collision)
    {
        // -- Boulder pushing ---------------------------------------------------
        Boulder boulder = collision.gameObject.GetComponent<Boulder>();
        if (boulder != null && rig != null)
        {
            // Need stamina to push.
            if (curStamina <= 0f)
            {
                _isPushing = false;
            }
            else
            {
                boulder.TryPush(rig.position, IntendedVelocity);

                // Drain stamina while actively in contact and pressing toward the boulder.
                Vector2 toBoulder = ((Vector2)boulder.transform.position - rig.position).normalized;
                if (Vector2.Dot(IntendedVelocity, toBoulder) > 0f)
                {
                    _isPushing = true;
                    curStamina -= staminaDegenRatePushing * Time.deltaTime;
                    curStamina = Mathf.Max(curStamina, 0f);
                    _staminaRegenTimer = 0f;
                }
                else
                {
                    _isPushing = false;
                }
            }
        }

        // -- Bridge engagement ------------------------------------------------
        // Use Stay (not Enter) so the player still engages if a bridge gets
        // built underneath them mid-frame, and so two-block straddles always
        // have BOTH blocks in the set even if one Enter event gets missed.
        var bridgePiece = collision.gameObject.GetComponent<BridgeWalkable>();
        if (bridgePiece != null) EngageBridge(bridgePiece);
    }

    private void OnCollisionExit2D(Collision2D collision)
    {
        if (collision.gameObject.GetComponent<Boulder>() != null)
            _isPushing = false;

        // Same as OnTriggerExit2D — track set membership only, never
        // disengage. See the doc comment there for the "floating above
        // the sag" reasoning.
        var bw = collision.gameObject.GetComponent<BridgeWalkable>();
        if (bw != null) _bridgesWalkingOn.Remove(bw);
    }

    /// <summary>
    /// Add the contacted BridgeWalkable to the in-contact set, and on first
    /// engagement flip gravity on, drop climb state, and snap-to-top if we
    /// approached from beside or below. Subsequent calls while already
    /// engaged are no-ops apart from set bookkeeping.
    ///
    /// Note: there is intentionally no "must be above the block" check at the
    /// caller. The common entry path is climbing the wall and bumping the
    /// tack/anchor block — player.y is roughly EQUAL to block.y at contact,
    /// not strictly greater. Filtering on "above" would skip this case
    /// entirely. Instead the snap below handles the geometry: if the player
    /// is at or below the closest contact piece, we lift them onto it.
    /// </summary>
    private void EngageBridge(BridgeWalkable bw)
    {
        if (ropeActive || isDropping) return;
        // Skip while UP is still held from a release. Cleared in Move() the
        // moment vertical input drops back to 0, so a brief tap doesn't
        // permanently disable bridge engagement.
        if (_recentlyReleasedBridge) return;
        if (bw == null) return;

        _bridgesWalkingOn.Add(bw);
        if (_isOnBridge) return;     // already engaged; nothing else to do.

        _isOnBridge = true;
        gravityOn = true;
        isClimbing = false;
        if (rig == null) return;

        // Snap-to-top: if engagement happened from BESIDE or BELOW the closest
        // contacted piece (e.g. the player was climbing the wall and bumped
        // the anchor tack bolted to it), simply enabling gravity would slide
        // the player back down the wall — they'd never end up on the planks.
        // Lift the player to bridgeSnapOffset above that piece so the next
        // physics tick lands them ON it. Pure walk-on-from-above is
        // unaffected (the y-check fails).
        BridgeWalkable closest = null;
        float closestDistSq = float.MaxValue;
        foreach (var piece in _bridgesWalkingOn)
        {
            if (piece == null) continue;
            float d = ((Vector2)(piece.transform.position - transform.position)).sqrMagnitude;
            if (d < closestDistSq)
            {
                closestDistSq = d;
                closest = piece;
            }
        }
        if (closest != null && transform.position.y <= closest.transform.position.y)
        {
            // Preserve the player's x; only lift them onto the surface.
            Vector3 snap = transform.position;
            snap.y = closest.transform.position.y + bridgeSnapOffset;
            transform.position = snap;
            rig.linearVelocity = Vector2.zero;
        }

        if (runDebugs) Debug.Log("[HumanMovement] Bridge engaged");
    }

    /// <summary>
    /// Explicit bridge release. Called by the three "the player has left the
    /// bridge for real" paths: stepping onto Ground, pressing UP to climb a
    /// wall, or starting a drop. Clears the contact set, flips gravity off
    /// (back to top-down), and zeroes residual y-velocity so the player
    /// doesn't drift downward in top-down mode.
    ///
    /// Critically, contact loss alone (OnCollision/TriggerExit) does NOT
    /// call this — engagement is sticky across the gap between sagging
    /// planks. Without stickiness, walking off the high tack would drop
    /// gravity before the player could fall to the next plank, and they'd
    /// glide horizontally past the sag (the original bug).
    /// </summary>
    public void DisengageBridge()
    {
        if (!_isOnBridge) return;
        _bridgesWalkingOn.Clear();
        _isOnBridge = false;
        gravityOn = ropeActive;   // rope swings keep gravity on for swing physics.
        if (rig != null) rig.linearVelocity = Vector2.zero;
        if (runDebugs) Debug.Log("[HumanMovement] Bridge released");
    }

    private IEnumerator HandleEnterSurface(Collider2D other)
    {
        if(runDebugs) Debug.Log($"[HumanMovement] HandleEnterSurface: Entering surface with layer: {other.gameObject.layer}, isDropping: {isDropping}, _dropDurationElapsed: {_dropDurationElapsed}");
        
        yield return new WaitForSeconds(layerTransitionDelay);
        
        if(other == null || ropeActive) // Disable wall interaction while rope is active
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

            // Stepping onto Ground is one of the three explicit "leave the
            // bridge for real" paths — gravity goes back off, top-down
            // resumes. (Same call lives in HandleContinuousSurface for
            // continued contact across frames.)
            DisengageBridge();
            isClimbing = false;
            yield break; // exits the whole coroutine early
        }
        if (layer == LayerMask.NameToLayer("Climbable"))
        {
            // Same intent gate as HandleContinuousSurface — climb engagement
            // requires the player to be pressing UP at the moment the
            // post-delay tick fires. Without this, the layerTransitionDelay
            // path could still slip the player into climb mode unsolicited.
            if (Input.GetAxisRaw("Vertical") > 0f)
                isClimbing = true;
        }
    }

    private void HandleContinuousSurface(Collider2D other)
    {
        if (ropeActive) return; // Disable wall interaction while rope is active

        // BridgeWalkable behaves like Ground for climb-state purposes AND
        // engages bridge gravity. Routing engagement through the trigger
        // path (in addition to OnCollisionStay2D) catches cases where the
        // trigger collider's larger reach overlaps a plank a tick before
        // the solid one does. EngageBridge no-ops if already engaged, so
        // double-pathing is safe.
        var bw = other.GetComponent<BridgeWalkable>();
        if (bw != null)
        {
            // Always drop climb state — even if we don't (re-)engage this tick
            // (e.g. _recentlyReleasedBridge is latched), bridge contact must
            // not be reinterpreted as wall contact.
            isClimbing = false;
            EngageBridge(bw);
            return;
        }

        int layer = other.gameObject.layer;
        if (layer == LayerMask.NameToLayer("Ground"))
        {
            // Ground contact is one of the three explicit "leave the bridge
            // for real" paths — see DisengageBridge() doc.
            DisengageBridge();
            isClimbing = false;
            return;
        }
        if (layer == LayerMask.NameToLayer("Climbable"))
        {
            // While engaged with a bridge, ignore the wall's Climbable trigger.
            // Without this gate the wall keeps re-asserting isClimbing every
            // physics tick — the player would visually walk on the planks but
            // logically stay in climb state, with the up-facing sprite and the
            // wall yanking them off the bridge as soon as gravity took hold.
            if (_isOnBridge) return;
            // Climb engagement is INTENT-driven: contacting the wall isn't
            // enough; the player must also be pressing UP. This avoids the
            // bridge-bounce bug where the bouncing planks briefly drop trigger
            // overlap, _isOnBridge flips false for a tick, and on the same
            // physics frame the Climbable wall trigger flips climb mode on.
            // With this gate, the wall can only grab the player if the player
            // is actively asking to climb. Read Input directly (not MoveInput)
            // because OnTriggerStay can fire before Move() updates MoveInput.
            if (Input.GetAxisRaw("Vertical") > 0f)
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