using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Управляет общими UI-элементами боевой панели.
/// Текстовое отображение карт событий и битв перенесено в EventCardDeckUI / EventCardDisplay.
/// </summary>
public class BattleUIManager : MonoBehaviour
{
    [Header("Панель")]
    [SerializeField] private GameObject battlePanel;

    [Header("Кнопки")]
    [SerializeField] private Button endTurnButton;
    [SerializeField] private Button diceButton;

    // ──────────────────────────────────────────────────────────────────────

    private void Start()
    {
        if (battlePanel != null) battlePanel.SetActive(true);
        ValidateReferences();
        SetIdleState();
    }

    private void ValidateReferences()
    {
        if (battlePanel   == null) Debug.LogError($"{nameof(battlePanel)} не назначен!");
        if (endTurnButton == null) Debug.LogError($"{nameof(endTurnButton)} не назначен!");
        if (diceButton    == null) Debug.LogWarning($"{nameof(diceButton)} не назначен!");
    }

    // ──────────────────────────────────────────────────────────────────────
    // Состояния
    // ──────────────────────────────────────────────────────────────────────

    public void SetIdleState()
    {
        ShowEndTurnButton(false);
        ShowDiceButton(true);
    }

    // ──────────────────────────────────────────────────────────────────────
    // Кубик / результаты броска
    // ──────────────────────────────────────────────────────────────────────

    /// <summary>
    /// Вызывается из BattleManager после броска.
    /// Если нужно показать результат — передай его в EventCardDisplay через CardManager.
    /// Оставлен как точка расширения.
    /// </summary>
    public void DisplayDiceRoll(int diceResult, int baseAttack)
    {
        // Результат броска теперь отображается на лицевой стороне карты события.
        // При необходимости добавь отдельный TMP_Text для diceResult здесь.
        Debug.Log($"[BattleUIManager] Бросок: базовая атака {baseAttack}, кубик +{diceResult}");
    }

    /// <summary>Итог битвы — победа или поражение.</summary>
    public void DisplayBattleResult(bool victory, BattleCardData card, int playerFinalAttack)
    {
        Debug.Log($"[BattleUIManager] Результат: {(victory ? "ПОБЕДА" : "ПОРАЖЕНИЕ")}, " +
                  $"атака {playerFinalAttack} / требуется {card.requiredAttack}");
        // Визуальный результат показывается на карте события.
        // Здесь можно добавить отдельный оверлей (звезда победы, крест поражения и т.п.)
    }

    // ──────────────────────────────────────────────────────────────────────
    // Кнопки
    // ──────────────────────────────────────────────────────────────────────

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

    /// <summary>Сброс UI в начале нового хода.</summary>
    public void ClearEventText()
    {
        // Текст события живёт на карте — карта скрывается через CardManager.HideEventCard().
        // Метод оставлен для совместимости с вызовами из GameManager.
    }

    // ──────────────────────────────────────────────────────────────────────
    // Заглушки обратной совместимости
    // ──────────────────────────────────────────────────────────────────────

    [System.Obsolete("Текст события теперь на карте. Заглушка для совместимости.")]
    public void DisplayEventInfo(DiceEventType type, int diceValue) { }

    [System.Obsolete("Текст тени теперь на карте. Заглушка для совместимости.")]
    public void DisplayShadowCard(ShadowCardData card) { }

    [System.Obsolete("Текст битвы теперь на карте. Заглушка для совместимости.")]
    public void DisplayChallenge(BattleCardData card) { }

    [System.Obsolete("Используй ShowDiceButton(). Заглушка для совместимости.")]
    public void ShowBattleRollButton(bool isVisible) { }

    [System.Obsolete("Кнопка финализации удалена. Заглушка для совместимости.")]
    public void EnableFinishBattleButton(bool isActive) { }

    [System.Obsolete("Результат показывается на карте. Заглушка для совместимости.")]
    public void ShowPreliminaryResult(bool wouldWin) { }

    [System.Obsolete("cardPanel перенесён в EventCardDeckUI. Заглушка для совместимости.")]
    public void ShowCardPanel(bool isVisible) { }
}