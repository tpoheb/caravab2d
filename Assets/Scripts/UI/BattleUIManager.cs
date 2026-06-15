using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class BattleUIManager : MonoBehaviour
{
    [Header("EventPanel — текстовые поля")]
    [SerializeField] private TMP_Text rewardText;
    [SerializeField] private TMP_Text effectText;
    [SerializeField] private TMP_Text resultText;

    [Header("Панель и кнопки")]
    [SerializeField] private GameObject battlePanel;
    [SerializeField] private Button endTurnButton;
    [SerializeField] private Button diceButton;

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
        if (rewardText    == null) Debug.LogWarning($"{nameof(rewardText)} не назначен!");
        if (effectText    == null) Debug.LogWarning($"{nameof(effectText)} не назначен!");
        if (resultText    == null) Debug.LogWarning($"{nameof(resultText)} не назначен!");
        if (diceButton    == null) Debug.LogWarning($"{nameof(diceButton)} не назначен!");
    }

    public void SetIdleState()
    {
        ClearAll();
        ShowEndTurnButton(false);
        ShowDiceButton(true);
    }

    public void DisplayDiceRoll(int diceResult, int baseAttack, int enemyAttack)
    {
        int totalAttack = baseAttack + diceResult;
        bool wouldWin   = totalAttack >= enemyAttack;

        if (rewardText != null)
            rewardText.text = $"Ваша атака: <b>{baseAttack}</b> + <color=yellow>{diceResult}</color> = <b>{totalAttack}</b>\n"
                            + $"Атака врага: <b>{enemyAttack}</b>";

        if (effectText != null)
            effectText.text = wouldWin
                ? "<color=green>Сил достаточно!</color>"
                : "<color=red>Сил не хватает!</color>";
    }

    public void DisplayBattleResult(bool victory, BattleCardData card, int playerFinalAttack)
    {
        if (resultText != null)
            resultText.text = victory
                ? $"<color=green><b>ПОБЕДА!</b></color> +{card.rewardMoney} фелсов"
                : $"<color=red><b>ПОРАЖЕНИЕ!</b></color> {card.penaltyMoney} фелсов";
    }

    /// <summary>
    /// Показывает сообщение о побеге (Дымовая Завеса).
    /// Вызывается BattleManager.PrepareBattle и ForceEndBattle.
    /// </summary>
    public void DisplayEscapeMessage(string enemyName)
    {
        if (rewardText != null)
            rewardText.text = $"Встреча с <b>{enemyName}</b>";

        if (effectText != null)
            effectText.text = "<color=yellow>Дымовая завеса!</color>";

        if (resultText != null)
            resultText.text = "Вы скрылись без потерь.";
    }

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

    public void ClearEventText() => ClearAll();

    private void ClearAll()
    {
        if (rewardText != null) rewardText.text = "";
        if (effectText != null) effectText.text = "";
        if (resultText != null) resultText.text = "";
    }

    [System.Obsolete("Используй DisplayDiceRoll(diceResult, baseAttack, enemyAttack).")]
    public void DisplayDiceRoll(int diceResult, int baseAttack) => DisplayDiceRoll(diceResult, baseAttack, 0);

    [System.Obsolete("Предварительный результат теперь внутри DisplayDiceRoll.")]
    public void ShowPreliminaryResult(bool wouldWin) { }

    [System.Obsolete("Текст события теперь на карте.")]
    public void DisplayEventInfo(DiceEventType type, int diceValue) { }

    [System.Obsolete("Текст тени теперь на карте.")]
    public void DisplayShadowCard(ShadowCardData card) { }

    [System.Obsolete("Текст битвы теперь на карте.")]
    public void DisplayChallenge(BattleCardData card) { }

    [System.Obsolete("Используй ShowDiceButton().")]
    public void ShowBattleRollButton(bool isVisible) { }

    [System.Obsolete("Кнопка финализации удалена.")]
    public void EnableFinishBattleButton(bool isActive) { }

    [System.Obsolete("cardPanel перенесён в EventCardDeckUI.")]
    public void ShowCardPanel(bool isVisible) { }
}