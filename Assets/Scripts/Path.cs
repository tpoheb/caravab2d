using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class Path
{
    public CityCell startCity; // ��������� �����
    public CityCell endCity; // �������� �����
    public List<Transform> cells; // ������ ����

    // ���������� ������ ���� � �������� �������
    public List<Transform> GetReversedCells()
    {
        List<Transform> reversedCells = new List<Transform>(cells);
        reversedCells.Reverse();
        return reversedCells;
    }
}