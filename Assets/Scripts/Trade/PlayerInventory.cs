using System.Collections.Generic;
using System.Linq;
using UnityEngine;

[System.Serializable]
public class InventoryItem
{
    public Item item;
    public int quantity;
    public float averagePurchasePrice;
}

public class PlayerInventory : MonoBehaviour
{
    [Header("Settings")]
    [SerializeField] private int startMoney = 1000;
    [SerializeField] private PlayerStats playerStats;

    [Header("Контрабанда")]
    [Tooltip("Список Item-ассетов, которые считаются контрабандой (Осколки Прошлого и т.п.)")]
    [SerializeField] private List<Item> contrabandItems = new List<Item>();

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

    public bool AddItem(Item item, int quantity, int totalTransactionCost = 0)
    {
        if (!CanCarryItem(item, quantity))
        {
            Debug.LogWarning($"Can't carry {quantity} {item.name}. Not enough capacity.");
            return false;
        }

        var existing = items.Find(i => i.item == item);
        if (existing != null)
        {
            float currentTotalValue = existing.quantity * existing.averagePurchasePrice;
            existing.quantity += quantity;
            existing.averagePurchasePrice = (currentTotalValue + totalTransactionCost) / existing.quantity;
        }
        else
        {
            float initialAvgPrice = quantity > 0 ? (float)totalTransactionCost / quantity : 0;
            items.Add(new InventoryItem
            {
                item = item,
                quantity = quantity,
                averagePurchasePrice = initialAvgPrice
            });
        }

        OnInventoryChanged?.Invoke();
        return true;
    }

    public float GetItemAveragePrice(Item item)
    {
        var existing = items.Find(i => i.item == item);
        return existing != null ? existing.averagePurchasePrice : 0f;
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

    #region Event Card Methods

    /// <summary>
    /// Добавляет N единиц случайного существующего товара из инвентаря.
    /// Если инвентарь пуст — берёт первый Item из Resources/Items/.
    /// Вызывается эффектами AddGoods (Благословенный Оазис, Дар Незнакомца).
    /// </summary>
    public void AddRandomGoods(int amount)
    {
        Item target = null;

        if (items.Count > 0)
        {
            // Предпочитаем товар, которого уже есть больше всего (логично для каравана)
            target = items.OrderByDescending(i => i.quantity).First().item;
        }
        else
        {
            // Инвентарь пуст — загружаем первый попавшийся Item из ресурсов
            var all = Resources.LoadAll<Item>("Items");
            if (all.Length > 0)
                target = all[Random.Range(0, all.Length)];
        }

        if (target == null)
        {
            Debug.LogWarning("[PlayerInventory] AddRandomGoods: нет доступных товаров.");
            return;
        }

        // Добавляем без учёта средней цены (бесплатный товар — цена 0)
        AddItem(target, amount, totalTransactionCost: 0);
        Debug.Log($"[PlayerInventory] AddRandomGoods: +{amount} {target.name}.");
    }

    /// <summary>
    /// Удаляет N единиц случайных товаров из инвентаря.
    /// Выбирает случайные слоты до исчерпания лимита.
    /// Вызывается эффектом RemoveGoods (Обвал на Тропе).
    /// </summary>
    public void RemoveRandomGoods(int amount)
    {
        int remaining = amount;

        // Перемешиваем, чтобы удаление было честно случайным
        var shuffled = items.OrderBy(_ => Random.value).ToList();

        foreach (var slot in shuffled)
        {
            if (remaining <= 0) break;

            int toRemove = Mathf.Min(slot.quantity, remaining);
            RemoveItem(slot.item, toRemove);
            remaining -= toRemove;

            Debug.Log($"[PlayerInventory] RemoveRandomGoods: -{toRemove} {slot.item.name}.");
        }

        if (remaining > 0)
            Debug.Log($"[PlayerInventory] RemoveRandomGoods: не хватило товаров, удалено всё.");
    }

    /// <summary>
    /// Конфискует всю контрабанду (список contrabandItems в инспекторе).
    /// Возвращает true, если хоть что-то было изъято.
    /// Вызывается эффектом Confiscation (Тень Инквизитора).
    /// </summary>
    public bool ConfiscateContraband()
    {
        bool found = false;

        foreach (var contrabandItem in contrabandItems)
        {
            int stock = GetItemStock(contrabandItem);
            if (stock <= 0) continue;

            RemoveItem(contrabandItem, stock);
            found = true;
            Debug.Log($"[PlayerInventory] ConfiscateContraband: изъято {stock} {contrabandItem.name}.");
        }

        return found;
    }

    /// <summary>
    /// Удваивает количество конкретного товара в инвентаре.
    /// Вызывается эффектом DoubleGoods (Мистический Узел) после выбора игрока.
    /// </summary>
    public void DoubleGoods(Item item)
    {
        if (item == null) return;

        var slot = items.Find(i => i.item == item);
        if (slot == null || slot.quantity <= 0)
        {
            Debug.LogWarning($"[PlayerInventory] DoubleGoods: товара {item.name} нет в инвентаре.");
            return;
        }

        int addAmount = slot.quantity; // удваиваем = добавляем столько же
        AddItem(item, addAmount, totalTransactionCost: 0);
        Debug.Log($"[PlayerInventory] DoubleGoods: {item.name} удвоен ({addAmount} → {slot.quantity}).");
    }

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