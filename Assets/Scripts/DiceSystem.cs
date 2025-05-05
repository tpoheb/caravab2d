using UnityEngine;

public class DiceSystem : MonoBehaviour
{
    [Header("Настройки кубика")]
    [SerializeField] private int minValue = 1;
    [SerializeField] private int maxValue = 6;

    [Header("Бонусы/штрафы")]
    [SerializeField] private int[] moneyModifiers = new int[6]; // Бонусы к деньгам для каждого значения
    [SerializeField] private int[] attackModifiers = new int[6]; // Бонусы к атаке

    public int LastRollResult { get; private set; }
    public int LastMoneyModifier { get; private set; }
    public int LastAttackModifier { get; private set; }

    public event System.Action<int> OnDiceRolled;

    public void RollDice()
    {
        // Генерация случайного результата
        LastRollResult = Random.Range(minValue, maxValue + 1);

        // Определение модификаторов
        LastMoneyModifier = moneyModifiers[LastRollResult - 1];
        LastAttackModifier = attackModifiers[LastRollResult - 1];

        // Логирование
        Debug.Log($"Бросок кубика: {LastRollResult}\n" +
                 $"Модификатор денег: {LastMoneyModifier}\n" +
                 $"Модификатор атаки: {LastAttackModifier}");

        OnDiceRolled?.Invoke(LastRollResult);
    }
}