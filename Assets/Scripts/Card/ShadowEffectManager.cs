using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Применяет и откатывает эффекты карт Тени.
/// ProcessTurn() вызывается из GameManager каждый EndTurn.
/// OnEnterCity() и ConsumeBonusTrade() — из CityManager.
/// </summary>
public class ShadowEffectManager : MonoBehaviour
{
    [Header("Зависимости")]
    [SerializeField] private PlayerStats      playerStats;
    [SerializeField] private PlayerInventory  playerInventory;
    [SerializeField] private TeamSystem       teamSystem;

    // ── Стек временных эффектов ──────────────────────────────────────────
    private readonly List<ActiveEffect> _activeEffects = new List<ActiveEffect>();

    // ── Отложенные флаги (применяются при входе в город) ────────────────
    private bool  _wagePenaltyActive  = false;
    private float _bonusTradePercent  = 0f;

    // ─────────────────────────────────────────────────────────────────────
    // Публичный API
    // ─────────────────────────────────────────────────────────────────────

    /// <summary>Применить эффект карты Тени.</summary>
    public void ApplyCard(ShadowCardData card)
    {
        if (card == null) return;

        switch (card.effectType)
        {
            case ShadowEffectType.Money:
                ApplyMoney(card.value);
                break;

            case ShadowEffectType.AddGoods:
                playerInventory?.AddRandomGoods(card.value);
                Debug.Log($"[ShadowEffectManager] AddGoods: +{card.value} товаров.");
                break;

            case ShadowEffectType.RemoveGoods:
                playerInventory?.RemoveRandomGoods(Mathf.Abs(card.value));
                Debug.Log($"[ShadowEffectManager] RemoveGoods: -{Mathf.Abs(card.value)} товаров.");
                break;

            case ShadowEffectType.FireCrewMember:
                ApplyFireCrewMember();
                break;

            case ShadowEffectType.WagePenalty:
                _wagePenaltyActive = true;
                Debug.Log("[ShadowEffectManager] WagePenalty: двойное жалованье в следующем городе.");
                break;

            case ShadowEffectType.Confiscation:
                ApplyConfiscation(card.penaltyValue);
                break;

            case ShadowEffectType.BonusTrade:
                _bonusTradePercent += card.value;
                Debug.Log($"[ShadowEffectManager] BonusTrade: +{card.value}% к выгоде в следующем городе.");
                break;

            // Временные эффекты идут в стек
            case ShadowEffectType.Attack:
            case ShadowEffectType.Capacity:
            case ShadowEffectType.Bargain:
            case ShadowEffectType.TeamStats:
                ApplyTemporary(card);
                break;

            default:
                Debug.LogWarning($"[ShadowEffectManager] Неизвестный тип эффекта: {card.effectType}");
                break;
        }
    }

    /// <summary>
    /// Применить карту Тени через обёртку ShadowCardData, созданную на лету
    /// (используется HandManager для CapacityBoost и аналогичных карт руки).
    /// </summary>
    public void ApplyTransientCard(ShadowEffectType type, int value, int duration = 1)
    {
        var card = ScriptableObject.CreateInstance<ShadowCardData>();
        card.cardName   = $"[Transient] {type}";
        card.effectType = type;
        card.value      = value;
        card.isTemporary = duration > 0;
        card.duration   = duration;
        card.hideFlags  = HideFlags.DontSave;

        ApplyCard(card);
    }

    /// <summary>
    /// Тик в конце каждого хода — уменьшает счётчики и откатывает истёкшие эффекты.
    /// </summary>
    public void ProcessTurn()
    {
        for (int i = _activeEffects.Count - 1; i >= 0; i--)
        {
            _activeEffects[i].RemainingTurns--;
            if (_activeEffects[i].RemainingTurns <= 0)
            {
                RevertEffect(_activeEffects[i].Data);
                Debug.Log($"[ShadowEffectManager] Эффект '{_activeEffects[i].Data.cardName}' истёк.");
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
        if (!_wagePenaltyActive) return 1f;

        _wagePenaltyActive = false;
        Debug.Log("[ShadowEffectManager] WagePenalty применён: двойное жалованье.");
        return 2f;
    }

    /// <summary>
    /// Вызывается из CityManager при расчёте торговой прибыли.
    /// Возвращает накопленный % бонус и сбрасывает его.
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
        if (playerInventory == null) return;

        if (amount >= 0) playerInventory.AddMoney(amount);
        else             playerInventory.TrySpendMoney(Mathf.Abs(amount));

        Debug.Log($"[ShadowEffectManager] Money: {amount:+0;-0} динаров.");
    }

    private void ApplyFireCrewMember()
    {
        if (teamSystem == null)
        {
            Debug.LogWarning("[ShadowEffectManager] TeamSystem не назначен — FireCrewMember пропущен.");
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

        if (playerInventory.ConfiscateContraband())
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
        _activeEffects.Add(new ActiveEffect(card));
        ModifyStats(card.effectType, card.value);
        Debug.Log($"[ShadowEffectManager] '{card.cardName}' применён на {card.duration} ходов.");
    }

    private void ModifyStats(ShadowEffectType type, int value)
    {
        if (playerStats == null) return;

        switch (type)
        {
            case ShadowEffectType.Attack:    playerStats.ModifyAttack(value);                  break;
            case ShadowEffectType.Capacity:  playerStats.ModifyCapacity(value);                break;
            case ShadowEffectType.Bargain:   playerStats.ModifyBargain(value);                 break;
            case ShadowEffectType.TeamStats: playerStats.ApplyTeamStatsMultiplier(value);      break;
        }
    }

    private void RevertEffect(ShadowCardData card)
    {
        if (card.effectType == ShadowEffectType.TeamStats)
            playerStats?.RevertTeamStatsMultiplier(card.value);
        else
            ModifyStats(card.effectType, -card.value);
    }

    // ─────────────────────────────────────────────────────────────────────

    private sealed class ActiveEffect
    {
        public ShadowCardData Data           { get; }
        public int            RemainingTurns { get; set; }

        public ActiveEffect(ShadowCardData data)
        {
            Data           = data;
            RemainingTurns = data.duration;
        }
    }
}
