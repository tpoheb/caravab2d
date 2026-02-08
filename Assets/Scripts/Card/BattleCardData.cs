using UnityEngine;

/// <summary>
/// Scriptable Object для хранения данных о картах битвы (врагах).
/// </summary>
[CreateAssetMenu(fileName = "NewBattleCard", menuName = "Game Data/Battle Card")]
public class BattleCardData : ScriptableObject
{
    // --- Идентификация и Описание ---
    [Header("Идентификация")]
    [Tooltip("Уникальный ID для программного доступа.")]
    public int cardID;
    [Tooltip("Название врага или события.")]
    public string enemyName = "Речные пираты";
    
    // --- Игровые Характеристики ---
    [Header("Боевые Характеристики")]
    [Tooltip("Требуемая атака для победы.")]
    public int requiredAttack = 3;
    
    // --- Эффекты от Битвы ---
    [Header("Награды/Штрафы")]
    [Tooltip("Изменение денег в случае ПОБЕДЫ.")]
    public int rewardMoney = 60; // +60 фелсов
    [Tooltip("Изменение денег в случае ПОРАЖЕНИЯ.")]
    public int penaltyMoney = -50; // -50 фелсов
    
    // В будущем здесь можно добавить ссылки на изображения, другие ресурсы,
    // или более сложные модификаторы (опыт, мораль и т.д.).
}