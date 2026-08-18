using System;
using System.Collections;
using TMPro;
using Unity.Netcode;
using Unity.Netcode.Components;
using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
[RequireComponent(typeof(NetworkTransform))]
[RequireComponent(typeof(NetworkRigidbody))]
public class PlayerController : NetworkBehaviour
{
    private float moveSpeed;
    public float walkSpeed = 5f;
    public float sprintSpeed = 12f;
    public float jumpForce = 12f;
    private Rigidbody rb;
    private float horizontalInput, verticalInput;

    [SerializeField] private float landingCooldown = 0.07f; // delay after landing before jump allowed
    private float groundedTime; // time spent grounded since last landing
    private bool groundedLastFrame;
    [SerializeField] private ParticleSystem runningParticles;
    private float checkRadius = 0.4f;
    [SerializeField] private Transform groundCheckPoint;
    [SerializeField] private LayerMask groundMask;
    public bool jumpPressed = false;
    public bool queueJump = false;
    private Transform orientation;
    [SerializeField] private PlayerInputHandler playerInput;
    public bool sprintPressed = false;
    private float angleFromForward;
    [SerializeField] private Animator animator;
    public NetworkVariable<MovementState> netMovementState = new NetworkVariable<MovementState>(MovementState.idle, NetworkVariableReadPermission.Everyone,
        NetworkVariableWritePermission.Owner);
    private new CapsuleCollider collider;
    private float crouchSpeed = 3f;
    public bool isCrouching = false;
    private bool particlesEnabled = false;    
    public enum MovementState
    {
        walking,
        sprinting,
        crouchWalking,
        crouchIdle,
        air,
        idle
    }
    public MovementState currentState;
    private float desiredMoveSpeed, lastDesiredMoveSpeed;
    private readonly float airMultiplier = 0.4f;
    public float maxSlopeAngle = 45f;
    private RaycastHit slopeHit;
    private bool exitingSlope;
    public event Action<MovementState> OnStateChanged;


    void Awake()
    {
        rb = GetComponent<Rigidbody>();
        rb.interpolation = RigidbodyInterpolation.Interpolate;
        orientation = transform.Find("Orientation");
        playerInput = GetComponent<PlayerInputHandler>();
        collider = GetComponent<CapsuleCollider>();
    }

    private void StateHandler()
    {
        bool grounded = IsGrounded(); // Check grounded state once for efficiency
        MovementState previousState = currentState;  // Store previous state to detect changes
        if (grounded)
        {
            if (isCrouching)
            {
                if (horizontalInput != 0 || verticalInput != 0)
                {
                    currentState = MovementState.crouchWalking; //If crouching and moving, set to crouchWalking
                }
                else
                {
                    currentState = MovementState.crouchIdle; //If crouching and not moving, set to crouchIdle
                }
                moveSpeed = crouchSpeed; // Set move speed to crouch speed
            }
            else if (IsSprinting())
            {
                currentState = MovementState.sprinting; // If sprinting, set to sprinting
                moveSpeed = sprintSpeed; // Set move speed to sprint speed
            }
            else if (horizontalInput != 0 || verticalInput != 0)
            {
                currentState = MovementState.walking; // If moving (but not sprinting or crouching), set to walking
                moveSpeed = walkSpeed;
            }
            else
            {
                currentState = MovementState.idle; //If not moving at all, set to Idle
                rb.linearVelocity = new Vector3(0, rb.linearVelocity.y, 0); // Stop horizontal movement when idle
            }
        }
        else
        {
            currentState = MovementState.air; // If not grounded, set to air (jumping or falling)
        }

        if (previousState != currentState)
        {
            OnStateChanged?.Invoke(currentState); // Invoke event if state changed
        }
        netMovementState.Value = currentState; // Update network variable for animator and other clients
    }
    void Update()
    {
        if (IsOwner)
        {
            // Only the owner of this player object should process input and update movement state
            Vector2 moveInput = playerInput.MoveInput.normalized; // Get normalized input for consistent movement speed in all directions
            horizontalInput = moveInput.x; // Get separate horizontal and vertical inputs for use in movement and state logic
            verticalInput = moveInput.y;
            sprintPressed= playerInput.SprintInput;
            jumpPressed = playerInput.JumpInput;
            angleFromForward = Vector2.Angle(Vector2.up, moveInput); // Calculate angle from forward for sprinting logic
            isCrouching = playerInput.CrouchInput; 
            if (jumpPressed && IsGrounded())
            {
                queueJump = true; //Queue jump to be executed in FixedUpdate
            }
            if (currentState == MovementState.sprinting && !particlesEnabled) //Play running particles when sprinting, stop when not sprinting
            {
                runningParticles.Play(); // Play() starts particles from the beginning
                particlesEnabled = true; // Set flag to avoid calling Play() repeatedly while already playing
            }
            else if (currentState != MovementState.sprinting && particlesEnabled)
            {
                runningParticles.Stop(); 
                particlesEnabled = false;
            }


        }
        AnimatorUpdate(); // Update animator parameters based on current movement state
    }

    private void AnimatorUpdate()
    {   // Set animator bools based on current movement state for animation transitions
        animator.SetBool("isWalking", netMovementState.Value == MovementState.walking);
        animator.SetBool("isSprinting", netMovementState.Value == MovementState.sprinting);
        animator.SetBool("isIdle", netMovementState.Value == MovementState.idle);
        animator.SetBool("isAir", netMovementState.Value == MovementState.air);
        animator.SetBool("isCrouchWalking", netMovementState.Value == MovementState.crouchWalking);
        animator.SetBool("isCrouchIdle", netMovementState.Value == MovementState.crouchIdle);
    }   
    public override void OnNetworkSpawn() //runs once the player object is spawned on the network
    { 
        if (IsOwner)
        {
            rb.isKinematic = false; // Ensures the rigidbody is not kinematic, so forces can be applied.
        }
        else
        {
            StartCoroutine(nameof(FixAnimator));
        }
    }

    IEnumerator FixAnimator() //helper routine to fix animator update mode for non-owners.
    {
        yield return new WaitForSeconds(1f);
        animator.updateMode = AnimatorUpdateMode.Normal;
    }

    private void FixedUpdate()
    {   // Physics calculations and movement forces are processed here
        if (!IsOwner) return; // Only the owner should control movement

        // Track landing and time spent grounded
        bool grounded = IsGrounded();
        if (grounded && !groundedLastFrame)
        {
            groundedTime = 0f; // just landed
        }
        groundedTime = grounded ? groundedTime + Time.fixedDeltaTime : 0f;

        // Movement forces
        Vector3 movementDirection = orientation.forward * verticalInput + orientation.right * horizontalInput;
        if(OnSlope() && !exitingSlope)
        {
           rb.AddForce(GetSlopeMoveDirection(movementDirection) * moveSpeed * 20f, ForceMode.Force); //Apply force parallel to slope
            if (rb.linearVelocity.y > 0)
            {
                rb.AddForce(Vector3.down * 80f, ForceMode.Force); // Apply downward force to stick to slope when moving uphill
            }
        }
        else if (grounded)
        {
            rb.AddForce(10f * moveSpeed * movementDirection, ForceMode.Force); // Apply regular movement force when grounded
        }
        else
        {
            rb.AddForce(10f * moveSpeed * airMultiplier * movementDirection, ForceMode.Force); // Apply reduced air control force when not grounded
        }

        // Adding drag and limiting y-velocity when grounded to prevent bouncing and allow for better control
        if (grounded)
        {
            if (WorldEventManager.Instance.currentEvent.Value == WorldEventManager.WorldEventType.IcyFloor) return; //Icy Floor: no drag applied when grounded
            rb.linearDamping = 5f;
            if (rb.linearVelocity.y < -0.1f)
            {
                rb.linearVelocity = new Vector3(rb.linearVelocity.x, -0.1f, rb.linearVelocity.z);
            }
        }
        else
        {
            rb.linearDamping = 0f; //No drag in the air, better control
        }

        // Landing cooldown gate
        bool landingReady = groundedTime >= landingCooldown;
        if (landingReady) {
            exitingSlope = false; // Gets rid of the sticking-to-slope downward force, allowing for normal jumps on slopes
        }
        if (queueJump && landingReady) //If the player is ready to jump, execute jump and reset the jump queue
        {
            Jump();
            queueJump = false;
            animator.SetTrigger("Jumped");

        }

        // Collider adjustments for crouching
        if (isCrouching)
        {
            collider.height = 1.4f;
            collider.center = new Vector3(0, -0.2f, 0);
        }
        else
        {
            collider.height = 2f;
            collider.center = new Vector3(0, 0, 0);
        }

        SpeedControl(); //Limits movement speed
        StateHandler(); //Handles movement state

        groundedLastFrame = grounded;

        rb.useGravity = !OnSlope(); //Disable gravity on slope for smoother movement
    }

    private bool IsGrounded()
    {   // Sphere, centre = groundCheckPoint, radius = checkRadius, detecting for collisions with objects on the groundMask layer
        return Physics.CheckSphere(groundCheckPoint.position, checkRadius, groundMask);
    }

    private void Jump()
    {   //Y-velocity is reset, and jump force is applied upwards
        exitingSlope = true;
        rb.linearVelocity = new Vector3(rb.linearVelocity.x, 0f, rb.linearVelocity.z);
        rb.AddForce(transform.up * jumpForce, ForceMode.Impulse);
    }

    public bool IsSprinting()
    {   // Player can only sprint if sprint button is held, moving forward, and within 45 degrees of forward direction
        return sprintPressed && verticalInput > 0 && angleFromForward <= 45f;
    }
    void SpeedControl()
    {   //Limits the player's movement speed to the current moveSpeed
        if (OnSlope() && !exitingSlope)
        {   // When on a slope, 3D velocity must be limited
            if (rb.linearVelocity.magnitude > moveSpeed)
                rb.linearVelocity = rb.linearVelocity.normalized * moveSpeed;
        }
        else
        {   //Moving in the x-z plane, so only 2D velocity needs to be limited
            Vector2 flatVel = new Vector2(rb.linearVelocity.x, rb.linearVelocity.z);
            if (flatVel.magnitude > moveSpeed)
            {
                Vector2 limitedVel = flatVel.normalized * moveSpeed;
                rb.linearVelocity = new Vector3(limitedVel.x, rb.linearVelocity.y, limitedVel.y);
            }
        }
    }
    private bool OnSlope()
    {   //Casts a ray downwards to check if the player is on a slope
        if (Physics.Raycast(transform.position, Vector3.down, out slopeHit, collider.height / 2 + 0.5f))
        {
            float angle = Vector3.Angle(Vector3.up, slopeHit.normal);
            return angle < maxSlopeAngle && angle != 0;
        }
        return false;
    }
    private Vector3 GetSlopeMoveDirection(Vector3 direction)
    {   //Returns the movement direction adjusted to be parallel to the slope
        return Vector3.ProjectOnPlane(direction, slopeHit.normal).normalized;
    }
}