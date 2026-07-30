using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System;

/// <summary>
/// Вешается на префаб карточки юнита.
/// Вся карточка целиком — кликабельна через Button на root-объекте.
///
/// Ожидаемая иерархия префаба:
///   [Root] — Button (UnitCardUI)
///   ├─ NameText          — TextMeshProUGUI
///   ├─ HireCostText      — TextMeshProUGUI
///   └─ StatsText         — TextMeshProUGUI
/// </summary>
public class UnitCardUI : MonoBehaviour
{
    [Header("Карточка — текстовые поля")]
    [SerializeField] private TextMeshProUGUI nameText;
    [SerializeField] private TextMeshProUGUI hireCostText;
    [SerializeField] private TextMeshProUGUI statsText;

    [Header("Кнопка — вся карточка")]
    [SerializeField] private Button cardButton;

    // ─────────────────────────────────────────────────────────────────────

    /// <summary>
    /// Инициализирует карточку.
    /// </summary>
    /// <param name="data">ScriptableObject юнита.</param>
    /// <param name="onAction">Callback по клику на карточку (Нанять / Уволить).</param>
    /// <param name="interactable">false — если специальность уже занята.</param>
    /// <param name="showCost">false для карточек в разделе «Команда» (уже нанят).</param>
    public void Setup(UnitData data, Action onAction, bool interactable = true, bool showCost = true)
    {
        if (data == null)
        {
            Debug.LogError("[UnitCardUI] UnitData == null!", this);
            return;
        }

        if (nameText != null)
            nameText.text = $"{data.unitName}\n<size=70%>{FormatSpecialty(data.specialty)}</size>";

        if (hireCostText != null)
        {
            hireCostText.gameObject.SetActive(showCost);
            hireCostText.text = $"Цена найма: {data.hireCost} ₽";
        }

        if (statsText != null)
            statsText.text = FormatStats(data);

        if (cardButton != null)
        {
            cardButton.interactable = interactable;
            cardButton.onClick.RemoveAllListeners();
            if (interactable && onAction != null)
                cardButton.onClick.AddListener(() => onAction());
        }
    }

    // ─────────────────────────────────────────────────────────────────────

    private static string FormatStats(UnitData data)
    {
        var parts = new System.Collections.Generic.List<string>();

        if (data.attackBonus   != 0) parts.Add($"⚔ Атака {FormatBonus(data.attackBonus)}");
        if (data.bargainBonus  != 0) parts.Add($"💰 Торговля {FormatBonus(data.bargainBonus)}");
        if (data.capacityBonus != 0) parts.Add($"📦 Груз {FormatBonus(data.capacityBonus)}");

        return parts.Count > 0
            ? string.Join("\n", parts)
            : "Без бонусов";
    }

    private static string FormatBonus(int value) =>
        value >= 0 ? $"+{value}" : $"{value}";

    private static string FormatSpecialty(UnitSpecialty specialty) =>
        specialty switch
        {
            UnitSpecialty.Warrior  => "Воин",
            UnitSpecialty.Merchant => "Меняла",
            UnitSpecialty.Caravan  => "Караванщик",
            _                      => specialty.ToString()
        };
}