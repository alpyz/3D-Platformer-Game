using UnityEngine;
public enum TransformStates
{
    boulderToPogo,
    pogoToBoulder,
    pogoToMan,
    manToBoulder,
    manToPogo,
}
public enum PlayerState
{
    boulder,
    pogo,
    man,
}


public class PlayerTransform : MonoBehaviour // BU KOD playerlarda olacak büyük ihtimalle
{
    [Header("Player Objects")]
    [SerializeField] GameObject player1;
    [SerializeField] GameObject player2;
    [SerializeField] GameObject player3;

    [Header("States")]
    public PlayerState currentPlayerState;
    public TransformStates currentTransformState;

    [Header("Script References")]
    [SerializeField] PlayerInputHandler inputHandler;

    private void Update()
    {
        InputCheck();
        PlayerStateHandler();
        TransformStateHandler();
    }

    private void InputCheck() // boola çevir hepsinin ulaþtýðý
    {
        if (inputHandler.aimed) //diðer seçenekleri sonra ekle 3'ten 2'ye dönüþüm gibi
        {
            if (player1.activeSelf)
            {
                currentPlayerState = PlayerState.boulder;
            }
            else if (player2.activeSelf)
            {
                currentPlayerState = PlayerState.pogo;
            }
            else if (player3.activeSelf)
            {
                currentPlayerState = PlayerState.man;
            }
        }
    }
    private void PlayerStateHandler() // yukarý ve aþaðý dönüþüm boolu koy dönüþüm inputu true yapsýn.
    {
        switch(currentPlayerState)
        {
            case PlayerState.boulder:
                currentTransformState = TransformStates.boulderToPogo;
                break;
            case PlayerState.pogo:
                break;
            case PlayerState.man:
                break;
        }
    }
    private void TransformStateHandler()
    {
        switch(currentTransformState)
        {

        }
    }

}
