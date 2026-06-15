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

    // Бонус к атаке, добавленный картой руки (AddBonus / Благосклонность Звезд)
    // Сбрасывается после каждого боя
    private int _attackBonus = 0;

    public BattleUIManager GetUIManager() => uiManager;

    // ─────────────────────────────────────────────────────────────────────
    // Подготовка боя
    // ─────────────────────────────────────────────────────────────────────

    /// <summary>
    /// Вызывается из CardManager ПОСЛЕ флипа карты.
    /// Возвращает false, если бой был пропущен через EscapeBattle.
    /// </summary>
    public bool PrepareBattle(BattleCardData card)
    {
        if (card == null)
        {
            Debug.LogError("[BattleManager] карточка боя не передана!");
            return false;
        }

        // Проверяем дымовую завесу ДО инициализации боя
        if (HandManager.Instance != null && HandManager.Instance.ConsumeEscapeBattle())
        {
            Debug.Log("[BattleManager] EscapeBattle: бой пропущен дымовой завесой.");
            uiManager?.DisplayEscapeMessage(card.enemyName);
            _currentCard = null;
            return false;
        }

        _currentCard        = card;
        _currentEnemyAttack = card.requiredAttack;
        _lastDiceRoll       = 0;
        _attackBonus        = 0;
        _battleResolved     = false;

        Debug.Log($"[BattleManager] Бой с {card.enemyName}, требуемая атака: {_currentEnemyAttack}");
        return true;
    }

    // ─────────────────────────────────────────────────────────────────────
    // Бросок кубика
    // ─────────────────────────────────────────────────────────────────────

    public void ExecuteBattle(int diceValue)
    {
        if (_currentCard == null) return;

        _lastDiceRoll = diceValue;

        int playerBase  = teamSystem.GetTotalAttack() + _attackBonus;
        int playerTotal = playerBase + diceValue;

        uiManager.DisplayDiceRoll(diceValue, playerBase, _currentEnemyAttack);
    }

    public void RequestNewRoll()
    {
        int newDice = UnityEngine.Random.Range(1, 7);
        Debug.Log($"[BattleManager] Переброс → {newDice}");
        ExecuteBattle(newDice);
    }

    /// <summary>
    /// Добавляет временный бонус к атаке на текущий бой.
    /// Вызывается картой руки AddBonus (Благосклонность Звезд).
    /// Сбрасывается в FinalizeBattle / ForceEndBattle.
    /// </summary>
    public void AddAttackBonus(int value)
    {
        _attackBonus += value;
        Debug.Log($"[BattleManager] AttackBonus: +{value} (итого бонус: {_attackBonus}).");

        // Пересчитываем отображение с учётом нового бонуса
        if (_lastDiceRoll > 0)
            ExecuteBattle(_lastDiceRoll);
    }

    // ─────────────────────────────────────────────────────────────────────
    // Принудительный выход из боя (Дымовая завеса — если бой уже начался)
    // ─────────────────────────────────────────────────────────────────────

    /// <summary>
    /// Мгновенно завершает бой без штрафов и наград.
    /// Вызывается HandManager при использовании EscapeBattle во время InBattle.
    /// </summary>
    public void ForceEndBattle(bool escaped)
    {
        if (_battleResolved) return;

        _battleResolved = true;
        _attackBonus    = 0;

        if (escaped)
        {
            Debug.Log("[BattleManager] ForceEndBattle: сбежали без последствий.");
            uiManager?.DisplayEscapeMessage(_currentCard?.enemyName ?? "врага");
        }

        _currentCard = null;

        // Уведомляем GameManager, чтобы он перешёл в ResolvingEvent
        GameManager.Instance?.OnBattleForceEnded();
    }

    // ─────────────────────────────────────────────────────────────────────
    // Финализация (один раз за бой)
    // ─────────────────────────────────────────────────────────────────────

    public void FinalizeBattle()
    {
        if (_currentCard == null)
        {
            Debug.LogWarning("[BattleManager] FinalizeBattle вызван без активного боя.");
            return;
        }

        if (_battleResolved)
        {
            Debug.LogWarning("[BattleManager] FinalizeBattle вызван повторно — проигнорировано.");
            return;
        }

        _battleResolved = true;
        _attackBonus    = 0;

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
        Debug.Log($"[BattleManager] Победа, +{_currentCard.rewardMoney} фелсов.");
    }

    private void Lose()
    {
        teamSystem.RemoveMoney(Mathf.Abs(_currentCard.penaltyMoney));
        OnBattleLost?.Invoke();
        Debug.Log($"[BattleManager] Поражение, -{Mathf.Abs(_currentCard.penaltyMoney)} фелсов.");
    }
}