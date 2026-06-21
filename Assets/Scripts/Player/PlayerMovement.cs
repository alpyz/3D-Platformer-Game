using UnityEngine;

[RequireComponent(typeof(PlayerInputHandler))]
[RequireComponent(typeof(CharacterController))]
public class PlayerMovement : MonoBehaviour
{
    [Header("Movement")]
    public float maxSpeed = 8f;
    public float acceleration = 40f;
    public float deceleration = 60f;
    public float airControlMultiplier = 0.4f; // 0 = no air control, 1 = same as grounded

    [Header("Sprint")]
    public float sprintMultiplier = 1.6f;

    private Vector3 horizontalVelocity;

    [Header("Jump")]
    public float jumpHeight = 2f;
    public float gravity = -25f;
    public float lowJumpMultiplier = 2.5f;
    public float fallGravityMultiplier = 1.8f;
    public float coyoteTime = 0.12f;
    public float jumpBufferTime = 0.12f;

    [Header("Double Jump")]
    public int maxJumps = 2; // 1 = single jump, 2 = double jump, etc.
    private int jumpsRemaining;

    private PlayerInputHandler input;
    private CharacterController controller;

    private float coyoteTimer;
    private float jumpBufferTimer;
    private Vector3 velocity;

    private void Awake()
    {
        input = GetComponent<PlayerInputHandler>();
        controller = GetComponent<CharacterController>();
    }

    private void Update()
    {
        HandleTimers();
        HandleJump();
        ApplyGravity();
        HandleMove();

        controller.Move((horizontalVelocity + Vector3.up * velocity.y) * Time.deltaTime);
    }
    private void HandleMove()
    {
        Vector2 moveInput = input.moveInput;
        Vector3 inputDir = new Vector3(moveInput.x, 0f, moveInput.y);

        float targetSpeed = maxSpeed * (input.sprintHeld ? sprintMultiplier : 1f);
        Vector3 targetVelocity = inputDir * targetSpeed;

        float rate = inputDir.magnitude > 0.01f ? acceleration : deceleration;

        // Scale the rate of change while airborne, not the velocity itself
        if (!controller.isGrounded)
            rate *= airControlMultiplier;

        horizontalVelocity = Vector3.MoveTowards(horizontalVelocity, targetVelocity, rate * Time.deltaTime);
    }

    private void HandleTimers()
    {
        if (controller.isGrounded)
        {
            coyoteTimer = coyoteTime;
            jumpsRemaining = maxJumps; // reset double jump charges on landing
        }
        else
        {
            coyoteTimer -= Time.deltaTime;
        }

        jumpBufferTimer -= Time.deltaTime;

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

        bool groundedJumpAvailable = coyoteTimer > 0f;
        bool airJumpAvailable = jumpsRemaining > 0;

        if (groundedJumpAvailable || airJumpAvailable)
        {
            velocity.y = Mathf.Sqrt(jumpHeight * -2f * gravity);
            jumpBufferTimer = 0f;

            if (groundedJumpAvailable)
            {
                coyoteTimer = 0f;
                jumpsRemaining = maxJumps - 1; // first jump used, remaining air jumps left
            }
            else
            {
                jumpsRemaining--; // used an air jump
            }
        }
    }

    private void ApplyGravity()
    {
        float g = gravity;

        if (velocity.y < 0f)
        {
            g *= fallGravityMultiplier;
        }
        else if (velocity.y > 0f && !input.jumpHeld)
        {
            g *= lowJumpMultiplier;
        }

        velocity.y += g * Time.deltaTime;

        if (controller.isGrounded && velocity.y < 0f)
            velocity.y = -2f;
    }
}