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
        // ������� ����������
        foreach (Transform child in itemsContainer)
            Destroy(child.gameObject);

        // ���������� ����������
        cityNameText.text = currentCity.cityName;
        playerMoneyText.text = $"������: {playerInventory.money}";
        cityMoneyText.text = $"�����: {currentCity.cityGold}";

        // �������� ��������� UI
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
        // ������������ ���� � ������ ������ (������ �� �������)
        float bargainDiscount = Mathf.Clamp01(playerStats.Bargain * 0.01f); // 1% ������ �� ������� ������
        int adjustedPricePerUnit = Mathf.RoundToInt(cityItem.buyPrice * (1f - bargainDiscount));
        int totalCost = adjustedPricePerUnit * quantity;

        // �������� ����������������
        int totalWeight = cityItem.item.weight * quantity;
        if (!playerInventory.CanCarryMore(totalWeight))
        {
            Debug.Log("�� ������� ����������������!");
            return;
        }

        if (playerInventory.money >= totalCost &&
            cityItem.stock >= quantity)
        {
            playerInventory.money -= totalCost;
            currentCity.cityGold += totalCost;
            cityItem.stock -= quantity;
            playerInventory.AddItem(cityItem.item, quantity);

            Debug.Log($"������� {quantity} {cityItem.item.name} �� {totalCost} (���� �� �������: {adjustedPricePerUnit}, ������� ����: {cityItem.buyPrice})");
            UpdateUI();
        }
        else
        {
            Debug.Log($"������������ ������� ��� ������! �����: {totalCost}, ����: {playerInventory.money} | �� ������: {cityItem.stock}");
        }
    }

    public void SellItem(CityData.CityItem cityItem, int quantity)
    {
        // ������������ ���� � ������ ������ (�������� �� �������)
        float bargainBonus = Mathf.Clamp01(playerStats.Bargain * 0.01f); // 1% �������� �� ������� ������
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

            Debug.Log($"������� {quantity} {cityItem.item.name} �� {totalValue} (���� �� �������: {adjustedPricePerUnit}, ������� ����: {cityItem.sellPrice})");
            UpdateUI();
        }
        else
        {
            Debug.Log($"������������ ������ ��� � ������ ��� �����! ����� ������: {quantity}, ����: {playerStock} | ����� ����� � ������: {totalValue}, ����: {currentCity.cityGold}");
        }
    }

    public void CloseTrade()
    {
        tradePanel.SetActive(false);
    }

 
}