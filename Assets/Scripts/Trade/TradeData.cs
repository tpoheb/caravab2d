using System.Collections.Generic;
using UnityEngine; // Добавлено для Mathf

[System.Serializable]
public class TradeData
{
    public List<City> cities = new List<City>();
    public List<Product> products = new List<Product>();

    [System.Serializable]
    public class City
    {
        public string name;
        public List<Price> prices = new List<Price>();
    }

    [System.Serializable]
    public class Product
    {
        public string id;
        public string name;
    }

    [System.Serializable]
    public class Price
    {
        public string productId;
        
        // --- Динамическая экономика ---
        public float basePrice;      // Идеальная/базовая цена товара
        public float currentPrice;   // Текущая цена на рынке
        public float minPrice;       // Нижний порог падения цены
        public float maxPrice;       // Верхний предел роста цены
        
        // --- Настройки поведения ---
        public float volatility;     // Шаг изменения (например, 0.02f)
        public float regenRate;      // Скорость восстановления за ход (например, 0.1f)

        // --- Обратная совместимость ---
        // Так как спреда пока нет, покупка и продажа возвращают округленную текущую цену.
        // Сериализаторы (вроде JsonUtility) обычно игнорируют свойства { get; }, 
        // поэтому в файлы сохраняться будут только основные поля выше.
        public int buyPrice => Mathf.RoundToInt(currentPrice);
        public int sellPrice => Mathf.RoundToInt(currentPrice);
    }
}