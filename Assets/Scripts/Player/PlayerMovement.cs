
using Unity.Cinemachine;
using UnityEngine;

public enum MovementState
{
    Grounded,
    Airborne,
    Climbing,
    Sliding,
    Hanging,
}

public class PlayerMovement : MonoBehaviour
{
    [Header("Essentials")]
    PlayerInputHandler inputHandler;
    CharacterController controller;
    public MovementState currentState = MovementState.Grounded;


    [Header("Player Rotation")]
    Vector2 inputToRotate;
    public float rotationSpeed;
    private bool isPivotingOnSpot;

    [Header("Camera")]
    public CinemachineCamera playerCamera;
    

    [Header("LayerMasks")]
    public LayerMask climbable;

    [Header("Move")]
    public float moveSpeedMax = 15f;
    public float airSpeedMax = 7f;
    private float currentMaxSpeed;
    private float currentSmoothTime;
    private Vector3 smoothMoveVelocity;
    private Vector3 movementHorizontal;
    public float smoothTime = 0.15f;
    public float airSmoothTime = 1f;
    private Vector3 finalMovement;

    [Header("Gravity")]
    public float playerVerticalVelocity;
    public float gravitationalAcceleration;
    public float gravityMax;
    public float groundedVerticalVelocity;
    public Vector3 sphereCastOffsetCeiling;
    public float sphereCastHeightCeiling;

    [Header("Jump")]
    public float jumpPower;
    public int jumpRemaining = 2;
    public float jumpPowerModifier = 70f;
    public bool jumpTimerGoing;
    private float jumpBufferTimer;
    private const float JUMP_BUFFER_DURATION = 0.2f;

    [Header("Wall Jump")]
    public bool wallNearby;
    public float sphereCastOffsetWall;
    public float sphereCastForwardWall;
    public float sphereRadius;
    private Vector3 pureHorizontalNormal;
    public float wallJumpMultiplier;
    private float wallJumpLockoutTimer;
    private const float WALL_JUMP_LOCKOUT_DURATION = 0.25f;


    [Header("Climb")]
    public float climbSpeedMax;
    public float climbTimer;
    private float currentClimbTime;
    public float climbSmoothTime;
    public bool runningTowardsWall;

    [Header("Slide")]
    public float slideSpeedMax;
    public float slideSmoothTime;

    [Header("Ledge")]
    public bool canHang;
    public bool ledgeNearby;
    public float ledgeOffset = 0.1f;
    public float sphereCastLedgeHeight;
    public float ledgeRadius;
    public Vector3 sphereCastOffsetLedge;
    private float ledgeJumpLockoutTimer;
    private const float LEDGE_JUMP_LOCKOUT_DURATION = 0.25f;


    public float ledgeCheckLowerHeight = 0.8f;   // roughly waist height relative to controller center
    public float ledgeCheckUpperHeight = 2.2f;   // roughly above head height
    public float ledgeCheckForwardDistance = 0.6f;
    public float ledgeSurfaceMaxNormalAngle = 45f;
    private void Start()
    {
        ledgeCheckUpperHeight = controller.height * 0.95f;
        currentClimbTime = climbTimer;
        inputHandler = GetComponent<PlayerInputHandler>();
        controller = GetComponent<CharacterController>();
    }
    private void FixedUpdate()
    {
        

        if (inputHandler.jumpPressed)
        {
            inputHandler.ConsumeJumpPress();
            jumpBufferTimer = JUMP_BUFFER_DURATION;
            jumpTimerGoing = true;
        }
        Timers();

        WallDetection();
        
        
        StateHandler();
        
    }
    private void Timers()
    {
        if (wallJumpLockoutTimer > 0f)
        {
            wallJumpLockoutTimer -= Time.fixedDeltaTime;
        }

        if(ledgeJumpLockoutTimer > 0f)
        {
            ledgeJumpLockoutTimer -= Time.fixedDeltaTime;
        }

        if (jumpBufferTimer > 0)
        {
            jumpTimerGoing = true;
            jumpBufferTimer -= Time.fixedDeltaTime;
        }
        else if(jumpBufferTimer <=0)
        {

            jumpBufferTimer = 0f;
            jumpTimerGoing = false;
            
        }
    }
    private void StateHandler()
    {
        switch (currentState)
        {
            case MovementState.Grounded:
                currentMaxSpeed = moveSpeedMax;
                currentSmoothTime = smoothTime;
                currentClimbTime = climbTimer;
                jumpRemaining = 2;
                playerVerticalVelocity = groundedVerticalVelocity;
                //holding jump button determines the power
                if (inputHandler.jumpHeld && jumpPower < 0.1f)
                {
                    jumpPower += Time.fixedDeltaTime;
                }

                bool releasedEarly = !inputHandler.jumpHeld && jumpPower > 0.001f;
                bool reachedMaxCharge = jumpPower >= 0.1f;

                //check for jump buffer
                if (jumpTimerGoing && !inputHandler.jumpHeld && jumpPower < 0.001f)
                {
                    
                    inputHandler.ConsumeJumpPress();
                    jumpRemaining = 1;
                    playerVerticalVelocity = 0.20f * jumpPowerModifier; 
                    jumpPower = 0f;
                    currentState = MovementState.Airborne;
                    jumpBufferTimer = 0f;
                    jumpTimerGoing = false;
                    break;
                }
                //ground jump
                else if ((releasedEarly || reachedMaxCharge) && jumpTimerGoing)
                {
                    jumpBufferTimer = 0f;
                    jumpTimerGoing = false;
                    inputHandler.ConsumeJumpPress();
                    jumpRemaining = 1;
                    playerVerticalVelocity = (0.10f + jumpPower) * jumpPowerModifier;
                    jumpPower = 0f;
                    currentState = MovementState.Airborne;
                    break;
                }
                

                HorizontalMovement();
                VerticalMovement();
                if (!controller.isGrounded) currentState = MovementState.Airborne;
                break;

            case MovementState.Airborne:
                currentMaxSpeed = airSpeedMax;
                currentSmoothTime = airSmoothTime;
                currentClimbTime = climbTimer;
                if (jumpRemaining > 0 && jumpTimerGoing)
                {
                    
                    inputHandler.ConsumeJumpPress();
                    jumpBufferTimer = 0f;
                    jumpTimerGoing = false;

                    jumpRemaining = 0;
                    playerVerticalVelocity = 0.3f * jumpPowerModifier;
                    jumpPower = 0f;
                }
                HorizontalMovement();
                VerticalMovement();
                if (wallNearby && runningTowardsWall && wallJumpLockoutTimer <= 0f) 
                {
                    playerVerticalVelocity = 0f;
                    currentState = MovementState.Climbing;
                }
                if (canHang && (ledgeJumpLockoutTimer <= 0))
                {
                    currentState = MovementState.Hanging;
                }
                if(controller.isGrounded) currentState = MovementState.Grounded;
 
                break;

            case MovementState.Climbing:
                jumpRemaining = 1;
                Wallmovement();
                if (!wallNearby)
                {
                    currentState = MovementState.Airborne;
                }

                if (canHang && (currentClimbTime > climbTimer / 4f))
                {
                    //direkt yukarý týrman
                }
                else if (canHang &&( ledgeJumpLockoutTimer<=0))
                {
                    currentState = MovementState.Hanging;
                }
                break;

            case MovementState.Sliding:
                Wallmovement();
                if (!wallNearby)
                {
                    currentState = MovementState.Airborne;
                }
                else if (controller.isGrounded)
                {
                    currentState = MovementState.Grounded;
                }
                break;
            case MovementState.Hanging:
                jumpRemaining = 1;
                if (canHang)
                {
                    movementHorizontal= Vector3.zero;
                    playerVerticalVelocity = 0f;
                }
                if (jumpRemaining > 0 && jumpTimerGoing)
                {
                    currentState = MovementState.Airborne;
                    ledgeJumpLockoutTimer = LEDGE_JUMP_LOCKOUT_DURATION;
                    
                    
                }
                
                HorizontalMovement();
                VerticalMovement();

                break;
        }
    }




    private void HorizontalMovement()
    {
        inputToRotate = inputHandler.moveInput;
        float inputMagnitude = Mathf.Clamp01(inputToRotate.magnitude);
        float angleDifference = 0f;

        if (inputMagnitude > 0.1f && currentState == MovementState.Grounded)
        {
            float inputAngle = Mathf.Atan2(inputToRotate.x, inputToRotate.y) * Mathf.Rad2Deg;
            float cameraAngle = playerCamera.transform.rotation.eulerAngles.y;
            float targetAngle = inputAngle + cameraAngle;

            Quaternion targetRotation = Quaternion.Euler(0f, targetAngle, 0f);
            angleDifference = Quaternion.Angle(transform.rotation, targetRotation);

            if (!isPivotingOnSpot && angleDifference > 110f)
            {
                isPivotingOnSpot = true;
            }

            transform.rotation = Quaternion.RotateTowards(transform.rotation, targetRotation, rotationSpeed * Time.fixedDeltaTime);
        }

        if (isPivotingOnSpot && angleDifference < 15f)
        {
            isPivotingOnSpot = false;
        }

        if (inputMagnitude <= 0.05f)
        {
            isPivotingOnSpot = false;
        }

        Vector3 targetVelocity;

        if ((wallJumpLockoutTimer > 0f)||(ledgeJumpLockoutTimer>0))
        {
            targetVelocity = movementHorizontal;
        }
        else if (currentState == MovementState.Grounded && isPivotingOnSpot)
        {
            targetVelocity = Vector3.zero;
        }
        else if (currentState == MovementState.Grounded)
        {
            float alignmentModifier = Mathf.Clamp01(1f - (angleDifference / 180f));
            targetVelocity = transform.forward * inputMagnitude * currentMaxSpeed * alignmentModifier;
        }
        else if (currentState == MovementState.Airborne)
        {
            Vector3 camForward = playerCamera.transform.forward;
            Vector3 camRight = playerCamera.transform.right;

            camForward.y = 0f;
            camRight.y = 0f;
            camForward.Normalize();
            camRight.Normalize();

            Vector3 cameraRelativeDirection = (camForward * inputToRotate.y) + (camRight * inputToRotate.x);
            targetVelocity = cameraRelativeDirection.normalized * inputMagnitude * currentMaxSpeed;
        }
        else
        {
            targetVelocity = Vector3.zero;
        }

        movementHorizontal = Vector3.SmoothDamp(movementHorizontal, targetVelocity, ref smoothMoveVelocity, currentSmoothTime, Mathf.Infinity, Time.fixedDeltaTime);
    }



    private void VerticalMovement()
    {
        if (currentState == MovementState.Airborne)
        {
            jumpPower = 0f;
            playerVerticalVelocity -= gravitationalAcceleration * Time.fixedDeltaTime;
            playerVerticalVelocity = Mathf.Max(playerVerticalVelocity, gravityMax);

            
        }

        Vector3 motion = new Vector3(0, playerVerticalVelocity, 0);
        finalMovement = movementHorizontal + motion;
        controller.Move(finalMovement * Time.fixedDeltaTime);
        jumpPower = Mathf.Clamp01(jumpPower);

        Vector3 worldControllerCenter = transform.TransformPoint(controller.center);
        Vector3 castOrigin = worldControllerCenter + sphereCastOffsetCeiling;
        RaycastHit hitCeiling;

        if (Physics.SphereCast(castOrigin, controller.radius, transform.up, out hitCeiling, sphereCastHeightCeiling, climbable))
        {
            if (hitCeiling.normal.y < -0.5f) 
            {
                playerVerticalVelocity = groundedVerticalVelocity;
            }
        }


    }
    private void Wallmovement()
    {
        if ((currentState == MovementState.Climbing|| currentState == MovementState.Sliding) && jumpTimerGoing)
        {

            transform.rotation = transform.rotation* Quaternion.Euler(0, 180, 0);
            jumpRemaining = 0;
            inputHandler.ConsumeJumpPress();
            movementHorizontal = pureHorizontalNormal * wallJumpMultiplier;
            playerVerticalVelocity = wallJumpMultiplier * 2f;
            wallJumpLockoutTimer = WALL_JUMP_LOCKOUT_DURATION;
            currentState = MovementState.Airborne;
            jumpBufferTimer = 0f;
            jumpTimerGoing = false;
            return;
        }

        if (currentState == MovementState.Climbing && currentClimbTime > 0f)
        {
            currentClimbTime -= Time.fixedDeltaTime;
            float targetVerticalSpeed = climbSpeedMax;
            playerVerticalVelocity = Mathf.MoveTowards(playerVerticalVelocity, targetVerticalSpeed, (1f / climbSmoothTime) * Time.fixedDeltaTime);
            Vector3 climbMotion = new Vector3(0f, playerVerticalVelocity, 0f);
            controller.Move(climbMotion * Time.fixedDeltaTime);

        }
        else if (currentState == MovementState.Sliding)
        {
            float targetVerticalSpeed = -slideSpeedMax;
            playerVerticalVelocity = Mathf.MoveTowards(playerVerticalVelocity, targetVerticalSpeed, (1f / slideSmoothTime) * Time.fixedDeltaTime);
            Vector3 slideMotion = new Vector3(0f, playerVerticalVelocity, 0f);
            controller.Move(slideMotion * Time.fixedDeltaTime);
        }
        else if (currentState == MovementState.Climbing && currentClimbTime <= 0f)
        {
            currentClimbTime = 0f;
            currentState = MovementState.Sliding;
        }

    }
    

    private void WallDetection()
    {
        Vector3 ledgePosition = Vector3.zero;

        RaycastHit hitWall;
        Vector3 worldControllerCenter = transform.TransformPoint(controller.center);
        Vector3 castOrigin = worldControllerCenter +(transform.forward.normalized*sphereCastOffsetWall);

        wallNearby = Physics.SphereCast(castOrigin, sphereRadius, transform.forward, out hitWall, sphereCastForwardWall, climbable);
        if(wallNearby)
        {
            pureHorizontalNormal = new Vector3(hitWall.normal.x, 0f, hitWall.normal.z).normalized;
        }
        if (inputHandler.moveInput.magnitude > 0.01f&& wallNearby)
        {
            float dotproduct = Vector3.Dot(hitWall.normal,transform.forward);
            runningTowardsWall = dotproduct < -0.9f;
        }
        /*
        Vector3 worldControllerCenter2 = transform.TransformPoint(controller.center);
        Vector3 worldOffset = transform.TransformDirection(sphereCastOffsetLedge);
        Vector3 castOrigin2 = (worldControllerCenter2 + worldOffset);
        RaycastHit hitWallY;

        ledgeNearby = Physics.SphereCast(castOrigin2, ledgeRadius, -transform.up, out hitWallY, sphereCastLedgeHeight, climbable);
        if (ledgeNearby)
        {
            float heightDiff = hitWallY.point.y - transform.position.y;
            canHang = heightDiff > 0f && heightDiff < ledgeOffset;
        }
        else
        {
            canHang = false;
        }
        */
        

    }



}
