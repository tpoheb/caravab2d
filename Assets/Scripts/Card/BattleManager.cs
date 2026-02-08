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

        int playerBaseAttack = teamSystem.GetTotalAttack();
        int totalPlayerAttack = playerBaseAttack + diceValue;

        // Выводим информацию о броске в специальное поле кубика
        uiManager.DisplayDiceRoll(diceValue, playerBaseAttack);

        // Определяем исход
        bool isVictory = totalPlayerAttack >= _currentEnemyAttack;

        // Вызываем финальный UI результат (в нем уже заложена логика текста награды/штрафа)
        uiManager.DisplayBattleResult(isVictory, _currentCard, totalPlayerAttack);

        if (isVictory) Win();
        else Lose();
    }

    private void Win()
    {
        // Берем награду напрямую из карточки
        teamSystem.AddMoney(_currentCard.rewardMoney);
        // Выдаем карту руки в качестве награды
        HandManager.Instance.GiveRandomReward();
        OnBattleWon?.Invoke();
    }

    private void Lose()
    {
        // Берем штраф напрямую из карточки (используем Mathf.Abs, чтобы случайно не прибавить деньги, если в SO записано отрицательное число)
        teamSystem.RemoveMoney(Mathf.Abs(_currentCard.penaltyMoney));
        OnBattleLost?.Invoke();
    }
}