using UnityEngine;

[CreateAssetMenu(fileName = "NewHandCard", menuName = "Game/Cards/HandCard")]
public class HandCardData : ScriptableObject
{
    [Header("Основное")]
    public string cardName;

    [TextArea]
    public string description;

    public Sprite icon;

    [Header("Категория")]
    public CardCategory category;

    [Header("Эффект")]
    public CardEffectType effectType;

    /// <summary>
    /// Числовое значение эффекта.
    /// Для ChooseDice: не используется (игрок выбирает сам).
    /// Для DoubleGoods: не используется (игрок выбирает товар).
    /// </summary>
    public int value;

    public enum CardCategory
    {
        Tactical,   // Тактическая (бой, кубики)
        Logistic,   // Логистическая (путь, скорость)
        Economic,   // Экономическая (товары, деньги)
    }

    public enum CardEffectType
    {
        // ── Существующие ─────────────────────────────────────
        Reroll,         // Переброс любого кубика (пути или битвы)
        AddBonus,       // Бонус к атаке (существует в коде, но не подключён)
        CapacityBoost,  // Бонус к грузоподъёмности
        GoldBoost,      // Бонус к выгоде от торговли

        // ── Новые ────────────────────────────────────────────
        ChooseDice,     // Выбрать любое значение кубика пути (1-6) вместо броска
        EscapeBattle,   // Мгновенно завершить бой без штрафов и наград
        CancelCard,     // Отменить действие вытянутой карты Тени или Битвы
        DoubleGoods,    // Удвоить количество одного типа товара в инвентаре
    }
}