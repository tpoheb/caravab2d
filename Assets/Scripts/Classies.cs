using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class City
{
    public string cityName; // Название города
    public GameObject cityPanel; // Панель действий в городе
    public List<Direction> directions; // Доступные направления из города
}

[System.Serializable]
public class Direction
{
    public City destinationCity; // Город назначения
    public List<Tile> tiles; // Клетки на пути к городу назначения
}

[System.Serializable]
public class Tile
{
    public Sprite tileSprite; // Спрайт клетки
    public string tileName; // Имя клетки (опционально)
}