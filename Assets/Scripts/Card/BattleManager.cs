using UnityEngine;
using System;

public class BattleManager : MonoBehaviour
{
    public event Action OnBattleWon;
    public event Action OnBattleLost;

    [Header("Systems")]
    [SerializeField] private TeamSystem teamSystem;
    [SerializeField] private BattleUIManager uiManager;

    private BattleCardData _currentCard;
    private int _currentEnemyAttack;
    private int _lastDiceRoll;
    private bool _battleResolved = false;

    public BattleUIManager GetUIManager() => uiManager;

    // ─────────────────────────────────────────────────────────────────────
    // Подготовка боя
    // ─────────────────────────────────────────────────────────────────────

    /// <summary>
    /// Вызывается из CardManager ПОСЛЕ флипа карты — карта уже видна игроку.
    /// Только инициализирует данные боя; UI карты уже показан анимацией.
    /// </summary>
    public void PrepareBattle(BattleCardData card)
    {
        if (card == null)
        {
            Debug.LogError("BattleManager: карточка боя не передана!");
            return;
        }

        _currentCard        = card;
        _currentEnemyAttack = card.requiredAttack;
        _lastDiceRoll       = 0;
        _battleResolved     = false;

        Debug.Log($"BattleManager: бой с {card.enemyName}, требуемая атака: {_currentEnemyAttack}");
        // DisplayChallenge убран — карта уже показана через EventCardDisplay
    }

    // ─────────────────────────────────────────────────────────────────────
    // Бросок кубика
    // ─────────────────────────────────────────────────────────────────────

    public void ExecuteBattle(int diceValue)
    {
        if (_currentCard == null) return;

        _lastDiceRoll = diceValue;

        int playerBase  = teamSystem.GetTotalAttack();
        int playerTotal = playerBase + diceValue;
        bool wouldWin   = playerTotal >= _currentEnemyAttack;

        uiManager.DisplayDiceRoll(diceValue, playerBase);
        uiManager.ShowPreliminaryResult(wouldWin);
    }

    public void RequestNewRoll()
    {
        int newDice = UnityEngine.Random.Range(1, 7);
        Debug.Log($"BattleManager: переброс -> {newDice}");
        ExecuteBattle(newDice);
    }

    // ─────────────────────────────────────────────────────────────────────
    // Финализация (один раз за бой)
    // ─────────────────────────────────────────────────────────────────────

    public void FinalizeBattle()
    {
        if (_currentCard == null)
        {
            Debug.LogWarning("BattleManager: FinalizeBattle вызван без активного боя.");
            return;
        }

        if (_battleResolved)
        {
            Debug.LogWarning("BattleManager: FinalizeBattle вызван повторно — проигнорировано.");
            return;
        }

        _battleResolved = true;

        int playerTotal = teamSystem.GetTotalAttack() + _lastDiceRoll;
        bool isVictory  = playerTotal >= _currentEnemyAttack;

        uiManager.DisplayBattleResult(isVictory, _currentCard, playerTotal);

        if (isVictory) Win();
        else           Lose();
    }

    // ─────────────────────────────────────────────────────────────────────
    // Исходы
    // ─────────────────────────────────────────────────────────────────────

    private void Win()
    {
        teamSystem.AddMoney(_currentCard.rewardMoney);
        HandManager.Instance?.GiveRandomReward();
        OnBattleWon?.Invoke();
        Debug.Log($"BattleManager: победа, +{_currentCard.rewardMoney} фелсов");
    }

    private void Lose()
    {
        teamSystem.RemoveMoney(Mathf.Abs(_currentCard.penaltyMoney));
        OnBattleLost?.Invoke();
        Debug.Log($"BattleManager: поражение, -{Mathf.Abs(_currentCard.penaltyMoney)} фелсов");
    }
}