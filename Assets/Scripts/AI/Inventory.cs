using System.Collections.Generic;

/// <summary>
/// Инвентарь торговца. Хранит количество каждого товара.
/// </summary>
public class Inventory
{
    private readonly Dictionary<string, int> _items = new Dictionary<string, int>();

    public bool Has(string goodId) =>
        _items.TryGetValue(goodId, out var amount) && amount > 0;

    public int GetAmount(string goodId) =>
        _items.TryGetValue(goodId, out var amount) ? amount : 0;

    public void Add(string goodId, int amount)
    {
        if (!_items.ContainsKey(goodId)) _items[goodId] = 0;
        _items[goodId] += amount;
    }

    public bool Remove(string goodId, int amount)
    {
        if (!Has(goodId) || _items[goodId] < amount) return false;
        _items[goodId] -= amount;
        return true;
    }

    public IReadOnlyDictionary<string, int> GetAll() => _items;
}
