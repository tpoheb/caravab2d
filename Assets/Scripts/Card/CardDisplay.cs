using System;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Визуальный компонент карты события.
/// Префаб не содержит рубашку и анимацию — только лицевую сторону.
/// Появление мгновенное через SetActive.
/// Располагается внутри EventPanelUI поверх Image-колоды.
/// </summary>
public class CardDisplay : MonoBehaviour
{
    [Header("Текстовые поля карты")]
    [SerializeField] private TextMeshProUGUI titleText;

    [Header("Характеристики — Shadow-карта")]
    [SerializeField] private GameObject shadowStatsRoot;
    [SerializeField] private TextMeshProUGUI shadowEffectTypeText;
    [SerializeField] private TextMeshProUGUI shadowValueText;

    [Header("Характеристики — Battle-карта")]
    [SerializeField] private GameObject battleStatsRoot;
    [SerializeField] private TextMeshProUGUI battleRequiredAttackText;
    [SerializeField] private TextMeshProUGUI battleRewardText;

    private ICard _currentCard;

    public event Action OnCardRevealed;

    // ── Публичный API ─────────────────────────────────────────────────────

    /// <summary>
    /// Заполнить карту данными и показать мгновенно.
    /// </summary>
    public void ShowCard(ICard data)
    {
        _currentCard = data;
        FillContent(data);
        gameObject.SetActive(true);
        OnCardRevealed?.Invoke();
    }

    /// <summary>
    /// Скрыть карту.
    /// </summary>
    public void HideCard()
    {
        gameObject.SetActive(false);
        _currentCard = null;
    }

    public ICard GetCurrentCard() => _currentCard;

    // ── Наполнение ────────────────────────────────────────────────────────

    private void FillContent(ICard data)
    {
        if (titleText != null)
            titleText.text = data.CardName;

        switch (data)
        {
            case ShadowCardData shadow:
                ShowShadowStats(shadow);
                break;
            case BattleCardData battle:
                ShowBattleStats(battle);
                break;
            default:
                HideAllStats();
                break;
        }
    }

    private void ShowShadowStats(ShadowCardData card)
    {
        if (shadowStatsRoot != null) shadowStatsRoot.SetActive(true);
        if (battleStatsRoot  != null) battleStatsRoot.SetActive(false);

        if (shadowEffectTypeText != null)
            shadowEffectTypeText.text = FormatEffectType(card.effectType);

        if (shadowValueText != null)
            shadowValueText.text = FormatValue(card.effectType, card.value);
    }

    private void ShowBattleStats(BattleCardData card)
    {
        if (shadowStatsRoot != null) shadowStatsRoot.SetActive(false);
        if (battleStatsRoot  != null) battleStatsRoot.SetActive(true);

        if (battleRequiredAttackText != null)
            battleRequiredAttackText.text = $"Атака: {card.requiredAttack}";

        if (battleRewardText != null)
            battleRewardText.text = $"+{card.rewardMoney} / {card.penaltyMoney}";
    }

    private void HideAllStats()
    {
        if (shadowStatsRoot != null) shadowStatsRoot.SetActive(false);
        if (battleStatsRoot  != null) battleStatsRoot.SetActive(false);
    }

    // ── Форматирование ────────────────────────────────────────────────────

    private static string FormatEffectType(ShadowEffectType type) => type switch
    {
        ShadowEffectType.Money          => "Деньги",
        ShadowEffectType.Attack         => "Атака",
        ShadowEffectType.Capacity       => "Грузоподъёмность",
        ShadowEffectType.Bargain        => "Торговля",
        ShadowEffectType.AddGoods       => "Добавить товар",
        ShadowEffectType.RemoveGoods    => "Потеря товара",
        ShadowEffectType.FireCrewMember => "Команда",
        ShadowEffectType.WagePenalty    => "Жалованье",
        ShadowEffectType.Confiscation   => "Конфискация",
        ShadowEffectType.TeamStats      => "Характеристики команды",
        ShadowEffectType.BonusTrade     => "Цены товаров",
        _                               => type.ToString()
    };

    private static string FormatValue(ShadowEffectType type, int value)
    {
        string prefix = value >= 0 ? "<color=green>+" : "<color=red>";
        const string suffix = "</color>";

        return type switch
        {
            ShadowEffectType.Money or
            ShadowEffectType.BonusTrade or
            ShadowEffectType.Capacity or
            ShadowEffectType.TeamStats      => $"{prefix}{value}%{suffix}",

            ShadowEffectType.AddGoods or
            ShadowEffectType.RemoveGoods    => value == 1 ? "Частично" : "Полностью",

            ShadowEffectType.FireCrewMember => "1 человек",
            ShadowEffectType.Confiscation   => $"Штраф: {value}",
            _                               => $"{prefix}{value}{suffix}"
        };
    }
}