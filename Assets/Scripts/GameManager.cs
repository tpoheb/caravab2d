using System.Collections.Generic;
using UnityEngine;

public class GameManager : MonoBehaviour
{
    public List<Transform> cells; // Список клеток
    public Transform player; // Фишка игрока
    private int currentCellIndex = 0; // Текущая клетка

    public void EndTurn()
    {
        if (currentCellIndex < cells.Count - 1)
        {
            currentCellIndex++;
            MovePlayerToCell(currentCellIndex);
            CheckCellType();
        }
        else
        {
            Debug.Log("Игра окончена!");
        }
    }

    private void MovePlayerToCell(int cellIndex)
    {
        player.position = cells[cellIndex].position;
    }

    private void CheckCellType()
    {
        CityCell cityCell = cells[currentCellIndex].GetComponent<CityCell>();
        if (cityCell != null)
        {
            cityCell.OnPlayerEnter();
        }
    }
}