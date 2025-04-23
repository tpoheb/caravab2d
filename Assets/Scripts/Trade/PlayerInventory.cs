using System.Collections.Generic;
using System.Linq;
using UnityEngine;

[System.Serializable]
public class InventoryItem
{
    public Item item;
    public int quantity;
}

public class PlayerInventory : MonoBehaviour
{
    [Header("Settings")]
    [SerializeField] private int startMoney = 1000;
    [SerializeField] private PlayerStats playerStats;

    [Header("Debug")]
    [SerializeField] private List<InventoryItem> items = new List<InventoryItem>();

    public int Money { get; set; }
    public IReadOnlyList<InventoryItem> Items => items.AsReadOnly();

    public event System.Action OnInventoryChanged;
    public event System.Action OnMoneyChanged;

    private void Awake()
    {
        ValidateReferences();
        Money = startMoney;
    }

    private void ValidateReferences()
    {
        if (playerStats == null)
            playerStats = GetComponent<PlayerStats>();
    }

    #region Inventory Operations
    public bool AddItem(Item item, int quantity)
    {
        if (!CanCarryItem(item, quantity))
        {
            Debug.LogWarning($"Can't carry {quantity} {item.name}. Not enough capacity.");
            return false;
        }

        var existing = items.Find(i => i.item == item);
        if (existing != null)
        {
            existing.quantity += quantity;
        }
        else
        {
            items.Add(new InventoryItem { item = item, quantity = quantity });
        }

        OnInventoryChanged?.Invoke();
        return true;
    }

    public bool RemoveItem(Item item, int quantity)
    {
        var existing = items.Find(i => i.item == item);
        if (existing == null || existing.quantity < quantity)
            return false;

        existing.quantity -= quantity;

        if (existing.quantity <= 0)
            items.Remove(existing);

        OnInventoryChanged?.Invoke();
        return true;
    }

    public int GetItemStock(Item item)
    {
        if (item == null) return 0;
        return items.FirstOrDefault(i => i.item == item)?.quantity ?? 0;
    }

    public bool HasItem(Item item, int minQuantity = 1) =>
        GetItemStock(item) >= minQuantity;
    #endregion

    #region Money Operations
    public bool TrySpendMoney(int amount)
    {
        if (Money < amount)
            return false;

        Money -= amount;
        OnMoneyChanged?.Invoke();
        return true;
    }

    public void AddMoney(int amount)
    {
        Money += amount;
        OnMoneyChanged?.Invoke();
    }
    #endregion

    #region Capacity Calculations
    public bool CanCarryItem(Item item, int quantity) =>
        item != null && CanCarryMore(item.weight * quantity);

    public bool CanCarryMore(int weightToAdd) =>
        GetCurrentWeight() + weightToAdd <= playerStats.Capacity;

    public int GetCurrentWeight()
    {
        int total = 0;
        foreach (var item in items)
            total += item.item.weight * item.quantity;
        return total;
    }

    public int GetRemainingCapacity() =>
        playerStats.Capacity - GetCurrentWeight();
    #endregion

    #region Persistence
    public void SaveInventory()
    {
        PlayerPrefs.SetInt("PlayerMoney", Money);
        PlayerPrefs.SetInt("InventoryCount", items.Count);

        for (int i = 0; i < items.Count; i++)
        {
            PlayerPrefs.SetString($"InventoryItem_{i}", items[i].item.name);
            PlayerPrefs.SetInt($"InventoryQuantity_{i}", items[i].quantity);
        }
        PlayerPrefs.Save();
    }

    public void LoadInventory()
    {
        Money = PlayerPrefs.GetInt("PlayerMoney", startMoney);
        items.Clear();

        int count = PlayerPrefs.GetInt("InventoryCount", 0);
        for (int i = 0; i < count; i++)
        {
            string name = PlayerPrefs.GetString($"InventoryItem_{i}", "");
            int quantity = PlayerPrefs.GetInt($"InventoryQuantity_{i}", 0);

            if (Resources.Load<Item>($"Items/{name}") is Item item)
                items.Add(new InventoryItem { item = item, quantity = quantity });
        }
    }
    #endregion
}