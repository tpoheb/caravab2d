using UnityEngine;
using UnityEngine.UI;

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

    [Header("Зависимости")]
    [SerializeField] private EventPanelUI eventPanel;

    private void Start()
    {
        if (battlePanel != null) battlePanel.SetActive(true);
        ValidateReferences();
        SetIdleState();
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
        ShowDiceButton(true);
    }

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