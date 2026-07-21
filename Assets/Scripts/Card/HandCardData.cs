using UnityEngine;

/// <summary>
/// ScriptableObject с данными карты руки игрока.
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
    /// Для ChooseDice, DoubleGoods, IgnoreTax, EscapeBattle не используется.
    /// Для SalePriceBoost и PurchaseDiscount — целое число процентов (напр. 25, 20).
    /// </summary>
    public int value;

    // ─────────────────────────────────────────────────────────────────────

    public enum CardCategory
    {
        Tactical,   // Тактическая (бой, кубики)
        Logistic,   // Логистическая (путь, скорость)
        Economic,   // Экономическая (товары, деньги)
    }

    public enum CardEffectType
    {
        // ── Существующие ─────────────────────────────────────────────────
        Reroll,              // Переброс кубика
        AddBonus,            // Бонус к атаке команды (переиспользуется Крепким плечом)
        CapacityBoost,       // Временный бонус к грузоподъёмности
        GoldBoost,           // Бонус к выгоде от следующей торговли
        ChooseDice,          // Выбрать значение кубика пути вместо броска
        EscapeBattle,        // Избежать боя без штрафов (переиспользуется Притвориться бедным)
        CancelCard,          // Отменить следующую карту Тени или Битвы
        DoubleGoods,         // Удвоить количество одного товара в инвентаре

        // ── Новые торговые ───────────────────────────────────────────────
        SaleBonus,           // Слово Менялы:       +value монет к сумме продажи
        IgnoreTax,           // Второе дно каравана: игнорировать пошлину
        SalePriceBoost,      // Слух из первых уст: +value% к цене продажи
        PurchaseDiscount,    // Купец купцу друг:   -value% к цене покупки

        // ── Новые боевые ─────────────────────────────────────────────────
        EnemyAttackDebuff,   // Пыль в глаза:  -value к атаке противника
        BattlePenaltyReduce, // Клятва Пути:   -value% штрафа после поражения
    }
}