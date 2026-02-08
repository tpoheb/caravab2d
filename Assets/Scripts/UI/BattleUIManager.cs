using UnityEngine;
using TMPro; 
using UnityEngine.UI; 
using System; 

public class BattleUIManager : MonoBehaviour
{
    // --- ИЗДАТЕЛЬ СОБЫТИЯ ---
    
    [Header("Панель событий")]
    [SerializeField] private TMP_Text eventDescriptionText; // Текст: "Мирный проход", "Засада!" и т.д.
    [SerializeField] private Button endTurnButton;
    [SerializeField] private Button rollForAttackButton;
    
    [Header("UI Элементы Битвы")]
    [SerializeField] private GameObject battlePanel; // Основная панель (ВСЕГДА АКТИВНА)
    [SerializeField] private TMP_Text enemyNameText; 
    [SerializeField] private TMP_Text requiredAttackText; 
    [SerializeField] private TMP_Text effectText; 
    
    [Header("Элементы Кубика")]
    [SerializeField] private TMP_Text diceResultText; 

    private void Start()
    {
        // Панель активируется один раз при старте
        battlePanel.SetActive(true); 
        
        ValidateReferences();
        
        // Устанавливаем начальное состояние
        SetIdleState();
    }
    
    // --- ДОБАВЛЕНО: ЛОГИКА НАЖАТИЯ КНОПКИ ---
   

    /// <summary>
    /// ДОБАВЛЕНО: Проверка назначенных ссылок.
    /// </summary>
    private void ValidateReferences()
    {
        if (battlePanel == null) Debug.LogError($"{nameof(battlePanel)} не назначен!");
        if (enemyNameText == null) Debug.LogError($"{nameof(enemyNameText)} не назначен!");
        if (requiredAttackText == null) Debug.LogError($"{nameof(requiredAttackText)} не назначен!");
        if (effectText == null) Debug.LogError($"{nameof(effectText)} не назначен!");
        if (diceResultText == null) Debug.LogError($"{nameof(diceResultText)} не назначен!");
    }


    // --- НОВЫЙ МЕТОД ---
    /// <summary>
    /// Устанавливает UI в состояние ожидания, когда битва не активна.
    /// </summary>
    public void SetIdleState()
    {
        enemyNameText.text = "Ожидание события...";
        requiredAttackText.text = "";
        effectText.text = "";
        diceResultText.text = "";
    }

    /// <summary>
    /// (ШАГ 1) Отображает вызов битвы.
    /// </summary>
    public void DisplayChallenge(BattleCardData card)
    {
        if (card == null)
        {
            SetIdleState(); // Возврат к ожиданию, если карты нет
            return;
        }

        // Заполнение полей вызова
        enemyNameText.text = $"Битва: {card.enemyName}";
        requiredAttackText.text = $"Требуемая Атака: {card.requiredAttack}";
        effectText.text = "Бросьте кубик!"; 
        diceResultText.text = "Базовая атака: ...";
        
    }
    
    public void DisplayDiceRoll(int result, int baseAttack)
    {
        // Отображение базовой атаки игрока + результат кубика
        diceResultText.text = $"Базовая атака: {baseAttack}\nКубик: <color=yellow>+{result}</color>";
        effectText.text = "Расчет результата...";
    }

    /// <summary>
    /// (ШАГ 2) Отображает ФИНАЛЬНЫЙ результат битвы.
    /// </summary>
    public void DisplayBattleResult(bool victory, BattleCardData card, int playerFinalAttack)
    {
        string resultMessage;
        int moneyChange = victory ? card.rewardMoney : card.penaltyMoney;
        string moneyString = moneyChange.ToString("+#;-#;0");
        
        // Отображение финальной атаки игрока
        requiredAttackText.text = $"Ваша Атака: {playerFinalAttack} (Требуется: {card.requiredAttack})";

        if (victory)
        {
            resultMessage = $"<color=green>ПОБЕДА!</color>\nНаграда: {moneyString} фелсов.";
        }
        else
        {
            resultMessage = $"<color=red>ПОРАЖЕНИЕ!</color>\nШтраф: {Mathf.Abs(moneyChange)} фелсов.";
        }

        effectText.text = resultMessage;
        
    }
    public Button EndTurnButton => endTurnButton;
    /// <summary>
    /// Выводит описание результата броска на экран.
    /// </summary>
    public void DisplayEventInfo(DiceEventType type, int diceValue)
    {
        string message = "";

        switch (type)
        {
            case DiceEventType.Battle:
                message = $"Выпало {diceValue}: Впереди враги! Приготовьтесь к бою.";
                break;
            case DiceEventType.ShadowInfluence:
                message = $"Выпало {diceValue}: Тень сгущается... Вы тянете карту судьбы.";
                break;
            case DiceEventType.PeacefulPass:
                message = $"Выпало {diceValue}: Путь чист. Можно продолжать движение.";
                break;
        }

        if (eventDescriptionText != null)
            eventDescriptionText.text = message;

        // Активируем кнопку "Завершить ход", чтобы игрок мог нажать её и пойти дальше
        if (endTurnButton != null)
            endTurnButton.interactable = true;
    }
    public void ShowBattleRollButton(bool isVisible)
    {
        if (rollForAttackButton != null)
        {
            rollForAttackButton.gameObject.SetActive(isVisible);
            Debug.Log($"UI: Кнопка броска атаки {(isVisible ? "ПОКАЗАНА" : "СКРЫТА")}");
        }
    }
    public void ShowEndTurnButton(bool isVisible)
    {
        if (endTurnButton != null)
        {
            endTurnButton.gameObject.SetActive(isVisible);
            Debug.Log($"UI: Кнопка завершения хода {(isVisible ? "ПОКАЗАНА" : "СКРЫТА")}");
        }
    }
    public void UpdateBattleResultUI(string resultText)
    {
    if (eventDescriptionText != null)
        eventDescriptionText.text = resultText; 
    ShowBattleRollButton(false); // Прячем кнопку атаки
    ShowEndTurnButton(true);     // Показываем кнопку завершения хода
    }
    public void DisplayShadowCard(ShadowCardData card)
    {
        if (card == null) return;

        // Используем уже имеющийся eventDescriptionText
        string color = card.value >= 0 ? "green" : "red";
        string sign = card.value >= 0 ? "+" : "";
    
        string message = $"<b><color=purple>ВЛИЯНИЕ ТЕНИ:</color></b> {card.cardName}\n";
        message += $"{card.description}\n";
        message += $"Эффект: <color={color}>{sign}{card.value} {card.effectType}</color>";
    
        if (card.isTemporary) message += $" (на {card.duration} ходов)";

        if (eventDescriptionText != null)
            eventDescriptionText.text = message;

        ShowEndTurnButton(true); // Даем игроку нажать "Завершить ход" после прочтения
    }
    public void ClearEventText()
    {
        // Очистка основного описания события
        if (eventDescriptionText != null) 
            eventDescriptionText.text = "";

        // Очистка данных карточки битвы
        if (enemyNameText != null) 
            enemyNameText.text = "";

        if (requiredAttackText != null) 
            requiredAttackText.text = "";

        // Очистка результата броска кубика (число и бонус)
        if (diceResultText != null) 
            diceResultText.text = "";

        Debug.Log("UI: Все текстовые поля события очищены.");
    }
}