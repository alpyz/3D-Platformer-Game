using Unity.Cinemachine;
using Unity.VisualScripting;
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

    [Header("Animation")]
    [SerializeField] Animator playerAnimator;
    public float animationTransitionTime = 0.2f;

    AnimatorStateInfo currentAnimation;
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
    private float coyoteTimer;
    private const float JUMP_BUFFER_DURATION = 0.2f;
    private const float COYOTE_TIME_DURATION = 0.15f;
    
    [Header("Wall Jump")]
    public bool wallNearby;
    public float castSeperation = 4f;
    public Vector3 sphereCastOffsetWall;
    public float sphereCastHeight;
    public float sphereRadius;
    private Vector3 pureHorizontalNormal;
    public float wallJumpMultiplier;
    private float wallJumpLockoutTimer;
    private float wallVaultLockoutTimer;
    private const float WALL_JUMP_LOCKOUT_DURATION = 0.25f;
    private const float WALL_VAULT_LOCKOUT_DURATION = 1f;
    private int sphereCastHitCount;

    [Header("Climb")]
    private Vector3 wallOppositeDirectionClimb;
    public float climbSpeedMax;
    public float climbTimer;
    private float currentClimbTime;
    public float climbSmoothTime;
    public bool runningTowardsWall;
    private float slopeAngle;
    public float climbAngle;

    [Header("Slide")]
    public float slideSpeedMax;
    public float slideSmoothTime;

    [Header("Ledge")]
    public bool canHang;
    public bool castHitMiddle;
    public bool castHitUpper;
    public float castHeight = 0.4f;

    public Vector3 rayCastOffsetUpper;
    public Vector3 rayCastOffsetMiddle;
    private float ledgeJumpLockoutTimer;
    private const float LEDGE_JUMP_LOCKOUT_DURATION = 0.25f;


    private float hangEntryLockoutTimer;
    private const float HANG_ENTRY_LOCKOUT_DURATION = 0.15f;

    [Header("Particle Effects")]
    public ParticleSystem jumpParticleEffect;
    public ParticleSystem jumpParticleEffect2;
    private void Start()
    {
        currentClimbTime = climbTimer;
        inputHandler = GetComponent<PlayerInputHandler>();
        controller = GetComponent<CharacterController>();
    }
    private void FixedUpdate()
    {
        AnimationConditions();

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
        if(coyoteTimer > 0f)
        {
            coyoteTimer -= Time.fixedDeltaTime;
        }

        if (wallJumpLockoutTimer > 0f)
        {
            wallJumpLockoutTimer -= Time.fixedDeltaTime;
        }

        if (ledgeJumpLockoutTimer > 0f)
        {
            ledgeJumpLockoutTimer -= Time.fixedDeltaTime;
        }
        if (wallVaultLockoutTimer > 0f)
        {
            wallVaultLockoutTimer -= Time.fixedDeltaTime;
        }
        if (hangEntryLockoutTimer > 0f)
        {
            hangEntryLockoutTimer -= Time.fixedDeltaTime;
        }
        if (jumpBufferTimer > 0)
        {
            jumpTimerGoing = true;
            jumpBufferTimer -= Time.fixedDeltaTime;
        }
        else if (jumpBufferTimer <= 0)
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
                    
                    playerAnimator.CrossFade("Jump", animationTransitionTime, 0);
                    ParticleEffectJump(0);
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
                    playerAnimator.CrossFade("Jump", animationTransitionTime, 0);
                    ParticleEffectJump(0);
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
                if (!controller.isGrounded)
                {
                    if (jumpRemaining == 2)
                    {
                        coyoteTimer = COYOTE_TIME_DURATION;
                    }
                    currentState = MovementState.Airborne;
                }
                break;

            case MovementState.Airborne:
                currentMaxSpeed = airSpeedMax;
                currentSmoothTime = airSmoothTime;
                currentClimbTime = climbTimer;
                if(coyoteTimer>=0.03f && jumpTimerGoing)
                {
                    ParticleEffectJump(0);
                    playerAnimator.CrossFade("Jump",0,0);
                    inputHandler.ConsumeJumpPress();
                    jumpBufferTimer = 0f;
                    jumpTimerGoing = false;

                    jumpRemaining = 1;
                    playerVerticalVelocity = 0.3f * jumpPowerModifier;
                    jumpPower = 0f;
                }
                else if (jumpRemaining > 0 && jumpTimerGoing)
                {
                    ParticleEffectJump(1);
                    playerAnimator.CrossFade("Flip", animationTransitionTime, 0, 0f);
                    inputHandler.ConsumeJumpPress();
                    jumpBufferTimer = 0f;
                    jumpTimerGoing = false;

                    jumpRemaining = 0;
                    playerVerticalVelocity = 0.3f * jumpPowerModifier;
                    jumpPower = 0f;
                }
                HorizontalMovement();
                VerticalMovement();
                
                if ((slopeAngle>climbAngle)&&wallNearby && runningTowardsWall && wallJumpLockoutTimer <= 0f)
                {
                    playerVerticalVelocity = 0f;
                    currentState = MovementState.Climbing;
                }
                if (canHang && (ledgeJumpLockoutTimer <= 0))
                {
                    currentState = MovementState.Hanging;
                    // Bu geçiþi tetikleyen zýplama basýmýnýn hemen ardýndan
                    // Hanging state'inde "exit" olarak algýlanmasýný engelle.
                    jumpBufferTimer = 0f;
                    jumpTimerGoing = false;
                    hangEntryLockoutTimer = HANG_ENTRY_LOCKOUT_DURATION;
                }
                if (controller.isGrounded) currentState = MovementState.Grounded;

                break;

            case MovementState.Climbing:
                movementHorizontal = Vector3.zero;
                jumpRemaining = 1;
                Wallmovement();
                if (!wallNearby)
                {
                    currentState = MovementState.Airborne;
                }
                /*
                if (canHang && (currentClimbTime > climbTimer / 4f))
                {
                    //direkt yukarý týrman
                    Debug.Log("sj");
                }
                else if (canHang &&( ledgeJumpLockoutTimer<=0))
                {
                    currentState = MovementState.Hanging;
                }


                */
                if (canHang && (ledgeJumpLockoutTimer <= 0))
                {
                    currentState = MovementState.Hanging;
                    jumpBufferTimer = 0f;
                    jumpTimerGoing = false;
                    hangEntryLockoutTimer = HANG_ENTRY_LOCKOUT_DURATION;
                }
                break;

            case MovementState.Sliding:
                Wallmovement();
                if (!wallNearby)
                {
                    currentState = MovementState.Airborne;
                }
                else if (canHang)
                {
                    currentState = MovementState.Hanging;
                    jumpBufferTimer = 0f;
                    jumpTimerGoing = false;
                    hangEntryLockoutTimer = HANG_ENTRY_LOCKOUT_DURATION;
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
                    if (!currentAnimation.IsName("Hang"))
                    {
                        playerAnimator.CrossFade("Hang", animationTransitionTime, 0, 0f);
                    }

                    movementHorizontal = Vector3.zero;
                    playerVerticalVelocity = 0f;
                }
                else
                {
                    currentState = MovementState.Airborne;
                }
                if (hangEntryLockoutTimer <= 0f && jumpRemaining > 0 && jumpTimerGoing)
                {

                    wallVaultLockoutTimer = WALL_VAULT_LOCKOUT_DURATION;
                    currentState = MovementState.Airborne;
                    ledgeJumpLockoutTimer = LEDGE_JUMP_LOCKOUT_DURATION;
                    inputHandler.ConsumeJumpPress();
                    jumpBufferTimer = 0f;
                    jumpTimerGoing = false;

                }

                break;
        }
    }
    private void ParticleEffectJump(int jumpMode)
    {
        
        if (jumpMode==0)//ground jump
        {
            Vector3 effectPos = transform.TransformPoint(controller.center) - new Vector3(0, 0.88f, 0);
            GameObject jumpEffect = Instantiate(jumpParticleEffect.gameObject, effectPos, transform.rotation);

            Destroy(jumpEffect.gameObject, 4.0f);
        }
        else if (jumpMode==1) //air jump
        {
            Vector3 effectPos = transform.TransformPoint(controller.center) + new Vector3(0,0.5f , 0);
            GameObject jumpEffect = Instantiate(jumpParticleEffect2.gameObject, effectPos, transform.rotation);

            Destroy(jumpEffect.gameObject, 4.0f);
        }
        else // wall jump
        {

        }
    }
        

    private void AnimationConditions()
    {
        if (playerAnimator != null) currentAnimation = playerAnimator.GetCurrentAnimatorStateInfo(0);

        if (currentState == MovementState.Grounded)
        {
            if (inputHandler.moveInput.magnitude < 0.001f && !currentAnimation.IsName("Idle"))
            {
                playerAnimator.CrossFade("Idle", animationTransitionTime, 0);
            }
            else if (inputHandler.moveInput.magnitude >= 0.02f && !currentAnimation.IsName("Run"))
            {

                playerAnimator.CrossFade("Run", animationTransitionTime, 0);
            }
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

        if ((wallJumpLockoutTimer > 0f) || (ledgeJumpLockoutTimer > 0))
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
        else if (currentState == MovementState.Airborne && wallNearby)
        {
            targetVelocity = transform.forward * inputMagnitude * currentMaxSpeed;
        }
        else if(currentState == MovementState.Airborne)
        {
            Vector3 vel1 = playerCamera.transform.right * inputHandler.moveInput.x * currentMaxSpeed;
            Vector3 vel2 = playerCamera.transform.forward * inputHandler.moveInput.y * currentMaxSpeed;
            targetVelocity = vel1 + vel2;
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
        if (currentAnimation.normalizedTime < 1 && currentAnimation.IsName("Hang")) return;
        if ((currentState == MovementState.Climbing || currentState == MovementState.Sliding) && jumpTimerGoing)
        {

            playerAnimator.CrossFade("Jump", animationTransitionTime, 0);
            transform.rotation = transform.rotation * Quaternion.Euler(0, 180, 0);
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
            if (wallVaultLockoutTimer <= 0.02)
            {
                playerAnimator.CrossFade("Climb", animationTransitionTime, 0);

            }
            else
            {
                if (!currentAnimation.IsName("Vault"))
                {
                    playerAnimator.CrossFade("Vault", animationTransitionTime, 0, 0f);
                }
            }
            Quaternion targetRotation = Quaternion.LookRotation(wallOppositeDirectionClimb);
            transform.rotation = targetRotation;


            currentClimbTime -= Time.fixedDeltaTime;
            float targetVerticalSpeed = climbSpeedMax;
            playerVerticalVelocity = Mathf.MoveTowards(playerVerticalVelocity, targetVerticalSpeed, (1f / climbSmoothTime) * Time.fixedDeltaTime);
            Vector3 climbMotion = new Vector3(0f, playerVerticalVelocity, 0f);
            controller.Move(climbMotion * Time.fixedDeltaTime);

        }
        else if (currentState == MovementState.Sliding)
        {
            if (!currentAnimation.IsName("Slide")) { playerAnimator.CrossFade("Slide", animationTransitionTime, 0); }
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
        Vector3 seperation = Vector3.up * castSeperation;
        RaycastHit hitWall;


        sphereCastHitCount = 0;
        for (int i = 0; i < 3; i++)
        {
            Vector3 worldControllerCenter = transform.TransformPoint(controller.center);
            Vector3 worldOffset = transform.TransformDirection(sphereCastOffsetWall);
            Vector3 castOrigin = (worldControllerCenter + worldOffset + seperation * i);

            if (Physics.SphereCast(castOrigin, sphereRadius, transform.forward, out hitWall, sphereCastHeight, climbable))
            {
                sphereCastHitCount++;

            }
            wallNearby = sphereCastHitCount > 0;

            if (wallNearby)
            {
                pureHorizontalNormal = new Vector3(hitWall.normal.x, 0f, hitWall.normal.z).normalized;
                slopeAngle = Vector3.Angle(hitWall.normal, Vector3.up);
            }
            if (inputHandler.moveInput.magnitude > 0.01f && wallNearby)
            {
                float dotproduct = Vector3.Dot(hitWall.normal, transform.forward);
                runningTowardsWall = dotproduct < -0.7f;
                wallOppositeDirectionClimb = -1f * hitWall.normal;
            }
            
        }
        
        Vector3 worldControllerCenter2 = transform.TransformPoint(controller.center);
        Vector3 worldOffset2 = transform.TransformDirection(rayCastOffsetMiddle);
        Vector3 castOrigin2 = (worldControllerCenter2 + worldOffset2);
        RaycastHit hitLedgeMid;

        castHitMiddle = Physics.Raycast(castOrigin2, transform.forward, out hitLedgeMid, castHeight, climbable);

        Vector3 worldControllerCenter3 = transform.TransformPoint(controller.center);
        Vector3 worldOffset3 = transform.TransformDirection(rayCastOffsetUpper);
        Vector3 castOrigin3 = (worldControllerCenter3 + worldOffset3);
        RaycastHit hitLedgeUpper;

        castHitUpper = Physics.Raycast(castOrigin3, transform.forward, out hitLedgeUpper, castHeight, climbable);

        canHang = !castHitUpper && castHitMiddle;

    }



}