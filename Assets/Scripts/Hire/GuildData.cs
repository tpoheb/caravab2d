using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// ScriptableObject с данными гильдии.
/// Содержит имя, описание, вступительный взнос и список товаров со скидкой (будет проработан позже).
/// </summary>
[CreateAssetMenu(fileName = "New Guild", menuName = "Game/Guild Data")]
public class GuildData : ScriptableObject
{
    [Header("Основное")]
    public string guildName;

    [TextArea(3, 10)]
    public string description;

    public Sprite icon;

    [Header("Вступительный взнос")]
    [Tooltip("Сколько нужно заплатить при вступлении в гильдию")]
    public int entryFee = 500;

    [Header("Родная провинция (лор)")]
    public string homeProvince;

    [Header("Скидки на товары (будет проработано позже)")]
    [Tooltip("Товары, на которые гильдия даёт скидку при покупке")]
    public List<Item> discountedItems = new List<Item>();

    [Header("Процент скидки (будет проработано позже)")]
    [Range(0f, 0.5f)]
    [Tooltip("Процент скидки на discountedItems (0.1 = 10%)")]
    public float discountPercent = 0.1f;
}