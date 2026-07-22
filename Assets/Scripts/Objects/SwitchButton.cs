using UnityEngine;

public class SwitchButton : MonoBehaviour
{
    public bool buttonOn;
    public bool buttonReset = true;
    private void OnCollisionEnter(Collision collision)
    {
        
        if (buttonReset )
        {
            buttonOn = !buttonOn;
            buttonReset = false;
        }
    }
    private void OnCollisionExit(Collision collision)
    {
        buttonReset = true;
    }
    private void Update()
    {
        if (buttonOn)
        {
           // transform.rotation = ;
        }
    }

}
