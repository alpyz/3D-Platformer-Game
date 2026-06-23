using UnityEngine;

[RequireComponent(typeof(PlayerInputHandler))]
[RequireComponent(typeof(CharacterController))]
public class PlayerMovement : MonoBehaviour
{
    [Header("Movement")]
    public float maxSpeed = 8f;
    public float acceleration = 40f;
    public float deceleration = 60f;
    public float airControlMultiplier = 0.4f;

    [Header("Sprint")]
    public float sprintMultiplier = 1.6f;

    [Header("Jump")]
    public float jumpHeight = 2f;
    public float gravity = -25f;
    public float lowJumpMultiplier = 2.5f;
    public float fallGravityMultiplier = 1.8f;
    public float coyoteTime = 0.12f;
    public float jumpBufferTime = 0.12f;

    [Header("Double Jump")]
    public int maxJumps = 2;
    private int jumpsRemaining;

    [Header("Wall Climb")]
    public float wallClimbSpeed = 12f;
    public float wallSlideSpeed = 1.5f;
    public float wallClimbDuration = 1.5f;
    public float wallJumpHorizontalForce = 8f;
    public float wallJumpVerticalForce = 10f;
    public float wallCheckDistance = 0.6f;

    // wall state booleans — were missing, caused all the errors
    private bool isTouchingWall;
    private bool isWallClimbing;
    private bool isWallHanging;
    private Vector3 wallNormal;
    private float wallClimbTimer;
    private Vector3 lastWallNormal; // stores the normal of the wall that was jumped from

    [Header("Wall Climb Feel")]
    public float wallAttachCooldown = 0.2f;
    public float wallJumpControlLockDuration = 0.2f;
    public float wallClimbSpeedRampUp = 2f;
    public float wallSlideSpeedRampUp = 1f;
    public float wallJumpBoostWindow = 0.1f;
    public float wallDetachInputThreshold = 0.8f;
    public float wallAutoClimbDelay = 0.4f;
    public float wallHoldDuration = 1.2f;

    private float wallAutoClimbTimer;
    private float wallHoldTimer;
    private bool wallAutoClimbActive;
    private float wallAttachCooldownTimer;
    private float wallJumpControlLockTimer;
    private float wallCoyoteTimer;
    private float currentWallClimbSpeed;
    private float currentWallSlideSpeed;
    private bool wasOnWallLastFrame;
    public float wallSlideAcceleration = 6f; // how fast slide speed ramps up toward freefall

    private PlayerInputHandler input;
    private CharacterController controller;

    private float coyoteTimer;
    private float jumpBufferTimer;
    private Vector3 velocity;
    private Vector3 horizontalVelocity;

    private void Awake()
    {
        input = GetComponent<PlayerInputHandler>();
        controller = GetComponent<CharacterController>();
        wallClimbTimer = wallClimbDuration;
        wallHoldTimer = wallHoldDuration;
    }

    private void Update()
    {
        DetectWall();
        HandleTimers();
        HandleWallClimb();
        HandleJump();
        ApplyGravity();
        HandleMove();

        controller.Move((horizontalVelocity + Vector3.up * velocity.y) * Time.deltaTime);
    }

    private void DetectWall()
    {
        wasOnWallLastFrame = isTouchingWall;

        Vector3[] directions = { transform.forward, -transform.forward, transform.right, -transform.right };
        isTouchingWall = false;

        foreach (var dir in directions)
        {
            if (Physics.SphereCast(transform.position, 0.3f, dir, out RaycastHit hit, wallCheckDistance))
            {
                if (Mathf.Abs(hit.normal.y) < 0.3f)
                {
                    bool isSameWall = Vector3.Dot(hit.normal, lastWallNormal) > 0.9f;

                    if (isSameWall && wallAttachCooldownTimer > 0f)
                        continue;

                    // Fresh wall attachment — different wall than last one
                    bool isNewWall = Vector3.Dot(hit.normal, wallNormal) < 0.9f;

                    if (isNewWall && !wasOnWallLastFrame)
                    {
                        // Reset all climb state so climb restarts from scratch
                        wallClimbTimer = wallClimbDuration;
                        wallAutoClimbTimer = 0f;
                        wallAutoClimbActive = false;
                        currentWallClimbSpeed = 0f;
                        currentWallSlideSpeed = 0f;
                        isWallClimbing = false;
                        isWallHanging = false;
                    }

                    isTouchingWall = true;
                    wallNormal = hit.normal;
                    break;
                }
            }
        }

        if (wasOnWallLastFrame && !isTouchingWall)
            wallCoyoteTimer = wallJumpBoostWindow;
    }

    private void HandleWallClimb()
    {
        if (controller.isGrounded)
        {
            wallClimbTimer = wallClimbDuration;
            wallAutoClimbTimer = 0f;
            wallHoldTimer = wallHoldDuration;
            wallAutoClimbActive = false;
            isWallClimbing = false;
            isWallHanging = false;
            currentWallClimbSpeed = 0f;
            currentWallSlideSpeed = 0f;
            return;
        }

        if (!isTouchingWall)
        {
            wallAutoClimbTimer = 0f;
            wallAutoClimbActive = false;
            isWallClimbing = false;
            isWallHanging = false;
            currentWallClimbSpeed = 0f;
            return;
        }

        // Intentional detach — player pushes hard away from wall
        Vector2 moveInput = input.moveInput;
        Vector3 inputDir = new Vector3(moveInput.x, 0f, moveInput.y);
        float awayDot = Vector3.Dot(inputDir, wallNormal);

        if (awayDot > wallDetachInputThreshold)
        {
            isWallClimbing = false;
            isWallHanging = false;
            wallAttachCooldownTimer = wallAttachCooldown;
            wallAutoClimbActive = false;
            wallAutoClimbTimer = 0f;
            return;
        }

        // Phase 1 — Hold: brief hang before auto climb kicks in
        if (!wallAutoClimbActive)
        {
            isWallClimbing = false;
            isWallHanging = true;

            wallAutoClimbTimer += Time.deltaTime;

            if (wallAutoClimbTimer >= wallAutoClimbDelay)
                wallAutoClimbActive = true;

            return;
        }

        // Phase 2 — Active climb: auto climbing, stamina draining
        if (wallClimbTimer > 0f)
        {
            isWallClimbing = true;
            isWallHanging = false;
            wallClimbTimer -= Time.deltaTime;

            currentWallClimbSpeed = Mathf.MoveTowards(
                currentWallClimbSpeed,
                wallClimbSpeed,
                (wallClimbSpeed / wallClimbSpeedRampUp) * Time.deltaTime
            );
        }
        else
        {
            // Phase 3 — Stamina depleted: slide accelerates over time toward freefall
            isWallClimbing = false;
            isWallHanging = true;

            // Accelerate slide speed up toward gravity-equivalent freefall
            // instead of clamping to a fixed wallSlideSpeed, we let it keep building
            currentWallSlideSpeed = Mathf.MoveTowards(
                currentWallSlideSpeed,
                maxSpeed * 3f, // target is effectively freefall speed, not a gentle slide
                wallSlideAcceleration * Time.deltaTime
            );
        }
    }

    private void HandleTimers()
    {
        if (controller.isGrounded)
        {
            coyoteTimer = coyoteTime;
            jumpsRemaining = maxJumps;
        }
        else
        {
            coyoteTimer -= Time.deltaTime;
        }

        wallCoyoteTimer -= Time.deltaTime;
        jumpBufferTimer -= Time.deltaTime;
        wallAttachCooldownTimer -= Time.deltaTime;
        wallJumpControlLockTimer -= Time.deltaTime;

        if (input.jumpPressed)
        {
            jumpBufferTimer = jumpBufferTime;
            input.ConsumeJumpPress();
        }
    }

    private void HandleJump()
    {
        bool wantsJump = jumpBufferTimer > 0f;
        if (!wantsJump) return;

        bool wallJumpAvailable = isWallClimbing || isWallHanging || wallCoyoteTimer > 0f;

        if (wallJumpAvailable && !controller.isGrounded)
        {
            horizontalVelocity = wallNormal * wallJumpHorizontalForce;
            velocity.y = wallJumpVerticalForce;

            wallJumpControlLockTimer = wallJumpControlLockDuration;

            // Store which wall was jumped from instead of blanket cooldown
            lastWallNormal = wallNormal;
            wallAttachCooldownTimer = wallAttachCooldown;

            isWallClimbing = false;
            isWallHanging = false;
            wallCoyoteTimer = 0f;
            jumpsRemaining = maxJumps - 1;

            jumpBufferTimer = 0f;
            coyoteTimer = 0f;
            return;
        }

        bool groundedJumpAvailable = coyoteTimer > 0f;
        bool airJumpAvailable = jumpsRemaining > 0;

        if (groundedJumpAvailable || airJumpAvailable)
        {
            velocity.y = Mathf.Sqrt(jumpHeight * -2f * gravity);
            jumpBufferTimer = 0f;

            if (groundedJumpAvailable)
            {
                coyoteTimer = 0f;
                jumpsRemaining = maxJumps - 1;
            }
            else
            {
                jumpsRemaining--;
            }
        }
    }

    private void HandleMove()
    {
        if (wallJumpControlLockTimer > 0f)
            return;

        if (isWallClimbing || isWallHanging)
        {
            horizontalVelocity = Vector3.zero;
            return;
        }

        Vector2 moveInput = input.moveInput;
        Vector3 inputDir = new Vector3(moveInput.x, 0f, moveInput.y);

        float targetSpeed = maxSpeed * (input.sprintHeld ? sprintMultiplier : 1f);
        Vector3 targetVelocity = inputDir * targetSpeed;

        float rate = inputDir.magnitude > 0.01f ? acceleration : deceleration;

        if (!controller.isGrounded)
            rate *= airControlMultiplier;

        horizontalVelocity = Vector3.MoveTowards(horizontalVelocity, targetVelocity, rate * Time.deltaTime);
    }

    private void ApplyGravity()
    {
        if (isWallClimbing)
        {
            velocity.y = currentWallClimbSpeed;
            return;
        }

        if (isWallHanging)
        {
            if (!wallAutoClimbActive)
            {
                // Phase 1 hold: completely stationary, not falling at all
                velocity.y = 0f;
            }
            else
            {
                // Phase 3 slide: use accumulated slide speed, moving downward
                // applying it directly rather than through gravity so wall friction
                // controls the descent rather than freefall — but it keeps building
                velocity.y = -currentWallSlideSpeed;

                // Once slide speed is high enough, just let gravity take over fully
                // this is the "basically falling" threshold — wall can no longer hold them
                if (currentWallSlideSpeed >= maxSpeed * 2f)
                {
                    isWallHanging = false;
                    isTouchingWall = false; // detach — gravity takes over from here
                }
            }
            return;
        }

        float g = gravity;

        if (velocity.y < 0f)
            g *= fallGravityMultiplier;
        else if (velocity.y > 0f && !input.jumpHeld)
            g *= lowJumpMultiplier;

        velocity.y += g * Time.deltaTime;

        if (controller.isGrounded && velocity.y < 0f)
            velocity.y = -2f;
    }
}