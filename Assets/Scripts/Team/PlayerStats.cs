using UnityEngine;

[CreateAssetMenu(fileName = "New Player Stats", menuName = "Game/Player Stats")]
public class PlayerStats : ScriptableObject
{
    [Header("Основные характеристики")]
    [SerializeField, Range(1, 100)] private int _attack = 10;
    [SerializeField, Range(1, 20)] private int _bargain = 1; // Выгода
    [SerializeField, Range(10, 5000)] private int _capacity = 50; // Грузоподъемность

    public int Attack => _attack;
    public int Bargain => _bargain;
    public int Capacity => _capacity;

    // Методы для улучшения (можно вызывать из UI)
    public void UpgradeAttack(int value) => _attack += value;
    public void UpgradeBargain(int value) => _bargain = Mathf.Clamp(_bargain + value, 1, 100);
    public void UpgradeCapacity(int value) => _capacity = Mathf.Clamp(_capacity + value, 10, 500);
}