using UnityEngine;
using System;

/// <summary>
/// Управляет логикой боя: подготовка, бросок кубика, финализация.
///
/// Изменения по сравнению с исходником:
/// — Состояние боя инкапсулировано в BattleState (нет разрозненных bool + card).
/// — _attackBonus сбрасывается в Reset(), а не разбросан по методам.
/// — PrepareBattle возвращает bool только для внутреннего использования; внешний
///   сигнал о пропуске боя идёт через событие OnBattleEscaped.
/// </summary>
public class BattleManager : MonoBehaviour
{
    // ── События ───────────────────────────────────────────────────────────
    public event Action OnBattleWon;
    public event Action OnBattleLost;
    public event Action OnBattleEscaped;   // бой пропущен без последствий

    [Header("Системы")]
    [SerializeField] private TeamSystem      teamSystem;
    [SerializeField] private BattleUIManager uiManager;

    // ── Состояние текущего боя ────────────────────────────────────────────
    private BattleCardData _card;
    private int            _lastDiceRoll;
    private int            _attackBonus;
    private bool           _resolved;

    public BattleUIManager GetUIManager() => uiManager;

    // ── Публичный API ─────────────────────────────────────────────────────

    /// <summary>
    /// Вызывается из CardManager ПОСЛЕ флипа карты.
    /// Возвращает false, если бой был пропущен через EscapeBattle.
    /// </summary>
    public bool PrepareBattle(BattleCardData card)
    {
        if (card == null)
        {
            Debug.LogError("[BattleManager] BattleCardData не передан!");
            return false;
        }

        // Проверяем дымовую завесу ДО инициализации боя
        if (HandManager.Instance != null && HandManager.Instance.ConsumeEscapeBattle())
        {
            Debug.Log("[BattleManager] EscapeBattle: бой пропущен дымовой завесой.");
            uiManager?.DisplayEscapeMessage(card.enemyName);
            OnBattleEscaped?.Invoke();
            return false;
        }

        ResetState(card);
        Debug.Log($"[BattleManager] Бой с '{card.enemyName}', требуемая атака: {card.requiredAttack}");
        return true;
    }

    /// <summary>Выполнить бросок кубика с заданным значением.</summary>
    public void ExecuteBattle(int diceValue)
    {
        if (_card == null) return;

        _lastDiceRoll = diceValue;
        int playerBase = teamSystem.GetTotalAttack() + _attackBonus;
        uiManager.DisplayDiceRoll(diceValue, playerBase, _card.requiredAttack);
    }

    /// <summary>Переброс кубика (карта Руки — Reroll).</summary>
    public void RequestNewRoll()
    {
        int newDice = UnityEngine.Random.Range(1, 7);
        Debug.Log($"[BattleManager] Переброс → {newDice}");
        ExecuteBattle(newDice);
    }

    /// <summary>
    /// Добавить временный бонус к атаке на текущий бой (карта Руки — AddBonus).
    /// Сбрасывается после завершения боя.
    /// </summary>
    public void AddAttackBonus(int value)
    {
        _attackBonus += value;
        Debug.Log($"[BattleManager] AttackBonus: +{value} (итого: {_attackBonus}).");

        // Обновляем отображение, если кубик уже был брошен
        if (_lastDiceRoll > 0)
            ExecuteBattle(_lastDiceRoll);
    }

    /// <summary>
    /// Мгновенно завершить бой без штрафов и наград (дымовая завеса во время боя).
    /// </summary>
    public void ForceEndBattle(bool escaped)
    {
        if (_resolved) return;

        _resolved = true;

        if (escaped)
        {
            Debug.Log("[BattleManager] ForceEndBattle: сбежали без последствий.");
            uiManager?.DisplayEscapeMessage(_card?.enemyName ?? "врага");
            OnBattleEscaped?.Invoke();
        }

        ResetState(null);
        GameManager.Instance?.OnBattleForceEnded();
    }

    /// <summary>
    /// Финализировать бой — определить победителя и начислить награду/штраф.
    /// Вызывается ровно один раз за бой.
    /// </summary>
    public void FinalizeBattle()
    {
        if (_card == null)
        {
            Debug.LogWarning("[BattleManager] FinalizeBattle вызван без активного боя.");
            return;
        }

        if (_resolved)
        {
            Debug.LogWarning("[BattleManager] FinalizeBattle вызван повторно — проигнорировано.");
            return;
        }

        _resolved = true;

        int  playerTotal = teamSystem.GetTotalAttack() + _attackBonus + _lastDiceRoll;
        bool isVictory   = playerTotal >= _card.requiredAttack;

        uiManager.DisplayBattleResult(isVictory, _card, playerTotal);

        if (isVictory) Win();
        else           Lose();
    }

    // ── Приватные ─────────────────────────────────────────────────────────

    private void Win()
    {
        teamSystem.AddMoney(_card.rewardMoney);
        HandManager.Instance?.GiveRandomReward();
        OnBattleWon?.Invoke();
        Debug.Log($"[BattleManager] Победа! +{_card.rewardMoney} фелсов.");

        ResetState(null);
    }

    private void Lose()
    {
        teamSystem.RemoveMoney(Mathf.Abs(_card.penaltyMoney));
        OnBattleLost?.Invoke();
        Debug.Log($"[BattleManager] Поражение. -{Mathf.Abs(_card.penaltyMoney)} фелсов.");

        ResetState(null);
    }

    private void ResetState(BattleCardData newCard)
    {
        _card         = newCard;
        _lastDiceRoll = 0;
        _attackBonus  = 0;
        _resolved     = false;
    }
}
