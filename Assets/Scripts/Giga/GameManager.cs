using UnityEngine;

public class GameManager : MonoBehaviour
{
    public static GameManager instance;

    public PlayerToken playerToken;
    public City[] cities;

    private void Awake()
    {
        if (instance == null)
        {
            instance = this;
        }
        else
        {
            Destroy(gameObject);
        }
    }

    public void EndTurnButtonPressed()
    {
        // ѕровер€ем, есть ли направление дл€ перемещени€
        if (playerToken.currentCity.directions.Count > 0)
        {
            playerToken.currentCity.SelectDirection(playerToken.currentCity.directions[0]);
        }
    }
}