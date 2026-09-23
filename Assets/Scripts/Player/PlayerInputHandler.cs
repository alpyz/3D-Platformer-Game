using System;
using UnityEngine;
using UnityEngine.InputSystem;


public enum PlayerModeState
{
    human,
    boulder,
}
public class PlayerInputHandler : MonoBehaviour
{
    private PlayerInputActions controls;

    public Vector2 moveInput { get; private set; }
    public bool jumpPressed { get; private set; }
    public bool jumpHeld { get; private set; }
    public bool sprintHeld { get; private set; }
    public bool aimed { get; private set; }
    public bool throwed { get;  set; }
    public bool cameraMoving { get; private set; }








    public bool secondPlayerJump { get; private set; }



    private void Awake()
    {
        if (controls == null)
            controls = new PlayerInputActions();
    }

    private void OnEnable()
    {
        if (controls == null)
            controls = new PlayerInputActions();

        controls.Movement.Enable();
        controls.Camera.Enable();
        controls.ModeChange.Enable();


        controls.Movement.Jump.started += OnJumpStarted;
        controls.Movement.Jump.canceled += OnJumpCanceled;
        controls.Movement.Sprint.started += OnSprintStarted;
        controls.Movement.Sprint.canceled += OnSprintCanceled;

        controls.Movement.Aim.started += OnAimStarted;
        controls.Movement.Aim.canceled += OnAimCanceled;
        controls.Movement.Throw.started += OnThrowStarted;
        controls.Movement.Throw.started += OnThrowCanceled;

        controls.Movement.JumpStart.started += OnJumpStartStarted;
        controls.Movement.JumpStart.canceled += OnJumpStartCanceled;




;
    }
    

    private void OnDisable()
    {
        controls.Movement.Jump.started -= OnJumpStarted;
        controls.Movement.Jump.canceled -= OnJumpCanceled;
        controls.Movement.Sprint.started -= OnSprintStarted;
        controls.Movement.Sprint.canceled -= OnSprintCanceled;



        controls.Movement.Aim.started -= OnAimStarted;
        controls.Movement.Aim.canceled -= OnAimCanceled;
        controls.Movement.Throw.started -= OnThrowStarted;
        controls.Movement.Throw.started -= OnThrowCanceled;




        controls.Movement.JumpStart.started -= OnJumpStartStarted;
        controls.Movement.JumpStart.canceled -= OnJumpStartCanceled;






        controls.Movement.Disable();
        controls.Camera.Disable();
        controls.ModeChange.Disable();
    }

    

    private void Update()
    {
        moveInput = controls.Movement.Move.ReadValue<Vector2>();
        
    }

    private void OnThrowStarted(InputAction.CallbackContext context)
    {
        if(aimed) throwed = true;

    }

    private void OnThrowCanceled(InputAction.CallbackContext context)
    {
        
    }


    private void OnAimStarted(InputAction.CallbackContext context)
    {
        aimed = true;
    }
    private void OnAimCanceled(InputAction.CallbackContext context)
    {

    }

    private void OnJumpStartStarted(InputAction.CallbackContext context)
    {
        secondPlayerJump = true;
    }
    private void OnJumpStartCanceled(InputAction.CallbackContext context)
    {
        secondPlayerJump = false;
    }


    private void OnSprintStarted(InputAction.CallbackContext context) => sprintHeld = true;
    private void OnSprintCanceled(InputAction.CallbackContext context) => sprintHeld = false;

    private void OnJumpStarted(InputAction.CallbackContext context)
    {
        jumpPressed = true;
        jumpHeld = true;
        aimed = false;
    }

    private void OnJumpCanceled(InputAction.CallbackContext context) => jumpHeld = false;

    public void ConsumeJumpPress() => jumpPressed = false;
}