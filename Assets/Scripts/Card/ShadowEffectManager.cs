using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Применяет и откатывает эффекты карт Тени.
/// Вызывать ProcessTurn() из GameManager каждый EndTurn.
/// </summary>
public class ShadowEffectManager : MonoBehaviour
{
    [Header("Зависимости")]
    [SerializeField] private PlayerStats playerStats;
    [SerializeField] private PlayerInventory playerInventory;
    [SerializeField] private TeamSystem teamSystem; // нужен для FireCrewMember

    private List<ActiveEffect> _activeEffects = new List<ActiveEffect>();

    // Флаги отложенных эффектов (применяются при входе в город)
    private bool  _wagePenaltyActive = false;
    private float _bonusTradePercent = 0f;

    // ─────────────────────────────────────────────────────────────────────
    // Public API
    // ─────────────────────────────────────────────────────────────────────

    public void ApplyCard(ShadowCardData card)
    {
        switch (card.effectType)
        {
            case ShadowEffectType.Money:
                ApplyMoney(card.value);
                return;

            case ShadowEffectType.AddGoods:
                playerInventory?.AddRandomGoods(card.value);
                return;

            case ShadowEffectType.RemoveGoods:
                playerInventory?.RemoveRandomGoods(Mathf.Abs(card.value));
                return;

            case ShadowEffectType.FireCrewMember:
                ApplyFireCrewMember();
                return;

            case ShadowEffectType.WagePenalty:
                _wagePenaltyActive = true;
                Debug.Log("[ShadowEffectManager] WagePenalty: двойное жалованье в следующем городе.");
                return;

            case ShadowEffectType.Confiscation:
                ApplyConfiscation(card.penaltyValue);
                return;

            case ShadowEffectType.BonusTrade:
                _bonusTradePercent += card.value;
                Debug.Log($"[ShadowEffectManager] BonusTrade: +{card.value}% к выгоде в следующем городе.");
                return;

            // Временные эффекты — идут в стек
            case ShadowEffectType.Attack:
            case ShadowEffectType.Capacity:
            case ShadowEffectType.Bargain:
            case ShadowEffectType.TeamStats:
                ApplyTemporary(card);
                return;
        }
    }

    /// <summary>
    /// Вызывается из GameManager при завершении каждого хода.
    /// </summary>
    public void ProcessTurn()
    {
        for (int i = _activeEffects.Count - 1; i >= 0; i--)
        {
            _activeEffects[i].remainingTurns--;
            if (_activeEffects[i].remainingTurns <= 0)
            {
                RevertEffect(_activeEffects[i].data);
                Debug.Log($"[ShadowEffectManager] Эффект '{_activeEffects[i].data.cardName}' истёк.");
                _activeEffects.RemoveAt(i);
            }
        }
    }

    /// <summary>
    /// Вызывается из CityManager при входе в город.
    /// Возвращает множитель жалованья: 1.0 = норма, 2.0 = двойное.
    /// </summary>
    public float OnEnterCity()
    {
        float wageMultiplier = 1f;

        if (_wagePenaltyActive)
        {
            wageMultiplier = 2f;
            _wagePenaltyActive = false;
            Debug.Log("[ShadowEffectManager] WagePenalty применён: двойное жалованье.");
        }

        return wageMultiplier;
    }

    /// <summary>
    /// Вызывается из CityManager при расчёте торговой прибыли.
    /// Возвращает процентный бонус и сбрасывает его.
    /// </summary>
    public float ConsumeBonusTrade()
    {
        float bonus = _bonusTradePercent;
        _bonusTradePercent = 0f;
        if (bonus > 0f)
            Debug.Log($"[ShadowEffectManager] BonusTrade применён: +{bonus}%.");
        return bonus;
    }

    // ─────────────────────────────────────────────────────────────────────
    // Приватные методы
    // ─────────────────────────────────────────────────────────────────────

    private void ApplyMoney(int amount)
    {
        if (amount >= 0)
            playerInventory.AddMoney(amount);
        else
            playerInventory.TrySpendMoney(Mathf.Abs(amount));

        Debug.Log($"[ShadowEffectManager] Money: {amount:+0;-0} динаров.");
    }

    private void ApplyFireCrewMember()
    {
        if (teamSystem == null)
        {
            Debug.LogWarning("[ShadowEffectManager] TeamSystem не назначен — FireCrewMember не выполнен.");
            return;
        }

        bool fired = teamSystem.FireRandomCrewMember();
        Debug.Log(fired
            ? "[ShadowEffectManager] FireCrew: случайный член команды уволен."
            : "[ShadowEffectManager] FireCrew: команда уже пуста.");
    }

    private void ApplyConfiscation(int penalty)
    {
        if (playerInventory == null) return;

        bool hadContraband = playerInventory.ConfiscateContraband();
        if (hadContraband)
        {
            ApplyMoney(-penalty);
            Debug.Log($"[ShadowEffectManager] Confiscation: контрабанда изъята, штраф {penalty} дин.");
        }
        else
        {
            Debug.Log("[ShadowEffectManager] Confiscation: контрабанды не нашли.");
        }
    }

    private void ApplyTemporary(ShadowCardData card)
    {
        var effect = new ActiveEffect { data = card, remainingTurns = card.duration };
        _activeEffects.Add(effect);
        ApplyStatsDelta(card.effectType, card.value);
        Debug.Log($"[ShadowEffectManager] '{card.cardName}' применён на {card.duration} ходов.");
    }

    private void ApplyStatsDelta(ShadowEffectType type, int value)
    {
        if (playerStats == null) return;

        switch (type)
        {
            case ShadowEffectType.Attack:
                playerStats.ModifyAttack(value);
                break;
            case ShadowEffectType.Capacity:
                playerStats.ModifyCapacity(value);
                break;
            case ShadowEffectType.Bargain:
                playerStats.ModifyBargain(value);
                break;
            case ShadowEffectType.TeamStats:
                playerStats.ApplyTeamStatsMultiplier(value);
                break;
        }
    }

    private void RevertEffect(ShadowCardData card)
    {
        if (card.effectType == ShadowEffectType.TeamStats)
            playerStats.RevertTeamStatsMultiplier(card.value);
        else
            ApplyStatsDelta(card.effectType, -card.value);
    }

    // ─────────────────────────────────────────────────────────────────────

    [System.Serializable]
    public class ActiveEffect
    {
        public ShadowCardData data;
        public int remainingTurns;
    }
}