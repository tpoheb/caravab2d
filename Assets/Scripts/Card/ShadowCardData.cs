using UnityEngine;

public enum ShadowEffectType
{
    // ── Существующие ──────────────────────────────────────────
    Money,       // Изменение динаров (мгновенно)
    Attack,      // Изменение атаки (временно)
    Capacity,    // Изменение грузоподъёмности (временно)
    Bargain,     // Изменение торговли (временно)

    // ── Новые ─────────────────────────────────────────────────
    AddGoods,        // Добавить N ед. случайного товара в инвентарь
    RemoveGoods,     // Удалить N ед. случайных товаров из инвентаря
    FireCrewMember,  // Уволить случайного члена команды
    WagePenalty,     // Следующий визит в город: двойное жалованье
    Confiscation,    // Конфисковать контрабанду + штраф в Money
    TeamStats,       // Все характеристики команды * multiplier на duration ходов
    BonusTrade,      // % бонус к выгоде в следующем городе
}

[CreateAssetMenu(fileName = "NewShadowCard", menuName = "Event System/Card")]
public class ShadowCardData : ScriptableObject
{
    [Header("Основное")]
    public string cardName;

    [TextArea]
    public string description;

    [Header("Эффект")]
    public ShadowEffectType effectType;

    /// <summary>
    /// Основное числовое значение эффекта.
    /// Отрицательное = дебафф, положительное = бафф.
    /// Для TeamStats: множитель в процентах (например -50 = половина характеристик).
    /// Для BonusTrade: процент бонуса (например 5 = +5%).
    /// Для Confiscation: штраф в динарах при отсутствии контрабанды (0 = только конфискация).
    /// </summary>
    public int value;

    [Header("Временность")]
    [Tooltip("Длится ли эффект несколько ходов (false = мгновенный)")]
    public bool isTemporary = false;

    [Tooltip("Количество ходов действия (если isTemporary = true)")]
    public int duration = 1;

    [Header("Дополнительно")]
    [Tooltip("Для Confiscation: размер штрафа в динарах дополнительно к конфискации")]
    public int penaltyValue = 200;

    [Tooltip("Для BonusTrade: применяется один раз при следующем визите в город")]
    public bool applyOnceInCity = false;
}