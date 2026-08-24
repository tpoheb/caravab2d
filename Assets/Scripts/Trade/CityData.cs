using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
[CreateAssetMenu(fileName = "New City", menuName = "Trade/City Data")]
public class CityData : ScriptableObject
{
    public string cityName;
    public int cityGold;
    public List<CityItem> items = new List<CityItem>();

    [TextArea(3, 10)]
    public string info;

    public List<string> rumors = new List<string>();

    [Header("Столица")]
    [Tooltip("Отметьте, если это столица. Только в столице можно вступать в гильдии.")]
    public bool isCapital;

    [Header("Найм юнитов")]
    public List<UnitData> availableUnits = new List<UnitData>();

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

        // Спрос теперь вычисляется автоматически через UpdateDemand(),
        // поэтому скрываем поле от ручного редактирования в Inspector.
        [HideInInspector]
        public DemandLevel demand = DemandLevel.Normal;

        [Header("Dynamic Sell Pricing (Город покупает у игрока)")]
        public float baseSellPrice;
        public float currentSellPrice;
        public float minSellPrice;
        public float maxSellPrice;

        [Header("Settings")]
        public float volatility = 0.02f;
        public float regenRate = 0.10f;

        /// <summary>
        /// Порог отклонения цены от базовой, при котором появляется стрелка спроса.
        /// </summary>
        public const float DemandThreshold = 0.25f;

        // Свойства для UI, чтобы ничего не сломалось в ItemUI
        public int buyPrice => Mathf.RoundToInt(currentBuyPrice);
        public int sellPrice => Mathf.RoundToInt(currentSellPrice);

        public bool IsValid()
        {
            return item != null && stock >= 0 && baseBuyPrice > 0;
        }

        /// <summary>
        /// Автоматически пересчитывает уровень спроса на основе отклонения
        /// текущей цены покупки от базовой. Если цена выросла/упала более чем
        /// на DemandThreshold, выставляется High/Low, иначе Normal.
        /// </summary>
        public void UpdateDemand()
        {
            if (baseBuyPrice <= 0f)
            {
                demand = DemandLevel.Normal;
                return;
            }

            float deviation = (currentBuyPrice - baseBuyPrice) / baseBuyPrice;

            if (deviation >= DemandThreshold)
                demand = DemandLevel.High;
            else if (deviation <= -DemandThreshold)
                demand = DemandLevel.Low;
            else
                demand = DemandLevel.Normal;
        }

        public void RegeneratePrice()
        {
            // Восстанавливаем обе цены
            currentBuyPrice = currentBuyPrice + (baseBuyPrice - currentBuyPrice) * regenRate;
            currentBuyPrice = Mathf.Clamp(currentBuyPrice, minBuyPrice, maxBuyPrice);

            currentSellPrice = currentSellPrice + (baseSellPrice - currentSellPrice) * regenRate;
            currentSellPrice = Mathf.Clamp(currentSellPrice, minSellPrice, maxSellPrice);

            // Спрос мог вернуться к норме после регенерации цены
            UpdateDemand();
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
        if (rumors == null) rumors = new List<string>();
        if (availableUnits == null) availableUnits = new List<UnitData>();
    }
}