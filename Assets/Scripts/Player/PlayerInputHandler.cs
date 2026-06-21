using System;
using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerInputHandler : MonoBehaviour
{
    private PlayerInputActions controls;
    public Vector2 moveInput { get; private set; }
    public bool jumpPressed { get; private set; }
    public bool jumpHeld { get; private set; }
    public bool sprintHeld { get; private set; }
    private void Awake()
    {
        controls = new PlayerInputActions();
    }

    private void OnEnable()
    {
        controls.Movement.Enable();

        controls.Movement.Jump.started += OnJumpStarted;
        controls.Movement.Jump.canceled += OnJumpCanceled;
        controls.Movement.Sprint.started += OnSprintStarted;
        controls.Movement.Sprint.canceled += OnSprintCanceled;
    }

    

    private void OnDisable()
    {
        controls.Movement.Jump.started -= OnJumpStarted;
        controls.Movement.Jump.canceled -= OnJumpCanceled;
        controls.Movement.Sprint.started -= OnSprintStarted;
        controls.Movement.Sprint.canceled -= OnSprintCanceled;
        controls.Movement.Disable();
    }

    private void Update()
    {
        moveInput = controls.Movement.Move.ReadValue<Vector2>();
    }
    private void OnSprintStarted(InputAction.CallbackContext context)
    {
        sprintHeld = true;
    }

    private void OnSprintCanceled(InputAction.CallbackContext context)
    {
        sprintHeld = false;
    }



    private void OnJumpStarted(InputAction.CallbackContext context)
    {
        jumpPressed = true;
        jumpHeld = true;
    }

    private void OnJumpCanceled(InputAction.CallbackContext context)
    {
        jumpHeld = false;
    }


    public void ConsumeJumpPress()
    {
        jumpPressed = false;
    }
}
