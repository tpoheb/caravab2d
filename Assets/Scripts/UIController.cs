using UnityEngine;

public class UIController : MonoBehaviour
{
    public GameManager gameManager;

    public void OnEndTurnButtonClicked()
    {
        gameManager.EndTurn();
    }
}