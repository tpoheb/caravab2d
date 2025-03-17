using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class City
{
    public string cityName; // �������� ������
    public GameObject cityPanel; // ������ �������� � ������
    public List<Direction> directions; // ��������� ����������� �� ������
}

[System.Serializable]
public class Direction
{
    public City destinationCity; // ����� ����������
    public List<Tile> tiles; // ������ �� ���� � ������ ����������
}

[System.Serializable]
public class Tile
{
    public Sprite tileSprite; // ������ ������
    public string tileName; // ��� ������ (�����������)
}