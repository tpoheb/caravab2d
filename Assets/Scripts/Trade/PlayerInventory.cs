using System.Collections.Generic;
using UnityEngine;

public class PlayerInventory : MonoBehaviour
{
    public int money = 1000;
    public int maxCapacity = 100; // ������������ ����������������
    public List<InventoryItem> items = new List<InventoryItem>();

    [System.Serializable]
    public class InventoryItem
    {
        public Item item;
        public int quantity;
    }

    public bool CanCarryMore(int weightToAdd)
    {
        return GetCurrentWeight() + weightToAdd <= maxCapacity;
    }
    // ��������� ������� � ��������� (� ��������� ����������������)
    public void AddItem(Item item, int quantity)
    {
        if (item == null || quantity <= 0) return;

        // ���������, �� �������� �� ��� ����������������
        if (GetCurrentWeight() + (item.weight * quantity) > maxCapacity)
        {
            Debug.LogWarning("�� ������� ����������������!");
            return;
        }

        var existingItem = items.Find(i => i.item == item);
        if (existingItem != null)
        {
            existingItem.quantity += quantity;
        }
        else
        {
            items.Add(new InventoryItem { item = item, quantity = quantity });
        }
    }

    // ������� ������� �� ���������
    public void RemoveItem(Item item, int quantity)
    {
        if (item == null || quantity <= 0) return;

        var existingItem = items.Find(i => i.item == item);
        if (existingItem != null)
        {
            existingItem.quantity -= quantity;
            if (existingItem.quantity <= 0)
            {
                items.Remove(existingItem);
            }
        }
    }

    // ���������� ���������� ���������� ��������
    public int GetItemStock(Item item)
    {
        if (item == null) return 0;

        var existingItem = items.Find(i => i.item == item);
        return existingItem != null ? existingItem.quantity : 0;
    }

    // ����� �����: �������� ����������� ����� �������
    public bool CanCarryItem(Item item, int quantity)
    {
        if (item == null) return false;
        return GetCurrentWeight() + (item.weight * quantity) <= maxCapacity;
    }

    // ����� �����: ��������� �������� ����
    public int GetCurrentWeight()
    {
        int totalWeight = 0;
        foreach (var inventoryItem in items)
        {
            totalWeight += inventoryItem.item.weight * inventoryItem.quantity;
        }
        return totalWeight;
    }

    // ����� �����: ��������� ���������� ����������������
    public int GetRemainingCapacity()
    {
        return maxCapacity - GetCurrentWeight();
    }

    // ��������� ��������� ������ (�������� ���)
    public void SaveInventory()
    {
        PlayerPrefs.SetInt("PlayerMoney", money);
        PlayerPrefs.SetInt("PlayerMaxCapacity", maxCapacity);

        PlayerPrefs.SetInt("InventoryCount", items.Count);
        for (int i = 0; i < items.Count; i++)
        {
            PlayerPrefs.SetString($"InventoryItem_{i}", items[i].item.name);
            PlayerPrefs.SetInt($"InventoryQuantity_{i}", items[i].quantity);
        }
        PlayerPrefs.Save();
    }

    // ��������� ��������� ������ (�������� ���)
    public void LoadInventory()
    {
        money = PlayerPrefs.GetInt("PlayerMoney", 1000);
        maxCapacity = PlayerPrefs.GetInt("PlayerMaxCapacity", 100);

        int itemCount = PlayerPrefs.GetInt("InventoryCount", 0);
        items.Clear();

        for (int i = 0; i < itemCount; i++)
        {
            string itemName = PlayerPrefs.GetString($"InventoryItem_{i}", "");
            int quantity = PlayerPrefs.GetInt($"InventoryQuantity_{i}", 0);

            Item item = Resources.Load<Item>($"Items/{itemName}");
            if (item != null)
            {
                items.Add(new InventoryItem { item = item, quantity = quantity });
            }
        }
    }
}