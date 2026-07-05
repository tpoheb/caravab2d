using UnityEngine;

/// <summary>
/// ScriptableObject с данными карты руки игрока.
///
/// Изменения: CardEffectType вынесен в отдельный файл HandCardEffectType,
/// чтобы не конфликтовать с BattleCardEffect.cs (который теперь удалён —
/// его содержимое было дублем).
/// </summary>
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
    /// Для ChooseDice и DoubleGoods не используется.
    /// </summary>
    public int value;

    // ──────────────────────────────────────────────────────────────────────

    public enum CardCategory
    {
        Tactical,   // Тактическая (бой, кубики)
        Logistic,   // Логистическая (путь, скорость)
        Economic,   // Экономическая (товары, деньги)
    }

    /// <summary>
    /// Типы эффектов карт руки.
    /// BattleCardEffect.cs (старый дубликат) — удалить из проекта.
    /// </summary>
    public enum CardEffectType
    {
        Reroll,         // Переброс любого кубика
        AddBonus,       // Бонус к атаке в текущем бою
        CapacityBoost,  // Временный бонус к грузоподъёмности
        GoldBoost,      // Бонус к выгоде от следующей торговли
        ChooseDice,     // Выбрать значение кубика пути (1–6) вместо броска
        EscapeBattle,   // Завершить бой без штрафов и наград
        CancelCard,     // Отменить следующую карту Тени или Битвы
        DoubleGoods,    // Удвоить количество одного типа товара в инвентаре
    }
}