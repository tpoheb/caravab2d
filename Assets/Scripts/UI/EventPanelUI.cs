using UnityEngine;
using TMPro;

/// <summary>
/// Панель событий — отображает детали розыгранной карты.
/// Располагается на Canvas отдельно от префаба карты.
/// </summary>
public class EventPanelUI : MonoBehaviour
{
    [Header("Текстовые поля")]
    [SerializeField] private TextMeshProUGUI titleText;
    [SerializeField] private TextMeshProUGUI descriptionText;
    [SerializeField] private TextMeshProUGUI effectTypeText;
    [SerializeField] private TextMeshProUGUI valueText;
    [SerializeField] private TextMeshProUGUI durationText;
    [SerializeField] private TextMeshProUGUI resultText;

    [Header("Панель")]
    [SerializeField] private GameObject panelRoot;

    private void Start()
    {
        if (panelRoot != null) panelRoot.SetActive(false);
        ValidateReferences();
    }

    private void ValidateReferences()
    {
        if (panelRoot == null) Debug.LogError($"{nameof(panelRoot)} не назначен!");
        if (titleText == null) Debug.LogWarning($"{nameof(titleText)} не назначен!");
        if (descriptionText == null) Debug.LogWarning($"{nameof(descriptionText)} не назначен!");
        if (effectTypeText == null) Debug.LogWarning($"{nameof(effectTypeText)} не назначен!");
        if (valueText == null) Debug.LogWarning($"{nameof(valueText)} не назначен!");
        if (durationText == null) Debug.LogWarning($"{nameof(durationText)} не назначен!");
        if (resultText == null) Debug.LogWarning($"{nameof(resultText)} не назначен!");
    }

    /// <summary>
    /// Показать детали Shadow-карты.
    /// </summary>
    public void DisplayShadowCard(ShadowCardData card)
    {
        if (panelRoot != null) panelRoot.SetActive(true);

        if (titleText != null)
            titleText.text = card.cardName;

        if (descriptionText != null)
            descriptionText.text = card.description;

        if (effectTypeText != null)
            effectTypeText.text = FormatEffectType(card.effectType);

        if (valueText != null)
            valueText.text = FormatValue(card.effectType, card.value);

        if (durationText != null)
            durationText.text = card.isTemporary 
                ? $"Длительность: {card.duration} ходов" 
                : "Мгновенный эффект";

        if (resultText != null)
            resultText.text = ""; // Заполняется после применения эффекта
    }

    /// <summary>
    /// Показать детали Battle-карты.
    /// </summary>
    public void DisplayBattleCard(BattleCardData card)
    {
        if (panelRoot != null) panelRoot.SetActive(true);

        if (titleText != null)
            titleText.text = card.enemyName;

        if (descriptionText != null)
            descriptionText.text = card.description;

        if (effectTypeText != null)
            effectTypeText.text = "<color=red>БИТВА</color>";

        if (valueText != null)
            valueText.text = $"Требуется атака: {card.requiredAttack}";

        if (durationText != null)
            durationText.text = "";

        if (resultText != null)
            resultText.text = $"Победа: +{card.rewardMoney} фелсов | Поражение: {card.penaltyMoney} фелсов";
    }

    /// <summary>
    /// Показать результат применения эффекта.
    /// </summary>
    public void DisplayResult(string result, bool isPositive)
    {
        if (resultText == null) return;

        string color = isPositive ? "green" : "red";
        resultText.text = $"<color={color}>{result}</color>";
    }

    /// <summary>
    /// Скрыть панель.
    /// </summary>
    public void HidePanel()
    {
        if (panelRoot != null) panelRoot.SetActive(false);
        ClearAll();
    }

    public void ClearAll()
    {
        if (titleText != null) titleText.text = "";
        if (descriptionText != null) descriptionText.text = "";
        if (effectTypeText != null) effectTypeText.text = "";
        if (valueText != null) valueText.text = "";
        if (durationText != null) durationText.text = "";
        if (resultText != null) resultText.text = "";
    }

    // ── Helpers ──────────────────────────────────────────────────────────

    private string FormatEffectType(ShadowEffectType type)
    {
        return type switch
        {
            ShadowEffectType.Money => "Деньги",
            ShadowEffectType.Attack => "Атака",
            ShadowEffectType.Capacity => "Грузоподъёмность",
            ShadowEffectType.Bargain => "Торговля",
            ShadowEffectType.AddGoods => "Добавить товар",
            ShadowEffectType.RemoveGoods => "Потеря товара",
            ShadowEffectType.FireCrewMember => "Команда",
            ShadowEffectType.WagePenalty => "Жалованье",
            ShadowEffectType.Confiscation => "Конфискация",
            ShadowEffectType.TeamStats => "Характеристики команды",
            ShadowEffectType.BonusTrade => "Цены товаров",
            _ => type.ToString()
        };
    }

    private string FormatValue(ShadowEffectType type, int value)
    {
        string prefix = value >= 0 ? "<color=green>+" : "<color=red>";
        string suffix = "</color>";

        return type switch
        {
            ShadowEffectType.Money or
            ShadowEffectType.BonusTrade or
            ShadowEffectType.Capacity or
            ShadowEffectType.TeamStats => $"{prefix}{value}%{suffix}",

            ShadowEffectType.AddGoods or
            ShadowEffectType.RemoveGoods => value == 1 ? "Частично" : "Полностью",

            ShadowEffectType.FireCrewMember => "1 человек",
            ShadowEffectType.Confiscation => $"Штраф: {value}",
            _ => $"{prefix}{value}{suffix}"
        };
    }
}