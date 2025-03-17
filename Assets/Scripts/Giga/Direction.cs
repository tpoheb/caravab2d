using System.Collections.Generic;
using UnityEngine;

public class Direction : MonoBehaviour
{
    public List<Cell> cells;

    public void MoveToNextCell(PlayerToken player)
    {
        if (cells.Count > 0 && !cells[0].IsOccupied)
        {
            player.transform.position = cells[0].transform.position;
            cells[0].IsOccupied = true;
        }
    }
}