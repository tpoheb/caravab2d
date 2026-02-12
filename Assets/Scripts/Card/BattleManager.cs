using UnityEngine;
using System;


public class BattleManager : MonoBehaviour
{
    public event Action OnBattleWon;
    public event Action OnBattleLost;

    [Header("Systems")]
    [SerializeField] private TeamSystem teamSystem;
    [SerializeField] private BattleUIManager uiManager;

    // Ссылка на текущую карту битвы
    private BattleCardData _currentCard;
    private int _currentEnemyAttack;
    private int lastDiceRoll;

    public BattleUIManager GetUIManager() => uiManager;

    // Теперь принимаем карточку как параметр
    public void PrepareBattle(BattleCardData card)
    {
        if (card == null)
        {
            Debug.LogError("BattleManager: Попытка начать бой без карточки!");
            return;
        }

        _currentCard = card;
        // Берем атаку из данных карточки (можно добавить небольшой рандом, если хочешь)
        _currentEnemyAttack = card.requiredAttack;
        
        Debug.Log($"BattleManager: Битва с {card.enemyName}! Требуемая атака: {_currentEnemyAttack}");
        
        // Показываем вызов в UI (имя врага, нужная атака)
        uiManager.DisplayChallenge(card);
        uiManager.ShowBattleRollButton(true);
    }

    public void ExecuteBattle(int diceValue)
    {
        if (_currentCard == null) return;

        lastDiceRoll = diceValue; // Запоминаем бросок
        int playerBaseAttack = teamSystem.GetTotalAttack();
        int totalPlayerAttack = playerBaseAttack + diceValue;

        // Визуализируем бросок
        uiManager.DisplayDiceRoll(diceValue, playerBaseAttack);

        // ОПРЕДЕЛЯЕМ ПРЕДВАРИТЕЛЬНЫЙ ИСХОД
        bool wouldWin = totalPlayerAttack >= _currentEnemyAttack;

        // ВЫЗЫВАЕМ ТЕ САМЫЕ МЕТОДЫ (Ошибки исчезнут после сохранения BattleUIManager)
        uiManager.ShowPreliminaryResult(wouldWin);
        uiManager.EnableFinishBattleButton(true); 
    }

// Этот метод ты должен назначить на OnClick кнопки finalizeButton в инспекторе
    public void FinalizeBattle()
    {
        int playerBaseAttack = teamSystem.GetTotalAttack();
        int totalPlayerAttack = playerBaseAttack + lastDiceRoll;

        bool isVictory = totalPlayerAttack >= _currentEnemyAttack;

        uiManager.EnableFinishBattleButton(false);
        uiManager.DisplayBattleResult(isVictory, _currentCard, totalPlayerAttack);

        if (isVictory) Win();
        else Lose();
    }

    private void Win()
    {
        teamSystem.AddMoney(_currentCard.rewardMoney);

        if (HandManager.Instance != null)
        {
            HandManager.Instance.GiveRandomReward();
        }
        else
        {
            Debug.LogError("BattleManager: HandManager.Instance не найден! Проверь, есть ли скрипт на сцене.");
        }

        OnBattleWon?.Invoke();
    }
    
    public void RequestNewRoll()
    {
        // 1. Скрываем кнопку принятия результата, так как мы перебрасываем
        uiManager.EnableFinishBattleButton(false);

        // 2. Генерируем новое значение (от 1 до 6)
        int newDiceValue = UnityEngine.Random.Range(1, 7);

        Debug.Log($"[Battle] Переброс! Новое значение кубика: {newDiceValue}");

        // 3. Вызываем существующую логику расчета битвы с новым значением
        ExecuteBattle(newDiceValue);
    }
    private void Lose()
    {
        // Берем штраф напрямую из карточки (используем Mathf.Abs, чтобы случайно не прибавить деньги, если в SO записано отрицательное число)
        teamSystem.RemoveMoney(Mathf.Abs(_currentCard.penaltyMoney));
        OnBattleLost?.Invoke();
    }
}