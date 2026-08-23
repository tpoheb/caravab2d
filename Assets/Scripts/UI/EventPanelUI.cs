using UnityEngine;
using TMPro;

/// <summary>
/// Панель событий — текстовые поля для карт событий и боёв.
///
/// Структура полей:
///   ┌─────────────────────────────────────────────────────┐
///   │ titleText       — название карты / имя врага        │
///   │ descriptionText — описание карты                    │
///   │ effectTypeText  — тип эффекта | атака игрока+кубик  │
///   │ valueText       — значение | "Сил достаточно/нет"   │
///   │ durationText    — длительность | награда/штраф      │
///   │ resultText      — итог эффекта | победа/поражение   │
///   └─────────────────────────────────────────────────────┘
///
/// Источники вызовов:
///   CardDeckUI      → DisplayShadowCard / DisplayBattleCard
///   BattleUIManager → DisplayDiceRoll / DisplayBattleResult / DisplayEscapeMessage
/// </summary>
public class EventPanelUI : MonoBehaviour
{
    // ── Инспектор ─────────────────────────────────────────────────────────

    [Header("Панель")]
    [SerializeField] private GameObject panelRoot;

    [Header("Текстовые поля — общие")]
    [Tooltip("Название Shadow-карты или имя врага Battle-карты.")]
    [SerializeField] private TextMeshProUGUI titleText;

    [Tooltip("Описание карты (из ICard.Description).")]
    [SerializeField] private TextMeshProUGUI descriptionText;

    [Header("Текстовые поля — детали эффекта")]
    [Tooltip("Shadow: тип эффекта (Деньги, Атака…). Battle: метка БИТВА.")]
    [SerializeField] private TextMeshProUGUI effectTypeText;

    [Tooltip("Shadow: значение эффекта (+20%, 1 человек…). Battle: требуемая атака.")]
    [SerializeField] private TextMeshProUGUI valueText;

    [Tooltip("Shadow: длительность / мгновенный. Battle: награда и штраф.")]
    [SerializeField] private TextMeshProUGUI durationText;

    [Header("Текстовое поле — итог")]
    [Tooltip("Shadow: результат применения эффекта. Battle: победа/поражение с суммой.")]
    [SerializeField] private TextMeshProUGUI resultText;

    // ── Unity ─────────────────────────────────────────────────────────────

    private void Start()
    {
        // Видимость панели управляется снаружи (GameManager / CityManager):
        //   — в городе:  HidePanel()
        //   — на дороге: ShowPanel()
        // Не трогаем SetActive здесь.
        ValidateReferences();
    }

    private void ValidateReferences()
    {
        if (panelRoot       == null) Debug.LogError($"[EventPanelUI] {nameof(panelRoot)} не назначен!");
        if (titleText       == null) Debug.LogWarning($"[EventPanelUI] {nameof(titleText)} не назначен!");
        if (descriptionText == null) Debug.LogWarning($"[EventPanelUI] {nameof(descriptionText)} не назначен!");
        if (effectTypeText  == null) Debug.LogWarning($"[EventPanelUI] {nameof(effectTypeText)} не назначен!");
        if (valueText       == null) Debug.LogWarning($"[EventPanelUI] {nameof(valueText)} не назначен!");
        if (durationText    == null) Debug.LogWarning($"[EventPanelUI] {nameof(durationText)} не назначен!");
        if (resultText      == null) Debug.LogWarning($"[EventPanelUI] {nameof(resultText)} не назначен!");
    }

    // ── Публичный API — отображение карт ─────────────────────────────────

    /// <summary>
    /// Показать детали Shadow-карты сразу после флипа CardDisplay.
    /// resultText остаётся пустым — заполнится через DisplayResult().
    /// </summary>
    public void DisplayShadowCard(ShadowCardData card)
    {
        Show();

        // Название
        SetText(titleText, card.cardName);

        // Описание
        SetText(descriptionText, card.description);

        // Тип эффекта
        SetText(effectTypeText, FormatEffectType(card.effectType));

        // Значение эффекта
        SetText(valueText, FormatValue(card.effectType, card.value));

        // Длительность
        SetText(durationText, card.isTemporary
            ? $"Длительность: {card.duration} ходов"
            : "Мгновенный эффект");

        // Итог — заполнится после применения эффекта
        SetText(resultText, "");
    }

    /// <summary>
    /// Показать детали Battle-карты сразу после флипа CardDisplay.
    /// resultText остаётся пустым — заполнится через DisplayBattleResult().
    /// </summary>
    public void DisplayBattleCard(BattleCardData card)
    {
        Show();

        // Название
        SetText(titleText, card.enemyName);

        // Описание
        SetText(descriptionText, card.description);

        // Тип — фиксированная метка
        SetText(effectTypeText, "<color=red>БИТВА</color>");

        // Требуемая атака
        SetText(valueText, $"Требуется атака: {card.requiredAttack}");

        // Награда и штраф
        SetText(durationText,
            $"<color=green>Победа: +{card.rewardMoney} фелсов</color>  " +
            $"<color=red>Поражение: {card.penaltyMoney} фелсов</color>");

        // Итог — заполнится после броска кубика
        SetText(resultText, "");
    }

    // ── Публичный API — результаты ────────────────────────────────────────

    /// <summary>
    /// Результат применения Shadow-эффекта или произвольное сообщение.
    /// </summary>
    public void DisplayResult(string message, bool isPositive)
    {
        if (resultText == null) return;
        string color = isPositive ? "green" : "red";
        resultText.text = $"<color={color}>{message}</color>";
    }

    /// <summary>
    /// Итог боя: победа или поражение с итоговыми числами.
    /// Вызывается из BattleUIManager после броска кубика.
    /// </summary>
    public void DisplayBattleResult(bool victory, int rewardOrPenalty)
    {
        if (resultText == null) return;

        resultText.text = victory
            ? $"<color=green><b>ПОБЕДА!</b></color>  +{rewardOrPenalty} фелсов"
            : $"<color=red><b>ПОРАЖЕНИЕ!</b></color>  {rewardOrPenalty} фелсов";
    }

    /// <summary>
    /// Результат броска кубика — атака игрока vs врага.
    /// Вызывается из BattleUIManager вместо rewardText/effectText.
    /// effectTypeText ← строка атак, valueText ← оценка шансов.
    /// </summary>
    public void DisplayDiceRoll(int diceResult, int baseAttack, int enemyAttack)
    {
        int totalAttack = baseAttack + diceResult;
        bool wouldWin   = totalAttack >= enemyAttack;

        SetText(effectTypeText,
            $"Ваша атака ({baseAttack}) + кубик ({diceResult}) (<size=150%>{totalAttack}</size>)\n" +
            $"Атака врага: ({enemyAttack})");

        SetText(valueText, wouldWin
            ? "<color=green>Сил достаточно!</color>"
            : "<color=red>Сил не хватает!</color>");
    }

    /// <summary>
    /// Сообщение о побеге через Дымовую Завесу.
    /// Вызывается из BattleUIManager вместо rewardText/effectText/resultText.
    /// </summary>
    public void DisplayEscapeMessage(string enemyName)
    {
        SetText(effectTypeText, $"Встреча с <b>{enemyName}</b>");
        SetText(valueText,      "<color=yellow>Дымовая завеса!</color>");
        SetText(resultText,     "Вы скрылись без потерь.");
    }

    /// <summary>
    /// Показать панель (вызывается при выходе из города).
    /// Поля пустые — заполнятся когда придёт карта.
    /// </summary>
    public void ShowPanel()
    {
        if (panelRoot != null) panelRoot.SetActive(true);
    }

    /// <summary>
    /// Скрыть панель и очистить все поля (вызывается при входе в город).
    /// </summary>
    public void HidePanel()
    {
        if (panelRoot != null) panelRoot.SetActive(false);
        ClearAll();
    }

    /// <summary>
    /// Очистить только поле результата (например, перед новым броском).
    /// </summary>
    public void ClearResult()
    {
        SetText(resultText, "");
    }

    /// <summary>
    /// Полная очистка всех полей без скрытия панели.
    /// </summary>
    public void ClearAll()
    {
        SetText(titleText,       "");
        SetText(descriptionText, "");
        SetText(effectTypeText,  "");
        SetText(valueText,       "");
        SetText(durationText,    "");
        SetText(resultText,      "");
    }

    // ── Вспомогательные методы ────────────────────────────────────────────

    private void Show()
    {
        if (panelRoot != null) panelRoot.SetActive(true);
    }

    private static void SetText(TextMeshProUGUI field, string value)
    {
        if (field != null) field.text = value;
    }

    private static string FormatEffectType(ShadowEffectType type) => type switch
    {
        ShadowEffectType.Money            => "Деньги",
        ShadowEffectType.Attack           => "Атака",
        ShadowEffectType.Capacity         => "Грузоподъёмность",
        ShadowEffectType.Bargain          => "Торговля",
        ShadowEffectType.AddGoods         => "Добавить товар",
        ShadowEffectType.RemoveGoods      => "Потеря товара",
        ShadowEffectType.FireCrewMember   => "Команда",
        ShadowEffectType.WagePenalty      => "Жалованье",
        ShadowEffectType.Confiscation     => "Конфискация",
        ShadowEffectType.TeamStats        => "Характеристики команды",
        ShadowEffectType.BonusTrade       => "Цены товаров",
        _                                 => type.ToString()
    };

    private static string FormatValue(ShadowEffectType type, int value)
    {
        string prefix = value >= 0 ? "<color=green>+" : "<color=red>";
        string suffix = "</color>";

        return type switch
        {
            ShadowEffectType.Money or
            ShadowEffectType.BonusTrade or
            ShadowEffectType.Capacity or
            ShadowEffectType.TeamStats    => $"{prefix}{value}%{suffix}",

            ShadowEffectType.AddGoods or
            ShadowEffectType.RemoveGoods  => value == 1 ? "Частично" : "Полностью",

            ShadowEffectType.FireCrewMember => "1 человек",
            ShadowEffectType.Confiscation   => $"Штраф: {value}",
            _                               => $"{prefix}{value}{suffix}"
        };
    }
}