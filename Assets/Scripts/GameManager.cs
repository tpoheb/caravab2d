using System.Collections.Generic;
using UnityEngine;

public class GameManager : MonoBehaviour
{
    public List<Transform> cells; // ������ ������
    public Transform player; // ����� ������
    private int currentCellIndex = 0; // ������� ������

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
            Debug.Log("���� ��������!");
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