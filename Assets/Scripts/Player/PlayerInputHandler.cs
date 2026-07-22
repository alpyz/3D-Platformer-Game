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
    public bool cameraMoving { get; private set; }

    public PlayerModeState state  = PlayerModeState.boulder;

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



        controls.ModeChange.Boulder.performed += OnBoulderPerformed;
        controls.ModeChange.Human.performed += OnHumanPerformed;
    }

    

    private void OnDisable()
    {
        controls.Movement.Jump.started -= OnJumpStarted;
        controls.Movement.Jump.canceled -= OnJumpCanceled;
        controls.Movement.Sprint.started -= OnSprintStarted;
        controls.Movement.Sprint.canceled -= OnSprintCanceled;




        controls.ModeChange.Boulder.performed -= OnBoulderPerformed;
        controls.ModeChange.Human.performed -= OnHumanPerformed;



        controls.Movement.Disable();
        controls.Camera.Disable();
        controls.ModeChange.Disable();
    }

    

    private void Update()
    {
        moveInput = controls.Movement.Move.ReadValue<Vector2>();
    }
    private void OnHumanPerformed(InputAction.CallbackContext context)
    {
        state = PlayerModeState.human;
    }

    private void OnBoulderPerformed(InputAction.CallbackContext context)
    {
        state = PlayerModeState.boulder;
    }


   

    private void OnSprintStarted(InputAction.CallbackContext context) => sprintHeld = true;
    private void OnSprintCanceled(InputAction.CallbackContext context) => sprintHeld = false;

    private void OnJumpStarted(InputAction.CallbackContext context)
    {
        jumpPressed = true;
        jumpHeld = true;
    }

    private void OnJumpCanceled(InputAction.CallbackContext context) => jumpHeld = false;

    public void ConsumeJumpPress() => jumpPressed = false;
}