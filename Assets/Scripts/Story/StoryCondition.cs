using System;
using UnityEngine;

namespace StorySystem
{
    public enum StoryConditionType
    {
        ArriveAtCity,           // Игрок прибыл в конкретный город
        CollectEventCards,      // Собрано N карт-событий (или конкретная карта по ID)
        HaveItemInInventory,    // В инвентаре есть товар с конкретным ID
        HireUnitByType          // Нанят член команды конкретного типа
    }

    [Serializable]
    public class StoryCondition
    {
        [Tooltip("Тип условия")]
        public StoryConditionType conditionType;

        [Header("ArriveAtCity")]
        [Tooltip("ID города (CityData.cityId). Используется при conditionType = ArriveAtCity")]
        public string targetCityId;

        [Header("CollectEventCards")]
        [Tooltip("Минимальное количество карт событий. 0 = не проверяем количество")]
        public int requiredCardCount;
        [Tooltip("Конкретный ID карты. Пусто = любая карта события")]
        public string specificCardId;

        [Header("HaveItemInInventory")]
        [Tooltip("ID товара из TradeSystem. Используется при conditionType = HaveItemInInventory")]
        public string requiredItemId;
        [Tooltip("Минимальное количество товара. По умолчанию 1")]
        public int requiredItemAmount = 1;

        [Header("HireUnitByType")]
        [Tooltip("Тип юнита (соответствует полю в UnitData). Используется при conditionType = HireUnitByType")]
        public string requiredUnitType;
    }
}
