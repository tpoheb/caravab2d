using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Управляет кнопками боевой панели и делегирует отображение текстов в EventPanelUI.
/// Текстовые поля (rewardText, effectText, resultText) удалены —
/// BattleUIManager больше не владеет TMP-объектами напрямую.
/// </summary>
public class BattleUIManager : MonoBehaviour
{
    [Header("Панель и кнопки")]
    [SerializeField] private GameObject battlePanel;
    [SerializeField] private Button endTurnButton;
    [SerializeField] private Button diceButton;
    [SerializeField] private TextMeshProUGUI phaseText;

    [Header("Зависимости")]
    [SerializeField] private EventPanelUI eventPanel;

    private void Start()
    {
        if (battlePanel != null) battlePanel.SetActive(true);
        ValidateReferences();
        if (GameManager.Instance != null)
            ShowPhase(GameManager.Instance.State, false, GameManager.Instance.CanAdvancePhase);
        else SetIdleState();
    }

    private void ValidateReferences()
    {
        if (battlePanel   == null) Debug.LogError($"[BattleUIManager] {nameof(battlePanel)} не назначен!");
        if (endTurnButton == null) Debug.LogError($"[BattleUIManager] {nameof(endTurnButton)} не назначен!");
        if (diceButton    == null) Debug.LogWarning($"[BattleUIManager] {nameof(diceButton)} не назначен!");
        if (eventPanel    == null) Debug.LogError($"[BattleUIManager] {nameof(eventPanel)} не назначен — тексты боя не будут показаны!");
    }

    // ── Состояния ─────────────────────────────────────────────────────────

    public void SetIdleState()
    {
        eventPanel?.ClearAll();
        ShowEndTurnButton(false);
        ShowDiceButton(false);
    }

    /// <summary>Одна существующая кнопка продолжает подготовку, встречу и постфазу.</summary>
    public void ShowPhase(GameState state, bool isBattle, bool canContinue)
    {
        // В MapPrototipe старая кнопка кубика используется для вытягивания карты.
        if (state != GameState.DrawingCard) ShowDiceButton(false);
        string label;
        string phase;
        switch (state)
        {
            case GameState.PreparingEncounter:
                label = isBattle ? "Начать битву" : "Начать событие";
                phase = isBattle ? "Подготовка к битве" : "Подготовка к событию";
                break;
            case GameState.InBattle:
                label = "Завершить бой";
                phase = "Битва";
                break;
            case GameState.ResolvingEvent:
                label = "Применить событие";
                phase = "Событие";
                break;
            case GameState.PostEncounter:
                label = "Завершить ход";
                phase = isBattle ? "После битвы" : "После события";
                break;
            default:
                ShowEndTurnButton(false);
                return;
        }

        ShowEndTurnButton(true);
        if (endTurnButton == null) return;
        endTurnButton.interactable = canContinue;
        var buttonText = endTurnButton.GetComponentInChildren<TextMeshProUGUI>(true);
        if (buttonText != null) buttonText.text = label;
        EnsurePhaseText(buttonText);
        if (phaseText != null) phaseText.text = phase;
    }

    private void EnsurePhaseText(TextMeshProUGUI template)
    {
        if (phaseText != null || template == null) return;
        var obj = new GameObject("Текущая фаза", typeof(RectTransform));
        obj.transform.SetParent(endTurnButton.transform, false);
        phaseText = obj.AddComponent<TextMeshProUGUI>();
        phaseText.font = template.font;
        phaseText.fontSize = 18;
        phaseText.alignment = TextAlignmentOptions.Center;
        phaseText.raycastTarget = false;
        var rect = phaseText.rectTransform;
        rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 1f);
        rect.pivot = new Vector2(0.5f, 0f);
        rect.anchoredPosition = new Vector2(0f, 8f);
        rect.sizeDelta = new Vector2(420f, 32f);
    }

    public void DisplaySettledPenalty(int amount) => eventPanel?.DisplayBattleResult(false, -amount);

    // ── Тексты — делегируем в EventPanelUI ───────────────────────────────

    /// <summary>
    /// Результат броска кубика: атака игрока vs атака врага.
    /// </summary>
    public void DisplayDiceRoll(int diceResult, int baseAttack, int enemyAttack)
        => eventPanel?.DisplayDiceRoll(diceResult, baseAttack, enemyAttack);

    /// <summary>
    /// Итог боя: победа или поражение.
    /// </summary>
    public void DisplayBattleResult(bool victory, BattleCardData card, int playerFinalAttack)
    {
        int amount = victory ? card.rewardMoney : card.penaltyMoney;
        eventPanel?.DisplayBattleResult(victory, amount);
    }

    /// <summary>
    /// Сообщение о побеге (Дымовая Завеса).
    /// </summary>
    public void DisplayEscapeMessage(string enemyName)
        => eventPanel?.DisplayEscapeMessage(enemyName);

    // ── Кнопки ────────────────────────────────────────────────────────────

    public Button EndTurnButton => endTurnButton;

    public void ShowEndTurnButton(bool isVisible)
    {
        if (endTurnButton != null)
            endTurnButton.gameObject.SetActive(isVisible);
    }

    public void ShowDiceButton(bool isVisible)
    {
        if (diceButton != null)
            diceButton.gameObject.SetActive(isVisible);
    }

    // ── Устаревшие методы (совместимость) ────────────────────────────────

    [System.Obsolete("Используй DisplayDiceRoll(diceResult, baseAttack, enemyAttack).")]
    public void DisplayDiceRoll(int diceResult, int baseAttack)
        => DisplayDiceRoll(diceResult, baseAttack, 0);

    [System.Obsolete("Предварительный результат теперь внутри DisplayDiceRoll.")]
    public void ShowPreliminaryResult(bool wouldWin) { }

    [System.Obsolete("Используй ShowDiceButton().")]
    public void ShowBattleRollButton(bool isVisible) { }

    [System.Obsolete("Кнопка финализации удалена.")]
    public void EnableFinishBattleButton(bool isActive) { }

    [System.Obsolete("Очистка текстов через EventPanelUI.ClearAll().")]
    public void ClearEventText() => eventPanel?.ClearAll();
}
