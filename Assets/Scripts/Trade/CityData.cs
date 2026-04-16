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
        
        [Header("Dynamic Buy Pricing (Город продает игроку)")]
        public float baseBuyPrice;      
        public float currentBuyPrice;   
        public float minBuyPrice;       
        public float maxBuyPrice;       

        [Header("Dynamic Sell Pricing (Город покупает у игрока)")]
        public float baseSellPrice;
        public float currentSellPrice;
        public float minSellPrice;
        public float maxSellPrice;

        [Header("Settings")]
        public float volatility = 0.02f; 
        public float regenRate = 0.10f;  

        // Свойства для UI, чтобы ничего не сломалось в ItemUI
        public int buyPrice => Mathf.RoundToInt(currentBuyPrice);
        public int sellPrice => Mathf.RoundToInt(currentSellPrice);

        public bool IsValid()
        {
            return item != null && stock >= 0 && baseBuyPrice > 0;
        }

        public void RegeneratePrice()
        {
            // Восстанавливаем обе цены
            currentBuyPrice = currentBuyPrice + (baseBuyPrice - currentBuyPrice) * regenRate;
            currentBuyPrice = Mathf.Clamp(currentBuyPrice, minBuyPrice, maxBuyPrice);

            currentSellPrice = currentSellPrice + (baseSellPrice - currentSellPrice) * regenRate;
            currentSellPrice = Mathf.Clamp(currentSellPrice, minSellPrice, maxSellPrice);
        }
    }

    public bool IsValid()
    {
        if (string.IsNullOrEmpty(cityName)) return false;
        if (items == null) items = new List<CityItem>();
        return true;
    }

    private void OnValidate()
    {
        if (items == null) items = new List<CityItem>();
    }
}