using System.Timers;
using Unity.Cinemachine;
using UnityEngine;

using UnityEngine.UIElements;


public enum BoulderState
{
    BoulderGrounded,
    BoulderAirborne,
}
public enum BoulderSpeedState
{
    BoulderSlow,
    BoulderModerate,
    BoulderFast,
}


public class PlayerBoulder : MonoBehaviour
{
    [Header("Essentials")]
    Rigidbody rb;
    PlayerInputHandler inputHandler;
    public BoulderState currentBoulderState  = BoulderState.BoulderGrounded;
    public BoulderSpeedState currentBoulderSpeedState = BoulderSpeedState.BoulderSlow;
    private Vector2 inputToRotate;
    public float rotationSpeed2 = 720f;
    public CinemachineCamera playerCamera;
    SphereCollider colliderBall;

    [Header("Ground Check")]
    public bool boulderGrounded; 
    public Vector3 sphereCastOffset;
    public float sphereCastLength;
    public Vector3 rayCastOffset;
    public float rayCastLength;
    public LayerMask groundLayer;



    [Header("Movement")]
    public float boulderRotationSpeed;
    public float boulderForce;
    public float boulderSpeedMax;
    public float boulderPivotingSpeed = 5f;
    public bool isPivoting;
    public Transform deneme;

    [Header("Jump & Gravity")]
    public float boulderGravityMax = -10f;
    public float boulderGravityIncrease = -5f;
    private float boulderDownWardMax = -10f;
    private float boulderDownwardForce = -50f;
    

    private void Start()
    {
        rb = GetComponent<Rigidbody>();
        colliderBall = GetComponent<SphereCollider>();
        inputHandler = GetComponentInParent<PlayerInputHandler>();

        
    }
    private void FixedUpdate()
    {
        GroundCheck();
        BoulderStateHandler();
        BoulderHorizontal();
        BoulderVertical();
    }
    private void BoulderStateHandler()
    {
        switch (currentBoulderState)
        {
            case BoulderState.BoulderGrounded:
                if (!boulderGrounded)
                {
                    currentBoulderState = BoulderState.BoulderAirborne;
                }
                break;
            case BoulderState.BoulderAirborne:
                if (boulderGrounded)
                {
                    currentBoulderState = BoulderState.BoulderGrounded;
                }
                break;
        }
    }
    private void GroundCheck()
    {
        Vector3 worldControllerCenter = transform.TransformPoint(colliderBall.center);
        Vector3 worldOffset = transform.TransformDirection(sphereCastOffset);
        Vector3 castOrigin = (worldControllerCenter + worldOffset);
        RaycastHit hitGround;

        boulderGrounded = (Physics.SphereCast(castOrigin, colliderBall.radius + 0.01f, -transform.up, out hitGround, sphereCastLength, groundLayer)) ;

    }

    private void BoulderHorizontal()
    {
        


        inputToRotate = inputHandler.moveInput;
        float inputMagnitude = Mathf.Clamp01(inputToRotate.magnitude);
        float angleDifference = 0f;

        if (inputMagnitude > 0.1f && currentBoulderState == BoulderState.BoulderGrounded)
        {
            float inputAngle = Mathf.Atan2(inputToRotate.x, inputToRotate.y) * Mathf.Rad2Deg;
            float cameraAngle = playerCamera.transform.rotation.eulerAngles.y;
            float targetAngle = inputAngle + cameraAngle;

            Quaternion targetRotation = Quaternion.Euler(0f, targetAngle, 0f);
            angleDifference = Quaternion.Angle(transform.rotation, targetRotation);



            if (!isPivoting && angleDifference > 110f&&rb.linearVelocity.magnitude<boulderPivotingSpeed)
            {
                isPivoting = true;
            }

            if (isPivoting && currentBoulderState == BoulderState.BoulderGrounded)
            {
                rb.linearVelocity= Vector3.zero;
            }


            transform.rotation = Quaternion.RotateTowards(transform.rotation, targetRotation, rotationSpeed2 * Time.fixedDeltaTime);
            rb.AddForce(transform.forward*boulderForce*inputHandler.moveInput.magnitude*Time.fixedDeltaTime);



            
        }

        if (isPivoting && angleDifference < 15f)
        {
            isPivoting = false;
        }

        if (inputMagnitude <= 0.05f)
        {
            isPivoting = false;
        }




        Vector3 horizontalVelocity = new Vector3(rb.linearVelocity.x, 0, rb.linearVelocity.z);
        float speed = horizontalVelocity.magnitude;
        if (speed > 0.001)
        {
            Vector3 moveDirection = horizontalVelocity.normalized;
            Vector3 perpendicularRight = Vector3.Cross( Vector3.up,moveDirection).normalized;
            float distanceTraveled = speed* Time.fixedDeltaTime;
            float angleRad = distanceTraveled / colliderBall.radius;
            float angleDeg = angleRad * Mathf.Rad2Deg;

            deneme.Rotate(perpendicularRight, angleDeg, Space.World);


            if(speed> boulderSpeedMax)
            {
                Vector3 clampedHorizontalVelocity = moveDirection * boulderSpeedMax;
                rb.linearVelocity = new Vector3(clampedHorizontalVelocity.x,rb.linearVelocity.y,clampedHorizontalVelocity.z);

            }




        }       
        
        
    }

    private void BoulderVertical()
    {
        if(boulderGravityMax < rb.linearVelocity.y&&currentBoulderState == BoulderState.BoulderAirborne)
        {
            rb.AddForce(Vector3.up * boulderGravityIncrease * Time.fixedDeltaTime);
        }
        else if(boulderDownWardMax< rb.linearVelocity.y && currentBoulderState == BoulderState.BoulderGrounded)
        {
            rb.AddForce(Vector3.up * boulderDownwardForce * Time.fixedDeltaTime);
        }
    }

}
