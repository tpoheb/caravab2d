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

    [Header("Момент розыгрыша")]
    [Tooltip("Можно указать несколько фаз. None запрещает розыгрыш карты.")]
    public PlayTiming playTiming;

    [System.Flags]
    public enum PlayTiming
    {
        None = 0,
        [InspectorName("Перед битвой")] BeforeBattle = 1,
        [InspectorName("Во время битвы")] DuringBattle = 2,
        [InspectorName("После битвы")] AfterBattle = 4,
        [InspectorName("Перед событием")] BeforeEvent = 8,
        [InspectorName("Во время события")] DuringEvent = 16,
        [InspectorName("После события")] AfterEvent = 32,
        [InspectorName("В городе")] InCity = 64,
    }

    public string TimingLabel
    {
        get
        {
            var labels = new System.Collections.Generic.List<string>();
            if ((playTiming & PlayTiming.BeforeBattle) != 0) labels.Add("перед битвой");
            if ((playTiming & PlayTiming.DuringBattle) != 0) labels.Add("во время битвы");
            if ((playTiming & PlayTiming.AfterBattle) != 0) labels.Add("после битвы");
            if ((playTiming & PlayTiming.BeforeEvent) != 0) labels.Add("перед событием");
            if ((playTiming & PlayTiming.DuringEvent) != 0) labels.Add("во время события");
            if ((playTiming & PlayTiming.AfterEvent) != 0) labels.Add("после события");
            if ((playTiming & PlayTiming.InCity) != 0) labels.Add("в городе");
            return string.Join(", ", labels);
        }
    }

    /// <summary>Начальные метки каталога импортёра; ассеты можно настроить отдельно.</summary>
    public static PlayTiming GetDefaultTiming(CardEffectType effect) => effect switch
    {
        CardEffectType.Reroll => PlayTiming.DuringBattle,
        CardEffectType.ChooseDice or CardEffectType.AddBonus or
        CardEffectType.EnemyAttackDebuff or CardEffectType.EscapeBattle
            => PlayTiming.BeforeBattle | PlayTiming.DuringBattle,
        CardEffectType.BattlePenaltyReduce => PlayTiming.AfterBattle,
        CardEffectType.CancelCard => PlayTiming.BeforeBattle | PlayTiming.BeforeEvent | PlayTiming.DuringEvent,
        CardEffectType.CapacityBoost or CardEffectType.DoubleGoods
            => PlayTiming.InCity | PlayTiming.BeforeBattle | PlayTiming.BeforeEvent |
               PlayTiming.DuringEvent | PlayTiming.AfterBattle | PlayTiming.AfterEvent,
        _ => PlayTiming.InCity,
    };

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
        ChooseDice,          // Выбрать значение боевого кубика
        EscapeBattle,        // Избежать боя без штрафов (переиспользуется Притвориться бедным)
        CancelCard,          // Отменить раскрытую карту до её разрешения
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
