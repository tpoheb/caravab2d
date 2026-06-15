using System.Collections.Generic;
using UnityEngine;
using System;

[CreateAssetMenu(fileName = "New Player Stats", menuName = "Game/Player Stats")]
public class PlayerStats : ScriptableObject
{
    [Header("Базовые характеристики (Начальные)")]
    [SerializeField, Range(1, 100)]   private int _baseAttack   = 10;
    [SerializeField, Range(-20, 20)]  private int _baseBargain  = 1;
    [SerializeField, Range(10, 5000)] private int _baseCapacity = 50;

    [NonSerialized] private int _currentAttack;
    [NonSerialized] private int _currentBargain;
    [NonSerialized] private int _currentCapacity;

    // Накопленный множитель команды (100 = норма, 50 = лихорадка -50%)
    // Стек нужен, потому что несколько эффектов TeamStats могут висеть одновременно
    [NonSerialized] private List<int> _teamMultiplierStack = new List<int>();

    public int Attack   => ApplyMultipliers(_currentAttack);
    public int Bargain  => ApplyMultipliers(_currentBargain);
    public int Capacity => _currentCapacity; // грузоподъёмность не масштабируется командой

    public event Action OnStatsChanged;

    // ──────────────────────────────────────────────────────────────────────

    public void Initialize()
    {
        _currentAttack   = _baseAttack;
        _currentBargain  = _baseBargain;
        _currentCapacity = _baseCapacity;
        _teamMultiplierStack?.Clear();
        _teamMultiplierStack = new List<int>();

        OnStatsChanged?.Invoke();
        Debug.Log("[PlayerStats] Характеристики сброшены к начальным значениям.");
    }

    // ── Прямые изменения ──────────────────────────────────────────────────

    public void ModifyAttack(int value)
    {
        _currentAttack += value;
        OnStatsChanged?.Invoke();
    }

    public void ModifyBargain(int value)
    {
        _currentBargain += value;
        OnStatsChanged?.Invoke();
    }

    public void ModifyCapacity(int value)
    {
        _currentCapacity += value;
        OnStatsChanged?.Invoke();
    }

    // ── Командный множитель (Лихорадка и подобные) ────────────────────────

    /// <summary>
    /// Применяет множитель к боевым характеристикам команды.
    /// percent = -50 означает «×0.5»; +20 — «×1.2».
    /// Вызывается ShadowEffectManager при применении карты TeamStats.
    /// </summary>
    public void ApplyTeamStatsMultiplier(int percent)
    {
        _teamMultiplierStack.Add(percent);
        OnStatsChanged?.Invoke();
        Debug.Log($"[PlayerStats] TeamMultiplier добавлен: {percent:+0;-0}%. " +
                  $"Стек: [{string.Join(", ", _teamMultiplierStack)}]");
    }

    /// <summary>
    /// Убирает один экземпляр множителя из стека (откат по истечении эффекта).
    /// Вызывается ShadowEffectManager в ProcessTurn когда duration → 0.
    /// </summary>
    public void RevertTeamStatsMultiplier(int percent)
    {
        if (_teamMultiplierStack.Remove(percent))
        {
            OnStatsChanged?.Invoke();
            Debug.Log($"[PlayerStats] TeamMultiplier снят: {percent:+0;-0}%. " +
                      $"Стек: [{string.Join(", ", _teamMultiplierStack)}]");
        }
        else
        {
            Debug.LogWarning($"[PlayerStats] RevertTeamStatsMultiplier: значение {percent} не найдено в стеке.");
        }
    }

    // ── Приватные ────────────────────────────────────────────────────────

    /// <summary>
    /// Перемножает все активные множители из стека.
    /// Порядок не важен — итог одинаков.
    /// </summary>
    private int ApplyMultipliers(int baseValue)
    {
        if (_teamMultiplierStack == null || _teamMultiplierStack.Count == 0)
            return baseValue;

        float result = baseValue;
        foreach (int pct in _teamMultiplierStack)
            result *= (100f + pct) / 100f;

        return Mathf.RoundToInt(result);
    }
}