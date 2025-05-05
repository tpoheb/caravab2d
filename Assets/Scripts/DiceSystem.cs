using UnityEngine;

public class DiceSystem : MonoBehaviour
{
    [Header("��������� ������")]
    [SerializeField] private int minValue = 1;
    [SerializeField] private int maxValue = 6;

    [Header("������/������")]
    [SerializeField] private int[] moneyModifiers = new int[6]; // ������ � ������� ��� ������� ��������
    [SerializeField] private int[] attackModifiers = new int[6]; // ������ � �����

    public int LastRollResult { get; private set; }
    public int LastMoneyModifier { get; private set; }
    public int LastAttackModifier { get; private set; }

    public event System.Action<int> OnDiceRolled;

    public void RollDice()
    {
        // ��������� ���������� ����������
        LastRollResult = Random.Range(minValue, maxValue + 1);

        // ����������� �������������
        LastMoneyModifier = moneyModifiers[LastRollResult - 1];
        LastAttackModifier = attackModifiers[LastRollResult - 1];

        // �����������
        Debug.Log($"������ ������: {LastRollResult}\n" +
                 $"����������� �����: {LastMoneyModifier}\n" +
                 $"����������� �����: {LastAttackModifier}");

        OnDiceRolled?.Invoke(LastRollResult);
    }
}