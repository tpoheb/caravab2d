using System.Collections.Generic;
using UnityEngine;

public class City : MonoBehaviour
{
    public List<Direction> directions;

    public void OpenPanel()
    {
        // Открытие панели города
        Debug.Log("Открывается панель города");
    }

    public void SelectDirection(Direction direction)
    {
        // Перемещаем игрока на первую клетку выбранного направления
        direction.MoveToNextCell(GameManager.instance.playerToken);
    }

  
}