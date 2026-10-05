using UnityEngine;
using System;

/// <summary>Кубик прибавки к атаке в битве. Выбор карт выполняет общая колода.</summary>
public class DiceSystem : MonoBehaviour
{
    [Header("Настройки кубика (D6)")]
    [SerializeField] private int minValue = 1;
    [SerializeField] private int maxValue = 6;

    public int LastRollResult { get; private set; }
    public event Action<int> OnDiceRolled;

    public void RollDice()
    {
        LastRollResult = UnityEngine.Random.Range(minValue, maxValue + 1);
        Debug.Log($"DiceSystem: Бросок кубика атаки — {LastRollResult}");
        OnDiceRolled?.Invoke(LastRollResult);
    }
}