using System.Collections.Generic;
using UnityEngine;

public class City : MonoBehaviour
{
    public List<Direction> directions;

    public void OpenPanel()
    {
        // �������� ������ ������
        Debug.Log("����������� ������ ������");
    }

    public void SelectDirection(Direction direction)
    {
        // ���������� ������ �� ������ ������ ���������� �����������
        direction.MoveToNextCell(GameManager.instance.playerToken);
    }

  
}