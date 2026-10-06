using UnityEngine;
using System;

/// <summary>Подготовка, изменение броска, результат боя и расчёт после посткомбата.</summary>
public class BattleManager : MonoBehaviour
{
    public event Action OnBattleWon;
    public event Action OnBattleLost;
    public event Action OnBattleEscaped;

    [Header("Системы")]
    [SerializeField] private TeamSystem teamSystem;
    [SerializeField] private BattleUIManager uiManager;

    private BattleCardData _card;
    private int _lastDiceRoll;
    private int _attackBonus;
    private int _enemyDebuff;
    private bool _resolved;
    private bool _victory;

    public bool HasDiceRoll => _card != null && _lastDiceRoll > 0;
    public bool IsDefeat => _card != null && _resolved && !_victory;
    public bool IsActive => _card != null && !_resolved;
    public BattleUIManager GetUIManager() => uiManager;

    /// <summary>Инициализация до розыгрыша карт подготовки, без броска и расчёта.</summary>
    public bool PrepareBattle(BattleCardData card)
    {
        ResetState(card);
        if (card == null) return false;
        Debug.Log($"[BattleManager] Подготовка боя с '{card.enemyName}', атака: {card.requiredAttack}");
        return true;
    }

    public void ExecuteBattle(int diceValue)
    {
        if (!IsActive) return;
        _lastDiceRoll = Mathf.Clamp(diceValue, 1, 6);
        RefreshPreview();
    }

    public void RequestNewRoll()
    {
        if (!IsActive || !HasDiceRoll) return;
        ExecuteBattle(UnityEngine.Random.Range(1, 7));
    }

    public void AddAttackBonus(int value)
    {
        if (!IsActive) return;
        _attackBonus += value;
        RefreshPreview();
    }

    public void AddEnemyAttackDebuff(int value)
    {
        if (!IsActive) return;
        _enemyDebuff += Mathf.Max(0, value);
        RefreshPreview();
    }

    private int EnemyAttack => Mathf.Max(0, _card.requiredAttack - _enemyDebuff);
    private int PlayerBaseAttack => teamSystem.GetTotalAttack() + _attackBonus;

    private void RefreshPreview()
    {
        if (HasDiceRoll)
            uiManager?.DisplayDiceRoll(_lastDiceRoll, PlayerBaseAttack, EnemyAttack);
    }

    public void ForceEndBattle(bool escaped)
    {
        if (!IsActive) return;
        string enemyName = _card.enemyName;
        ResetState(null);
        if (escaped)
        {
            uiManager?.DisplayEscapeMessage(enemyName);
            OnBattleEscaped?.Invoke();
        }
        GameManager.Instance?.OnBattleForceEnded();
    }

    public void CancelBattle() => ResetState(null);

    public void RefreshPostCombatPreview()
    {
        if (!IsDefeat) return;
        float reduction = HandManager.Instance?.PendingPenaltyReduction ?? 0f;
        int penalty = Mathf.RoundToInt(Mathf.Abs(_card.penaltyMoney) * (1f - reduction));
        uiManager?.DisplaySettledPenalty(penalty);
    }

    /// <summary>Фиксируем исход. Штраф ждёт посткомбат; победная награда доступна сразу.</summary>
    public void FinalizeBattle()
    {
        if (!IsActive || !HasDiceRoll) return;
        _resolved = true;
        int totalAttack = PlayerBaseAttack + _lastDiceRoll;
        _victory = totalAttack >= EnemyAttack;
        uiManager?.DisplayBattleResult(_victory, _card, totalAttack);
        if (_victory)
        {
            teamSystem.AddMoney(_card.rewardMoney);
            HandManager.Instance?.GiveRandomReward();
            OnBattleWon?.Invoke();
            Debug.Log($"[BattleManager] Победа! +{_card.rewardMoney} фелсов.");
        }
        else
        {
            OnBattleLost?.Invoke();
            Debug.Log("[BattleManager] Поражение. Штраф будет начислен после посткомбата.");
        }
    }

    /// <summary>Вызывается один раз после окна «после битвы», перед завершением хода.</summary>
    public void SettleBattle()
    {
        if (_card == null || !_resolved) return;
        if (!_victory)
        {
            float reduction = Mathf.Clamp01(HandManager.Instance?.ConsumePenaltyReduction() ?? 0f);
            int penalty = Mathf.RoundToInt(Mathf.Abs(_card.penaltyMoney) * (1f - reduction));
            teamSystem.RemoveMoney(penalty);
            uiManager?.DisplaySettledPenalty(penalty);
            Debug.Log($"[BattleManager] Штраф после посткомбата: {penalty} фелсов.");
        }
        ResetState(null);
    }

    private void ResetState(BattleCardData card)
    {
        _card = card;
        _lastDiceRoll = 0;
        _attackBonus = 0;
        _enemyDebuff = 0;
        _resolved = false;
        _victory = false;
    }
}
