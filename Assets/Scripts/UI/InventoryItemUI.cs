using UnityEngine;
using TMPro;

/// <summary>
/// Одна строка в панели инвентаря.
/// Показывает: название товара, количество, среднюю цену покупки.
/// </summary>
public class InventoryItemUI : MonoBehaviour
{
    [SerializeField] private TMP_Text itemNameText;
    [SerializeField] private TMP_Text quantityText;
    [SerializeField] private TMP_Text avgPriceText;

    public void Setup(string name, int quantity, float avgPrice)
    {
        if (itemNameText != null) itemNameText.text = name;
        if (quantityText != null) quantityText.text = quantity.ToString();
        if (avgPriceText != null)
            avgPriceText.text = avgPrice > 0 ? avgPrice.ToString("F1") : "—";
    }
}