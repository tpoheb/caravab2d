using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Панель инвентаря игрока — оверлей поверх игры.
/// Открывается кнопкой в TopBar, закрывается той же кнопкой или крестиком.
///
/// Подключение:
///   1. Создай Panel в Canvas поверх остальных элементов
///   2. Добавь ScrollView внутрь для списка товаров
///   3. Повесь этот компонент на корневой объект панели
///   4. Назначь ссылки в инспекторе
///   5. Кнопку в TopBar подключи к методу Toggle()
/// </summary>
public class InventoryPanel : MonoBehaviour
{
    [Header("Зависимости")]
    [SerializeField] private PlayerInventory playerInventory;

    [Header("UI")]
    [SerializeField] private GameObject panel;
    [SerializeField] private Transform  itemsContainer;
    [SerializeField] private GameObject inventoryItemPrefab;
    [SerializeField] private Button     closeButton;
    [SerializeField] private TMP_Text   totalWeightText;
    [SerializeField] private TMP_Text   emptyLabel;

    private readonly List<GameObject> _rows = new List<GameObject>();

    // ------------------------------------------------------------------
    // Unity
    // ------------------------------------------------------------------

    private void Awake()
    {
        if (closeButton != null)
            closeButton.onClick.AddListener(Close);

        panel.SetActive(false);
    }

    // ------------------------------------------------------------------
    // Публичные методы — вызываются из кнопок TopBar
    // ------------------------------------------------------------------

    /// <summary>Переключить видимость панели.</summary>
    public void Toggle()
    {
        if (panel.activeSelf)
            Close();
        else
            Open();
    }

    public void Open()
    {
        Refresh();
        panel.SetActive(true);
    }

    public void Close()
    {
        panel.SetActive(false);
        ClearRows();
    }

    // ------------------------------------------------------------------
    // Обновление данных
    // ------------------------------------------------------------------

    private void Refresh()
    {
        ClearRows();

        if (playerInventory == null)
        {
            Debug.LogError("[InventoryPanel] PlayerInventory не назначен!");
            return;
        }

        var items = playerInventory.Items;

        bool isEmpty = items == null || items.Count == 0;
        if (emptyLabel != null) emptyLabel.gameObject.SetActive(isEmpty);
        if (isEmpty) return;

        foreach (var entry in items)
        {
            if (entry.item == null || entry.quantity <= 0) continue;

            var go  = Instantiate(inventoryItemPrefab, itemsContainer);
            var row = go.GetComponent<InventoryItemUI>();

            if (row != null)
                row.Setup(entry.item.itemName, entry.quantity, entry.averagePurchasePrice);

            _rows.Add(go);
        }

        // Итоговый вес
        if (totalWeightText != null)
        {
            int current = playerInventory.GetCurrentWeight();
            int max     = playerInventory.GetRemainingCapacity() + current;
            totalWeightText.text = $"Вес: {current}/{max}";
        }
    }

    private void ClearRows()
    {
        foreach (var row in _rows)
            if (row != null) Destroy(row);
        _rows.Clear();
    }
}