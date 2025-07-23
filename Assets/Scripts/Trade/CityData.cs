using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
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
        public int buyPrice;
        public int sellPrice;

        // Добавляем валидацию
        public bool IsValid()
        {
            return item != null && stock >= 0 && buyPrice >= 0 && sellPrice >= 0;
        }
    }

    // Метод для проверки валидности данных города
    public bool IsValid()
    {
        if (string.IsNullOrEmpty(cityName))
            return false;

        if (items == null)
            items = new List<CityItem>();

        return true;
    }

    private void OnValidate()
    {
        if (items == null)
            items = new List<CityItem>();
    }
}