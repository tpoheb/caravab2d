using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class TradeItemSystem : MonoBehaviour
{
    public List<Item> items;
    public int playerMoney;

    public GameObject tradePanel;
    public Transform itemsContainer;
    public GameObject itemUIPrefab;

    private void Start()
    {
        if (tradePanel != null)
            tradePanel.SetActive(false);
    }

    public void EnterTrade()
    {
        if (tradePanel == null || itemsContainer == null || itemUIPrefab == null)
        {
            Debug.LogError("Trade references are not set!");
            return;
        }

        tradePanel.SetActive(true);
        UpdateUI();
        Debug.Log("Entered trade.");
    }

    public void ExitTrade()
    {
        if (tradePanel != null)
            tradePanel.SetActive(false);

        Debug.Log("Exited trade.");
    }

    private void UpdateUI()
    {
        if (itemsContainer == null || itemUIPrefab == null || items == null)
            return;

        // Clear existing items
        foreach (Transform child in itemsContainer)
        {
            Destroy(child.gameObject);
        }

        // Create new items
        foreach (Item item in items)
        {
            if (item == null) continue;

            GameObject itemUI = Instantiate(itemUIPrefab, itemsContainer);
            ItemUI itemUIComponent = itemUI.GetComponent<ItemUI>();
            if (itemUIComponent != null)
            {
                itemUIComponent.Initialize(item, this);
            }
        }
    }

    public void BuyItem(Item item, int quantity)
    {
        if (item == null || quantity <= 0)
        {
            Debug.LogWarning("Invalid item or quantity!");
            return;
        }

        try
        {
            int totalCost = checked(item.price * quantity);

            if (playerMoney >= totalCost && item.cityStock >= quantity)
            {
                playerMoney -= totalCost;
                item.cityStock -= quantity;
                item.playerStock += quantity;
                UpdateUI();
                Debug.Log($"Bought {quantity} {item.itemName}.");
            }
            else
            {
                Debug.Log($"Not enough {(playerMoney < totalCost ? "money" : "stock")}. " +
                         $"Need: {totalCost}, Have: {playerMoney}. " +
                         $"Stock: {item.cityStock}, Requested: {quantity}");
            }
        }
        catch (System.OverflowException)
        {
            Debug.LogError("Total cost exceeds maximum integer value!");
        }
    }

    public void SellItem(Item item, int quantity)
    {
        if (item == null || quantity <= 0)
        {
            Debug.LogWarning("Invalid item or quantity!");
            return;
        }

        if (item.playerStock >= quantity)
        {
            playerMoney += item.price * quantity;
            item.cityStock += quantity;
            item.playerStock -= quantity;
            UpdateUI();
            Debug.Log($"Sold {quantity} {item.itemName}.");
        }
        else
        {
            Debug.Log($"Not enough items to sell. Have: {item.playerStock}, Need: {quantity}");
        }
    }
}