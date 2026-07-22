using UnityEngine;
using static UnityEditorInternal.VersionControl.ListControl;

public class PlayerModeHandler : MonoBehaviour
{
    [Header("Essentials")]
    PlayerInputHandler inputHandler;
    PlayerMovement movement;
    PlayerBoulder boulder;
    CharacterController controller;
    Rigidbody rb;
    SphereCollider sphereCollider;
    private PlayerModeState lastState;

    private void Start()
    {
        inputHandler = GetComponent<PlayerInputHandler>();    
        movement = GetComponent<PlayerMovement>();
        boulder = GetComponent<PlayerBoulder>();
        controller = GetComponent<CharacterController>();
        rb= GetComponent<Rigidbody>();
        sphereCollider = GetComponent<SphereCollider>();

        lastState = inputHandler.state;
        ApplyStateChanges(lastState);
    }

    private void Update()
    {

        if (inputHandler.state != lastState)
        {
            lastState = inputHandler.state;
            ApplyStateChanges(lastState);
        }


        
    }

    private void ApplyStateChanges(PlayerModeState currentState)
    {
        switch (currentState)
        {
            case PlayerModeState.human:
                boulder.enabled = false;
                sphereCollider.enabled = false;

                rb.isKinematic = true;

                controller.enabled = true;
                movement.enabled = true;
                break;

            case PlayerModeState.boulder:

                movement.enabled = false;
                controller.enabled = false; 


                rb.isKinematic = false;
                sphereCollider.enabled = true;
                boulder.enabled = true;
                break;
        }
    }
}
