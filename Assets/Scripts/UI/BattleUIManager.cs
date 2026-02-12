using UnityEngine;
using TMPro; 
using UnityEngine.UI; 
using System; 

public class BattleUIManager : MonoBehaviour
{
    [Header("Панель событий")]
    [SerializeField] private TMP_Text eventDescriptionText; 
    [SerializeField] private Button endTurnButton;
    [SerializeField] private Button rollForAttackButton;
    
    [Header("UI Элементы Битвы")]
    [SerializeField] private GameObject battlePanel; 
    [SerializeField] private TMP_Text enemyNameText; 
    [SerializeField] private TMP_Text requiredAttackText; 
    [SerializeField] private TMP_Text effectText; 
    [SerializeField] private TMP_Text resultText; // Изменил тип на TMP_Text для консистентности
    [SerializeField] private Button finalizeButton;    

    [Header("Элементы Кубика")]
    [SerializeField] private TMP_Text diceResultText; 

    private void Start()
    {
        battlePanel.SetActive(true); 
        ValidateReferences();
        SetIdleState();

        // Скрываем кнопку финализации в начале
        EnableFinishBattleButton(false);
    }

    private void ValidateReferences()
    {
        if (battlePanel == null) Debug.LogError($"{nameof(battlePanel)} не назначен!");
        if (enemyNameText == null) Debug.LogError($"{nameof(enemyNameText)} не назначен!");
        if (requiredAttackText == null) Debug.LogError($"{nameof(requiredAttackText)} не назначен!");
        if (effectText == null) Debug.LogError($"{nameof(effectText)} не назначен!");
        if (diceResultText == null) Debug.LogError($"{nameof(diceResultText)} не назначен!");
        if (finalizeButton == null) Debug.LogWarning("Кнопка 'Finalize' не назначена!");
    }

    public void SetIdleState()
    {
        enemyNameText.text = "Ожидание события...";
        requiredAttackText.text = "";
        effectText.text = "";
        diceResultText.text = "";
        EnableFinishBattleButton(false);
    }

    public void DisplayChallenge(BattleCardData card)
    {
        if (card == null)
        {
            SetIdleState();
            return;
        }

        enemyNameText.text = $"Битва: {card.enemyName}";
        requiredAttackText.text = $"Требуемая Атака: {card.requiredAttack}";
        effectText.text = "Бросьте кубик!"; 
        diceResultText.text = "Базовая атака: ...";
        EnableFinishBattleButton(false);
    }
    
    public void DisplayDiceRoll(int result, int baseAttack)
    {
        diceResultText.text = $"Базовая атака: {baseAttack}\nКубик: <color=yellow>+{result}</color>";
        effectText.text = "Ожидание решения...";
    }

    // --- НОВЫЙ МЕТОД: Предварительный результат ---
    public void ShowPreliminaryResult(bool wouldWin)
    {
        if (wouldWin)
        {
            effectText.text = "<color=green>СИЛ ДОСТАТОЧНО!</color>\nНажмите 'Принять результат'.";
        }
        else
        {
            effectText.text = "<color=red>СИЛ НЕ ХВАТАЕТ!</color>\nИспользуйте карту или примите поражение.";
        }
    }

    // --- НОВЫЙ МЕТОД: Управление кнопкой завершения ---
    public void EnableFinishBattleButton(bool isActive)
    {
        if (finalizeButton != null)
        {
            finalizeButton.gameObject.SetActive(isActive);
        }
    }

    public void DisplayBattleResult(bool victory, BattleCardData card, int playerFinalAttack)
    {
        int moneyChange = victory ? card.rewardMoney : card.penaltyMoney;
        string moneyString = moneyChange.ToString("+#;-#;0");
        
        requiredAttackText.text = $"Ваша Атака: {playerFinalAttack} (Требуется: {card.requiredAttack})";

        string resultMessage = victory 
            ? $"<color=green>ПОБЕДА!</color>\nНаграда: {moneyString} фелсов." 
            : $"<color=red>ПОРАЖЕНИЕ!</color>\nШтраф: {Mathf.Abs(moneyChange)} фелсов.";

        effectText.text = resultMessage;
    }

    public void UpdateBattleResultUI(string resultTextContent)
    {
        if (eventDescriptionText != null)
            eventDescriptionText.text = resultTextContent; 
        
        ShowBattleRollButton(false);
        EnableFinishBattleButton(false); // Прячем кнопку финала после завершения
        ShowEndTurnButton(true);
    }

    // --- Остальные методы без изменений ---
    public Button EndTurnButton => endTurnButton;

    public void DisplayEventInfo(DiceEventType type, int diceValue)
    {
        string message = "";
        switch (type)
        {
            case DiceEventType.Battle:
                message = $"Выпало {diceValue}: Впереди враги!";
                break;
            case DiceEventType.ShadowInfluence:
                message = $"Выпало {diceValue}: Тень сгущается...";
                break;
            case DiceEventType.PeacefulPass:
                message = $"Выпало {diceValue}: Путь чист.";
                break;
        }

        if (eventDescriptionText != null)
            eventDescriptionText.text = message;

        if (endTurnButton != null)
            endTurnButton.interactable = true;
    }

    public void ShowBattleRollButton(bool isVisible)
    {
        if (rollForAttackButton != null)
            rollForAttackButton.gameObject.SetActive(isVisible);
    }

    public void ShowEndTurnButton(bool isVisible)
    {
        if (endTurnButton != null)
            endTurnButton.gameObject.SetActive(isVisible);
    }

    public void DisplayShadowCard(ShadowCardData card)
    {
        if (card == null) return;
        string color = card.value >= 0 ? "green" : "red";
        string sign = card.value >= 0 ? "+" : "";
        string message = $"<b><color=purple>ВЛИЯНИЕ ТЕНИ:</color></b> {card.cardName}\n{card.description}\n";
        message += $"Эффект: <color={color}>{sign}{card.value} {card.effectType}</color>";
        if (card.isTemporary) message += $" (на {card.duration} ходов)";

        if (eventDescriptionText != null) eventDescriptionText.text = message;
        ShowEndTurnButton(true);
    }

    public void ClearEventText()
    {
        if (eventDescriptionText != null) eventDescriptionText.text = "";
        if (enemyNameText != null) enemyNameText.text = "";
        if (requiredAttackText != null) requiredAttackText.text = "";
        if (diceResultText != null) diceResultText.text = "";
        EnableFinishBattleButton(false);
    }
}