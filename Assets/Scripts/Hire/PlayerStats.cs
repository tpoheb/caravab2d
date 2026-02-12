using System.Collections.Generic;
using UnityEngine;
using System;

[CreateAssetMenu(fileName = "New Player Stats", menuName = "Game/Player Stats")]
public class PlayerStats : ScriptableObject
{
    [Header("Базовые характеристики (Начальные)")]
    [SerializeField, Range(1, 100)] private int _baseAttack = 10;
    [SerializeField, Range(-20, 20)] private int _baseBargain = 1; 
    [SerializeField, Range(10, 5000)] private int _baseCapacity = 50; 

    // Текущие значения, которые будут меняться в игре
    // System.NonSerialized гарантирует, что изменения не запишутся в файл ассета
    [NonSerialized] private int _currentAttack;
    [NonSerialized] private int _currentBargain;
    [NonSerialized] private int _currentCapacity;

    // Публичные свойства для чтения (теперь читают текущие значения)
    public int Attack => _currentAttack;
    public int Bargain => _currentBargain;
    public int Capacity => _currentCapacity;

    // Событие, на которое подпишется TopBarUI
    public event Action OnStatsChanged;

    /// <summary>
    /// Сбрасывает текущие статы к базовым. 
    /// Вызывать в GameManager.cs в методе Start()
    /// </summary>
    public void Initialize()
    {
        _currentAttack = _baseAttack;
        _currentBargain = _baseBargain;
        _currentCapacity = _baseCapacity;
        
        OnStatsChanged?.Invoke();
        Debug.Log("[PlayerStats] Характеристики сброшены к начальным значениям.");
    }

    // Методы изменения теперь меняют текущие значения и уведомляют UI
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
}