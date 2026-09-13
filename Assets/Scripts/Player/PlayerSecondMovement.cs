using Unity.Cinemachine;
using UnityEngine;
using UnityEngine.UIElements;


public enum SecondPlayerState
{
    Grounded,
    Airborne,
    Fall,
    Slip,
}



public class PlayerSecondMovement : MonoBehaviour
{

    public SecondPlayerState currentState = SecondPlayerState.Grounded;

    [Header("Essentials")]
    PlayerInputHandler inputHandler;
    CharacterController controller;


    [Header("Animation")]
    [SerializeField] Animator playerAnimator2;
    private float animationTransitionTime = 0.00f;
    AnimatorStateInfo currentAnimation;

    [Header("Camera")]
    public CinemachineCamera playerCamera;

    [Header("HeadHit")]
    private int playerHits = 0;
    public int hitCountMax = 4;
    public bool dizzy;
    public float dizzyTimer = 0f;
    private const float DIZZY_TIMER = 2f;
    public float fallingVerticalVelocity = -15f;
    public bool isTouchingCeiling;

    [Header("Move")]
    public bool movementStarted;
    public float moveSpeedMax = 15f;
    private Vector3 smoothMoveVelocity;
    private Vector3 movementHorizontal;
    public float smoothTime = 0.15f;
    private Vector3 finalMovement;

    [Header("Jump")]
    public float jumpPowerModifier;
    public bool jumping;
    public bool jumped;
    private float jumpTimer;
    private const float JUMP_BUFFER_DURATION = 0.5f;

    [Header("Player Rotation")]
    Vector2 inputToRotate;
    public float rotationSpeed;


    [Header("Gravity")]
    public float playerVerticalVelocity;
    public float gravitationalAcceleration;
    public float gravityMax;
    public float groundedVerticalVelocity;
    public Vector3 sphereCastOffsetCeiling;
    public float sphereCastHeightCeiling;

    [Header("LayerMasks")]
    public LayerMask climbable;


    private void Start()
    {
        inputHandler = GetComponent<PlayerInputHandler>();
        controller = GetComponent<CharacterController>();
    }


    private void FixedUpdate()
    {
        if (inputHandler.secondPlayerJump && currentState == SecondPlayerState.Grounded&& !dizzy)
        {
            movementStarted = true;
        }
        Timers();
        AnimatorCheck(); 
        SecondStateHandler();
    }
    private void Timers()
    {
        if (jumpTimer > 0f)
        {
            jumpTimer -= Time.fixedDeltaTime;
            jumping = true;
        }
        else
        {
            jumping = false;
        }

        if (dizzyTimer > 0f)
        {

            dizzyTimer -= Time.fixedDeltaTime;
            dizzy = true;
        }
        else
        {
            dizzy=false;
            
        }

    }
    private void SecondStateHandler()
    {
        switch (currentState)
        {
            case SecondPlayerState.Grounded:
                playerVerticalVelocity = groundedVerticalVelocity;
                movementHorizontal = Vector3.zero;
                if (currentAnimation.IsName("2HeadHit"))
                {
                    currentState = SecondPlayerState.Fall;
                }


                VerticalMovement();
                if (!controller.isGrounded)
                {
                    currentState = SecondPlayerState.Airborne;
                }
                break;
            case SecondPlayerState.Airborne:
                jumped = false;
                HorizontalMovement();
                VerticalMovement();
                if (controller.isGrounded)
                {
                    currentState = SecondPlayerState.Grounded;
                }
                break;
            case SecondPlayerState.Fall:

                if (!currentAnimation.IsName("2Crash"))
                {
                    dizzy = true;
                    playerAnimator2.CrossFade("2Crash", animationTransitionTime, 0, 0.3f);
                    dizzyTimer = DIZZY_TIMER;
                    movementStarted = false;
                    playerHits = 0;

                }
                if (!dizzy)
                {
                    currentState = SecondPlayerState.Grounded;
                    
                }
                break;
        }
    }
    private void AnimatorCheck()
    {
        if (playerAnimator2 != null) currentAnimation = playerAnimator2.GetCurrentAnimatorStateInfo(0);
    }
    private void HorizontalMovement()
    {
        Vector3 targetVelocity;
        float inputAngle = Mathf.Atan2(inputHandler.moveInput.x, inputHandler.moveInput.y) * Mathf.Rad2Deg;
        float rotationAngle = inputAngle + transform.rotation.eulerAngles.y;
        Quaternion targetRotation = Quaternion.Euler(0f, rotationAngle, 0f);
        float angleDifference = Quaternion.Angle(transform.rotation, targetRotation);
        if (angleDifference < 25f)
        {
            targetVelocity = transform.forward * inputHandler.moveInput.magnitude * moveSpeedMax;
            movementHorizontal = Vector3.SmoothDamp(movementHorizontal, targetVelocity, ref smoothMoveVelocity, smoothTime, Mathf.Infinity, Time.fixedDeltaTime);
        }
        if (angleDifference >= 25f&&angleDifference < 50f)
        {
            transform.rotation = Quaternion.RotateTowards(transform.rotation, targetRotation, rotationSpeed * Time.fixedDeltaTime);
            targetVelocity = transform.forward * inputHandler.moveInput.magnitude * moveSpeedMax;
            movementHorizontal = Vector3.SmoothDamp(movementHorizontal, targetVelocity, ref smoothMoveVelocity, smoothTime, Mathf.Infinity, Time.fixedDeltaTime);
        }
        else if (angleDifference < 130f && angleDifference > 50f)
        {
            transform.rotation = Quaternion.RotateTowards(transform.rotation, targetRotation, rotationSpeed * Time.fixedDeltaTime);
        }
        else
        {
            Vector3 vel1 = transform.right * inputHandler.moveInput.x * moveSpeedMax;
            Vector3 vel2 = transform.forward * inputHandler.moveInput.y * moveSpeedMax;
            targetVelocity = vel1 + vel2;
            movementHorizontal = Vector3.SmoothDamp(movementHorizontal, targetVelocity, ref smoothMoveVelocity, smoothTime, Mathf.Infinity, Time.fixedDeltaTime);
        }





    }

    private void VerticalMovement()
    {

        if (currentState == SecondPlayerState.Airborne)
        {
            
            playerVerticalVelocity -= gravitationalAcceleration * Time.fixedDeltaTime;
            playerVerticalVelocity = Mathf.Max(playerVerticalVelocity, gravityMax);


        }

        if(movementStarted&& currentState == SecondPlayerState.Grounded)
        {
            jumping = true;
        }

        if(currentState == SecondPlayerState.Grounded)
        {
            if (jumping&&!jumped)
            {
                //anim oynayacayak frame'e göre zýplayacak altta
                playerAnimator2.CrossFade("2Jump", animationTransitionTime, 0, 0.0f);
                jumpTimer = JUMP_BUFFER_DURATION;
                jumped = true;

            }
            if (currentAnimation.IsName("2Jump") )
            {
                
                float animationPercentage = currentAnimation.normalizedTime % 1.0f;
                if (animationPercentage>= 0.35f)
                {
                    
                    playerVerticalVelocity = 0.2f * jumpPowerModifier;
                    
                }

            }

        }
        

        Vector3 motion = new Vector3(0, playerVerticalVelocity, 0);
        finalMovement = movementHorizontal + motion;
        controller.Move(finalMovement * Time.fixedDeltaTime);




        Vector3 worldControllerCenter = transform.TransformPoint(controller.center);
        Vector3 castOrigin = worldControllerCenter + sphereCastOffsetCeiling;
        RaycastHit hitCeiling;

        if (Physics.SphereCast(castOrigin, controller.radius, transform.up, out hitCeiling, sphereCastHeightCeiling, climbable))
        {
            if (hitCeiling.normal.y < -0.5f)
            {
                if(isTouchingCeiling) return;
                playerHits++;
                
                
                jumpTimer = 0f;
                jumping = false;
                if(playerHits >= hitCountMax)
                {
                    playerVerticalVelocity = fallingVerticalVelocity;
                    if (!currentAnimation.IsName("2HeadHit")) { playerAnimator2.CrossFade("2HeadHit", animationTransitionTime, 0, 0.0f); }
                }
                else
                {
                    playerVerticalVelocity = groundedVerticalVelocity;
                    playerAnimator2.CrossFade(currentAnimation.fullPathHash, animationTransitionTime, 0, 0.9f);
                }
                isTouchingCeiling = true;
            }
        }
        else
        {
            isTouchingCeiling = false;
        }
    }


}
