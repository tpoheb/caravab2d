using UnityEngine;
using System; // Добавлено для Action

public class DiceSystem : MonoBehaviour
{
    [Header("Настройки кубика")]
    // Используем стандартный кубик D6
    [SerializeField] private int minValue = 1; 
    [SerializeField] private int maxValue = 6; 

    // --- УДАЛЕНЫ УСТАРЕВШИЕ ПОЛЯ ---
    /*
    [SerializeField] private int[] moneyModifiers = new int[6];
    [SerializeField] private int[] attackModifiers = new int[6];
    public int LastMoneyModifier { get; private set; }
    public int LastAttackModifier { get; private set; }
    */

    public int LastRollResult { get; private set; }
    
    // --- НОВОЕ СОБЫТИЕ: Издает тип события ---
    public static event Action<DiceEventType> OnDiceEvent;

    // УДАЛЕНО: public event System.Action<int> OnDiceRolled; // Заменено на OnDiceEvent

    public void RollDice()
    {
        // Бросаем кубик (1d6)
        LastRollResult = UnityEngine.Random.Range(minValue, maxValue + 1);
        
        // Определяем тип события
        DiceEventType eventType = DetermineEventType(LastRollResult);

        // Издаем событие
        OnDiceEvent?.Invoke(eventType);

        // Логирование
        Debug.Log($"Результат броска: {LastRollResult}. Событие: {eventType}");
    }

    private DiceEventType DetermineEventType(int result)
    {
        if (result >= 1 && result <= 2)
        {
            return DiceEventType.Battle;
        }
        else if (result >= 3 && result <= 4)
        {
            return DiceEventType.ShadowInfluence;
        }
        else if (result >= 5 && result <= 6)
        {
            return DiceEventType.PeacefulPass;
        }
        else
        {
            // На всякий случай
            Debug.LogError($"Некорректный результат кубика: {result}");
            return DiceEventType.None;
        }
    }
}