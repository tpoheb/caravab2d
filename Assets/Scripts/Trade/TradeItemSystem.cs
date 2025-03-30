using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class TradeItemSystem : MonoBehaviour
{
    public PlayerInventory playerInventory;
    public List<CityData> cities;
    private CityData currentCity;
    [SerializeField] private PlayerStats playerStats;

    [Header("UI References")]
    public Text playerMoneyText;
    public Text cityMoneyText;
    public Text cityNameText;

    public GameObject tradePanel;
    public Transform itemsContainer;
    public GameObject itemUIPrefab;

    public void OpenCityTrade(CityData city)
    {
        currentCity = city;
        tradePanel.SetActive(true);
        UpdateUI();
    }

    private void UpdateUI()
    {
        // Очистка контейнера
        foreach (Transform child in itemsContainer)
            Destroy(child.gameObject);

        // Обновление заголовков
        cityNameText.text = currentCity.cityName;
        playerMoneyText.text = $"Золото: {playerInventory.money}";
        cityMoneyText.text = $"Казна: {currentCity.cityGold}";

        // Создание элементов UI
        foreach (var cityItem in currentCity.items)
        {
            var itemUI = Instantiate(itemUIPrefab, itemsContainer);
            var controller = itemUI.GetComponent<ItemUI>();

            int playerStock = playerInventory.GetItemStock(cityItem.item);
            controller.Initialize(cityItem, playerStock, this);
        }
    }

    public void BuyItem(CityData.CityItem cityItem, int quantity)
    {
        // Рассчитываем цену с учетом выгоды (скидка на покупку)
        float bargainDiscount = Mathf.Clamp01(playerStats.Bargain * 0.01f); // 1% скидки за единицу выгоды
        int adjustedPricePerUnit = Mathf.RoundToInt(cityItem.buyPrice * (1f - bargainDiscount));
        int totalCost = adjustedPricePerUnit * quantity;

        // Проверка грузоподъемности
        int totalWeight = cityItem.item.weight * quantity;
        if (!playerInventory.CanCarryMore(totalWeight))
        {
            Debug.Log("Не хватает грузоподъемности!");
            return;
        }

        if (playerInventory.money >= totalCost &&
            cityItem.stock >= quantity)
        {
            playerInventory.money -= totalCost;
            currentCity.cityGold += totalCost;
            cityItem.stock -= quantity;
            playerInventory.AddItem(cityItem.item, quantity);

            Debug.Log($"Куплено {quantity} {cityItem.item.name} за {totalCost} (Цена за единицу: {adjustedPricePerUnit}, базовая цена: {cityItem.buyPrice})");
            UpdateUI();
        }
        else
        {
            Debug.Log($"Недостаточно средств или товара! Нужно: {totalCost}, есть: {playerInventory.money} | На складе: {cityItem.stock}");
        }
    }

    public void SellItem(CityData.CityItem cityItem, int quantity)
    {
        // Рассчитываем цену с учетом выгоды (надбавка на продажу)
        float bargainBonus = Mathf.Clamp01(playerStats.Bargain * 0.01f); // 1% надбавки за единицу выгоды
        int adjustedPricePerUnit = Mathf.RoundToInt(cityItem.sellPrice * (1f + bargainBonus));
        int totalValue = adjustedPricePerUnit * quantity;

        int playerStock = playerInventory.GetItemStock(cityItem.item);

        if (playerStock >= quantity &&
            currentCity.cityGold >= totalValue)
        {
            playerInventory.money += totalValue;
            currentCity.cityGold -= totalValue;
            cityItem.stock += quantity;
            playerInventory.RemoveItem(cityItem.item, quantity);

            Debug.Log($"Продано {quantity} {cityItem.item.name} за {totalValue} (Цена за единицу: {adjustedPricePerUnit}, базовая цена: {cityItem.sellPrice})");
            UpdateUI();
        }
        else
        {
            Debug.Log($"Недостаточно товара или у города нет денег! Нужно товара: {quantity}, есть: {playerStock} | Нужно денег у города: {totalValue}, есть: {currentCity.cityGold}");
        }
    }

    public void CloseTrade()
    {
        tradePanel.SetActive(false);
    }

 
}