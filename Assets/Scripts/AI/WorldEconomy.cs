using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Экономика мира для ИИ-торговцев.
/// Использует существующие CityData и TradeTransactionHandler напрямую.
///
/// Размести на том же GameObject что AITurnManager.
/// Заполни CityBindings: каждый City из сцены → его CityData ScriptableObject.
/// </summary>
public class WorldEconomy : MonoBehaviour
{
    [Header("Все CityData в игре")]
    [Tooltip("Тот же список что в TradeSystem.allCities")]
    [SerializeField] private List<CityData> allCities = new List<CityData>();

    [Header("Связь City (сцена) → CityData (ScriptableObject)")]
    [SerializeField] private List<CityBinding> cityBindings = new List<CityBinding>();

    private Dictionary<City, CityData> _cityDataMap;
    private int _turnNumber;

    // ------------------------------------------------------------------
    // Unity
    // ------------------------------------------------------------------

    private void Awake()
    {
        BuildCityMap();
    }

    private void BuildCityMap()
    {
        _cityDataMap = new Dictionary<City, CityData>();
        foreach (var b in cityBindings)
        {
            if (b.city == null || b.cityData == null)
            {
                Debug.LogWarning("[WorldEconomy] Пропущена пустая связь City → CityData");
                continue;
            }
            _cityDataMap[b.city] = b.cityData;
        }
        Debug.Log($"[WorldEconomy] Готово: {_cityDataMap.Count} городов.");
    }

    // ------------------------------------------------------------------
    // Торговые операции для ИИ
    // ------------------------------------------------------------------

    /// <summary>
    /// ИИ покупает товар. Цена меняется по тем же правилам что для игрока.
    /// Деньги списываются/начисляются напрямую через AITrader.
    /// </summary>
    public void Buy(City city, string itemName, int amount, ITrader trader)
    {
        var cityData = GetCityData(city);
        if (cityData == null) return;

        var cityItem = FindItem(cityData, itemName);
        if (cityItem == null)
        {
            Debug.LogWarning($"[WorldEconomy] '{itemName}' не найден в {city.CityName}");
            return;
        }

        // Проверки
        int totalCost = Mathf.RoundToInt(cityItem.currentBuyPrice * amount);
        if (trader.Gold < totalCost)
        {
            Debug.Log($"[WorldEconomy] {trader.DisplayName}: не хватает золота для покупки {itemName}");
            return;
        }
        if (cityItem.stock < amount)
        {
            Debug.Log($"[WorldEconomy] {trader.DisplayName}: не хватает запаса {itemName} в {city.CityName}");
            return;
        }

        // Применяем сделку напрямую — без PlayerInventory
        if (trader is AITrader ai)
            ai.SpendGold(totalCost);

        cityData.cityGold += totalCost;
        cityItem.stock    -= amount;

        // Сдвигаем цены вверх (как в ExecuteBuyTransaction)
        for (int i = 0; i < amount; i++)
        {
            cityItem.currentBuyPrice  = Mathf.Min(cityItem.maxBuyPrice,
                cityItem.currentBuyPrice  * (1f + cityItem.volatility));
            cityItem.currentSellPrice = Mathf.Min(cityItem.maxSellPrice,
                cityItem.currentSellPrice * (1f + cityItem.volatility));
        }

        cityItem.UpdateDemand();

        // Добавляем в инвентарь ИИ и записываем среднюю цену
        if (trader is AITrader aiTrader)
        {
            aiTrader.Inventory.Add(itemName, amount);
            aiTrader.RecordPurchase(itemName, amount, totalCost);
        }

        AIDebugLog.RecordTrade(
            traderName: trader.DisplayName,
            kind:       AIDebugLog.TradeType.Buy,
            itemName:   itemName,
            amount:     amount,
            totalCost:  totalCost,
            cityName:   city.CityName
        );


        Debug.Log($"[WorldEconomy] {trader.DisplayName} купил {amount}x {itemName} " +
                  $"в {city.CityName} за {totalCost}g");
    }

    /// <summary>
    /// ИИ продаёт товар. Цена падает по тем же правилам что для игрока.
    /// </summary>
    public void Sell(City city, string itemName, int amount, ITrader trader)
    {
        var cityData = GetCityData(city);
        if (cityData == null) return;

        var cityItem = FindItem(cityData, itemName);
        if (cityItem == null)
        {
            Debug.LogWarning($"[WorldEconomy] '{itemName}' не найден в {city.CityName}");
            return;
        }

        int totalValue = Mathf.RoundToInt(cityItem.currentSellPrice * amount);

        // Кастуем сразу — все операции с инвентарём через AITrader
        if (!(trader is AITrader aiTrader))
        {
            Debug.LogWarning($"[WorldEconomy] Sell: {trader.DisplayName} не является AITrader");
            return;
        }

        // Проверки
        if (cityData.cityGold < totalValue)
        {
            Debug.Log($"[WorldEconomy] {city.CityName} не может выкупить {itemName} — нет золота");
            return;
        }
        if (!aiTrader.Inventory.Has(itemName))
        {
            Debug.Log($"[WorldEconomy] {trader.DisplayName}: нет {itemName} для продажи");
            return;
        }

        // Применяем сделку напрямую
        aiTrader.AddGold(totalValue);

        cityData.cityGold -= totalValue;
        cityItem.stock    += amount;
        aiTrader.Inventory.Remove(itemName, amount);

        // Сдвигаем цены вниз (как в ExecuteSellTransaction)
        for (int i = 0; i < amount; i++)
        {
            cityItem.currentBuyPrice  = Mathf.Max(cityItem.minBuyPrice,
                cityItem.currentBuyPrice  * (1f - cityItem.volatility));
            cityItem.currentSellPrice = Mathf.Max(cityItem.minSellPrice,
                cityItem.currentSellPrice * (1f - cityItem.volatility));
        }

        cityItem.UpdateDemand();

        AIDebugLog.RecordTrade(
            traderName: aiTrader.DisplayName,
            kind:       AIDebugLog.TradeType.Sell,
            itemName:   itemName,
            amount:     amount,
            totalCost:  totalValue,
            cityName:   city.CityName
            );

        Debug.Log($"[WorldEconomy] {trader.DisplayName} продал {amount}x {itemName} " +
                  $"в {city.CityName} за {totalValue}g");
    }

    // ------------------------------------------------------------------
    // Снимок мира для планирования ИИ
    // ------------------------------------------------------------------

    /// <summary>
    /// Снимок текущих цен и запасов по всем городам.
    /// ИИ использует currentBuyPrice для оценки арбитража.
    /// </summary>
    public GameSnapshot TakeSnapshot()
    {
        _turnNumber++;
        var citySnapshots = new List<CitySnapshot>();

        foreach (var b in cityBindings)
        {
            if (b.city == null || b.cityData == null) continue;

            var prices = new Dictionary<string, float>();
            var stocks = new Dictionary<string, int>();

            foreach (var cityItem in b.cityData.items)
            {
                if (cityItem?.item == null) continue;
                string id = cityItem.item.itemName;

                // ИИ видит buyPrice (сколько стоит купить здесь)
                // и sellPrice (сколько заплатят если продать здесь)
                // Для арбитража: покупаем там где buyPrice низкий,
                //                продаём там где sellPrice высокий
                prices[id] = cityItem.currentBuyPrice;
                stocks[id] = cityItem.stock;
            }

            citySnapshots.Add(new CitySnapshot(b.city, prices, stocks));
        }

        return new GameSnapshot(
            cities:     citySnapshots,
            traders:    new List<TraderSnapshot>(),
            allGoods:   GetAllItemNames(),
            turnNumber: _turnNumber
        );
    }

    /// <summary>
    /// Цена продажи в городе — используется AiStrategy для поиска лучшего рынка сбыта.
    /// </summary>
    public float GetSellPrice(City city, string itemName)
    {
        var cityData = GetCityData(city);
        if (cityData == null) return 0f;
        var item = FindItem(cityData, itemName);
        return item?.currentSellPrice ?? 0f;
    }

    // ------------------------------------------------------------------
    // Вспомогательные
    // ------------------------------------------------------------------

    public CityData GetCityData(City city)
    {
        if (_cityDataMap == null) BuildCityMap();
        if (!_cityDataMap.TryGetValue(city, out var data))
        {
            Debug.LogWarning($"[WorldEconomy] CityData не найден для {city?.CityName}. " +
                             $"Проверь CityBindings.");
        }
        return data;
    }

    private CityData.CityItem FindItem(CityData cityData, string itemName)
    {
        foreach (var item in cityData.items)
            if (item?.item != null && item.item.itemName == itemName)
                return item;
        return null;
    }

    private List<string> GetAllItemNames()
    {
        var ids = new HashSet<string>();
        foreach (var city in allCities)
        {
            if (city?.items == null) continue;
            foreach (var item in city.items)
                if (item?.item != null)
                    ids.Add(item.item.itemName);
        }
        return new List<string>(ids);
    }

    // ------------------------------------------------------------------
    // Вложенный тип
    // ------------------------------------------------------------------

    [System.Serializable]
    public class CityBinding
    {
        [Tooltip("City MonoBehaviour из сцены")]
        public City     city;
        [Tooltip("CityData ScriptableObject для этого города")]
        public CityData cityData;
    }
}