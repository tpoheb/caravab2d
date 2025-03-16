using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class Path
{
    public CityCell startCity; // Начальный город
    public CityCell endCity; // Конечный город
    public List<Transform> cells; // Клетки пути

    // Возвращает клетки пути в обратном порядке
    public List<Transform> GetReversedCells()
    {
        List<Transform> reversedCells = new List<Transform>(cells);
        reversedCells.Reverse();
        return reversedCells;
    }
}