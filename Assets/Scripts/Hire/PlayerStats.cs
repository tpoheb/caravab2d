using System.Collections.Generic;
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

    public void ModifyAttack(int value) => _attack += value;
    public void ModifyBargain(int value) => _bargain += value;
    public void ModifyCapacity(int value) => _capacity += value;
}