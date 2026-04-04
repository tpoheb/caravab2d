using UnityEngine;
using TMPro;
using UnityEngine.UI;

public class BattleUIManager : MonoBehaviour
{
    [Header("Панель событий")]
    [SerializeField] private TMP_Text eventDescriptionText;
    [SerializeField] private Button endTurnButton;
    [SerializeField] private Button diceButton; // Общая кнопка броска кубика

    [Header("UI Элементы Битвы")]
    [SerializeField] private GameObject battlePanel;
    [SerializeField] private GameObject cardPanel; // GameObject "Card" из иерархии
    [SerializeField] private TMP_Text enemyNameText;
    [SerializeField] private TMP_Text requiredAttackText;
    [SerializeField] private TMP_Text effectText;

    [Header("Элементы Кубика")]
    [SerializeField] private TMP_Text diceResultText;

    private void Start()
    {
        battlePanel.SetActive(true);
        ValidateReferences();
        SetIdleState();
    }

    private void ValidateReferences()
    {
        if (battlePanel == null)        Debug.LogError($"{nameof(battlePanel)} не назначен!");
        if (enemyNameText == null)      Debug.LogError($"{nameof(enemyNameText)} не назначен!");
        if (requiredAttackText == null) Debug.LogError($"{nameof(requiredAttackText)} не назначен!");
        if (effectText == null)         Debug.LogError($"{nameof(effectText)} не назначен!");
        if (diceResultText == null)     Debug.LogError($"{nameof(diceResultText)} не назначен!");
        if (endTurnButton == null)      Debug.LogError($"{nameof(endTurnButton)} не назначен!");
        if (cardPanel == null)          Debug.LogWarning($"{nameof(cardPanel)} не назначен!");
        if (diceButton == null)         Debug.LogWarning($"{nameof(diceButton)} не назначен!");
    }

    // --------------------
    // СОСТОЯНИЯ
    // --------------------

    public void SetIdleState()
    {
        enemyNameText.text      = "Ожидание события...";
        requiredAttackText.text = "";
        effectText.text         = "";
        diceResultText.text     = "";
        ShowEndTurnButton(false);
        ShowDiceButton(true); // В начале хода кнопка броска доступна
        ShowCardPanel(false);  // Карточка врага скрыта вне боя
    }

    // --------------------
    // БОЙ
    // --------------------

    public void DisplayChallenge(BattleCardData card)
    {
        if (card == null) { SetIdleState(); return; }

        enemyNameText.text      = $"Битва: {card.enemyName}";
        requiredAttackText.text = $"Требуемая атака: {card.requiredAttack}";
        effectText.text         = "Бросаем кубик...";
        diceResultText.text     = "";
        ShowCardPanel(true);   // Показываем карточку врага только в бою
    }

    public void DisplayDiceRoll(int diceResult, int baseAttack)
    {
        diceResultText.text = $"Базовая атака: {baseAttack}\nКубик: <color=yellow>+{diceResult}</color>";
    }

    public void ShowPreliminaryResult(bool wouldWin)
    {
        effectText.text = wouldWin
            ? "<color=green>СИЛ ДОСТАТОЧНО!</color>"
            : "<color=red>СИЛ НЕ ХВАТАЕТ!</color> Сыграйте карту или завершите ход.";
    }

    public void DisplayBattleResult(bool victory, BattleCardData card, int playerFinalAttack)
    {
        requiredAttackText.text = $"Ваша атака: {playerFinalAttack} (требуется: {card.requiredAttack})";

        int moneyChange = victory ? card.rewardMoney : card.penaltyMoney;
        string moneyStr = moneyChange.ToString("+#;-#;0");
        effectText.text = victory
            ? $"<color=green>ПОБЕДА!</color> Награда: {moneyStr} фелсов."
            : $"<color=red>ПОРАЖЕНИЕ!</color> Штраф: {Mathf.Abs(moneyChange)} фелсов.";
    }

    // --------------------
    // СОБЫТИЯ
    // --------------------

    public void DisplayEventInfo(DiceEventType type, int diceValue)
    {
        if (eventDescriptionText == null) return;

        eventDescriptionText.text = type switch
        {
            DiceEventType.Battle          => $"Выпало {diceValue}: впереди враги!",
            DiceEventType.ShadowInfluence => $"Выпало {diceValue}: тень сгущается...",
            DiceEventType.PeacefulPass    => $"Выпало {diceValue}: путь чист.",
            _                             => $"Выпало {diceValue}."
        };
    }

    public void DisplayShadowCard(ShadowCardData card)
    {
        if (card == null || eventDescriptionText == null) return;

        string color   = card.value >= 0 ? "green" : "red";
        string sign    = card.value >= 0 ? "+" : "";
        string message = $"<b><color=purple>ВЛИЯНИЕ ТЕНИ:</color></b> {card.cardName}\n{card.description}\n"
                       + $"Эффект: <color={color}>{sign}{card.value} {card.effectType}</color>";
        if (card.isTemporary)
            message += $" (на {card.duration} ходов)";

        eventDescriptionText.text = message;
        ShowEndTurnButton(true);
    }

    // --------------------
    // КНОПКИ
    // --------------------

    public Button EndTurnButton => endTurnButton;

    public void ShowEndTurnButton(bool isVisible)
    {
        if (endTurnButton != null)
            endTurnButton.gameObject.SetActive(isVisible);
    }

    public void ShowCardPanel(bool isVisible)
    {
        if (cardPanel != null)
            cardPanel.SetActive(isVisible);
    }

    /// <summary>
    /// Управляет видимостью кнопки броска кубика.
    /// Скрывается при входе в ResolvingEvent, возвращается при новом ходе.
    /// </summary>
    public void ShowDiceButton(bool isVisible)
    {
        if (diceButton != null)
            diceButton.gameObject.SetActive(isVisible);
    }

    public void ClearEventText()
    {
        if (eventDescriptionText != null) eventDescriptionText.text = "";
        if (enemyNameText != null)        enemyNameText.text        = "";
        if (requiredAttackText != null)   requiredAttackText.text   = "";
        if (diceResultText != null)       diceResultText.text       = "";
        if (effectText != null)           effectText.text           = "";
        ShowCardPanel(false);  // Прячем карточку врага после завершения хода
    }

    // --------------------
    // ЗАГЛУШКИ ДЛЯ ОБРАТНОЙ СОВМЕСТИМОСТИ
    // --------------------

    [System.Obsolete("Используй ShowDiceButton(). Заглушка для совместимости.")]
    public void ShowBattleRollButton(bool isVisible) { }

    [System.Obsolete("Кнопка финализации удалена. Заглушка для совместимости.")]
    public void EnableFinishBattleButton(bool isActive) { }
}