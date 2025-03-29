using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "New City", menuName = "Trade/City Data")]
public class CityData : ScriptableObject
{
    public string cityName;
    public int cityGold;
    public List<CityItem> items = new List<CityItem>();

    [System.Serializable]
    public class CityItem
    {
        public Item item;
        public int stock;
        public int buyPrice; // Цена покупки у города
        public int sellPrice; // Цена продажи городу
    }
}