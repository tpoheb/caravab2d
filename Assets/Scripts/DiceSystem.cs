using UnityEngine;
using System;

public class DiceSystem : MonoBehaviour
{
    [Header("Настройки кубика (D6)")]
    [SerializeField] private int minValue = 1;
    [SerializeField] private int maxValue = 6;

    public int LastRollResult { get; private set; }

    /// <summary>
    /// Сообщает результат броска и тип события
    /// </summary>
    public event Action<int, DiceEventType> OnDiceRolled;

    /// <summary>
    /// Бросок кубика (вызывается ТОЛЬКО GameManager)
    /// </summary>
    public void RollDice()
    {
        LastRollResult = UnityEngine.Random.Range(minValue, maxValue + 1);
        DiceEventType eventType = DetermineEventType(LastRollResult);

        Debug.Log($"DiceSystem: Бросок кубика — {LastRollResult}, событие — {eventType}");

        OnDiceRolled?.Invoke(LastRollResult, eventType);
    }

    private DiceEventType DetermineEventType(int result)
    {
        return result switch
        {
            <= 2 => DiceEventType.Battle,
            <= 4 => DiceEventType.ShadowInfluence,
            <= 6 => DiceEventType.PeacefulPass,
            _ => DiceEventType.None
        };
    }
}