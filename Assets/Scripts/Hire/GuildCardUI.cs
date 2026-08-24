using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System;

/// <summary>
/// Вешается на префаб карточки гильдии.
/// Вся карточка целиком — кликабельна через Button на root-объекте.
///
/// Ожидаемая иерархия префаба:
///   [Root] — Button (GuildCardUI)
///   ├─ NameText         — TextMeshProUGUI
///   ├─ DescriptionText  — TextMeshProUGUI
///   ├─ EntryFeeText     — TextMeshProUGUI
///   └─ IconImage        — Image (опционально)
/// </summary>
public class GuildCardUI : MonoBehaviour
{
    [Header("Карточка — текстовые поля")]
    [SerializeField] private TextMeshProUGUI nameText;
    [SerializeField] private TextMeshProUGUI descriptionText;
    [SerializeField] private TextMeshProUGUI entryFeeText;
    [SerializeField] private Image iconImage;

    [Header("Кнопка — вся карточка")]
    [SerializeField] private Button cardButton;

    private GuildData _data;

    /// <summary>
    /// Инициализирует карточку гильдии.
    /// </summary>
    /// <param name="data">ScriptableObject гильдии.</param>
    /// <param name="onAction">Callback по клику на карточку (Вступить / Сменить).</param>
    /// <param name="interactable">false — если уже в этой гильдии.</param>
    /// <param name="isCurrentGuild">true — выделить как текущую гильдию игрока.</param>
    public void Setup(GuildData data, Action onAction, bool interactable = true, bool isCurrentGuild = false)
    {
        if (data == null)
        {
            Debug.LogError("[GuildCardUI] GuildData == null!", this);
            return;
        }

        _data = data;

        if (nameText != null)
        {
            string prefix = isCurrentGuild ? "★ " : "";
            nameText.text = $"{prefix}{data.guildName}";
        }

        if (descriptionText != null)
            descriptionText.text = data.description ?? "";

        if (entryFeeText != null)
            entryFeeText.text = $"Взнос: {data.entryFee} ₽";

        if (iconImage != null)
        {
            if (data.icon != null)
            {
                iconImage.sprite = data.icon;
                iconImage.gameObject.SetActive(true);
            }
            else
            {
                iconImage.gameObject.SetActive(false);
            }
        }

        if (cardButton != null)
        {
            cardButton.interactable = interactable;
            cardButton.onClick.RemoveAllListeners();
            if (interactable && onAction != null)
                cardButton.onClick.AddListener(() => onAction());
        }
    }

    /// <summary>
    /// Возвращает GuildData, связанную с этой карточкой.
    /// </summary>
    public GuildData GetGuildData() => _data;
}