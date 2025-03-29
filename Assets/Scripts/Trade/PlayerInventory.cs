using System.Collections.Generic;
using UnityEngine;

public class PlayerInventory : MonoBehaviour
{
    public int money = 1000; // Начальные деньги игрока
    public List<InventoryItem> items = new List<InventoryItem>();

    [System.Serializable]
    public class InventoryItem
    {
        public Item item;
        public int quantity;
    }

    // Добавляет предмет в инвентарь
    public void AddItem(Item item, int quantity)
    {
        if (item == null || quantity <= 0) return;

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

    // Удаляет предмет из инвентаря
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

    // Возвращает количество указанного предмета
    public int GetItemStock(Item item)
    {
        if (item == null) return 0;

        var existingItem = items.Find(i => i.item == item);
        return existingItem != null ? existingItem.quantity : 0;
    }

    // Сохраняет инвентарь игрока
    public void SaveInventory()
    {
        PlayerPrefs.SetInt("PlayerMoney", money);

        // Сохраняем количество предметов
        PlayerPrefs.SetInt("InventoryCount", items.Count);

        // Сохраняем каждый предмет
        for (int i = 0; i < items.Count; i++)
        {
            PlayerPrefs.SetString($"InventoryItem_{i}", items[i].item.name);
            PlayerPrefs.SetInt($"InventoryQuantity_{i}", items[i].quantity);
        }

        PlayerPrefs.Save();
    }

    // Загружает инвентарь игрока
    public void LoadInventory()
    {
        money = PlayerPrefs.GetInt("PlayerMoney", 1000); // Значение по умолчанию

        int itemCount = PlayerPrefs.GetInt("InventoryCount", 0);
        items.Clear();

        // Загружаем каждый предмет
        for (int i = 0; i < itemCount; i++)
        {
            string itemName = PlayerPrefs.GetString($"InventoryItem_{i}", "");
            int quantity = PlayerPrefs.GetInt($"InventoryQuantity_{i}", 0);

            // Предполагаем, что у вас есть система загрузки предметов по имени
            Item item = Resources.Load<Item>($"Items/{itemName}");
            if (item != null)
            {
                items.Add(new InventoryItem { item = item, quantity = quantity });
            }
        }
    }
}