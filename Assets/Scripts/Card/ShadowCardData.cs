using UnityEngine;

public enum ShadowEffectType
{
    Money,           // Изменение динаров (мгновенно)
    Attack,          // Изменение атаки (временно)
    Capacity,        // Изменение грузоподъёмности (временно)
    Bargain,         // Изменение торговли (временно)
    AddGoods,        // Добавить N ед. случайного товара
    RemoveGoods,     // Удалить N ед. случайных товаров
    FireCrewMember,  // Уволить случайного члена команды
    WagePenalty,     // Двойное жалованье в следующем городе
    Confiscation,    // Конфисковать контрабанду + штраф
    TeamStats,       // Все характеристики команды * множитель на duration ходов
    BonusTrade,      // % бонус к выгоде в следующем городе
}

[CreateAssetMenu(fileName = "NewShadowCard", menuName = "Event System/Shadow Card")]
public class ShadowCardData : ScriptableObject, ICard
{
    [Header("Основное")]
    public string cardName;

    [TextArea]
    public string description;

    [Header("Эффект")]
    public ShadowEffectType effectType;

    [Tooltip("Основное числовое значение эффекта. Отрицательное = дебафф.")]
    public int value;

    [Header("Временность")]
    [Tooltip("Длится ли эффект несколько ходов (false = мгновенный)")]
    public bool isTemporary;

    [Tooltip("Количество ходов действия (если isTemporary = true)")]
    public int duration = 1;

    [Header("Дополнительно")]
    [Tooltip("Для Confiscation: штраф в динарах помимо конфискации")]
    public int penaltyValue = 200;

    // ── ICard ────────────────────────────────────────────────────────────
    string ICard.CardName    => cardName;
    string ICard.Description => description;
    CardDeckType ICard.DeckType   => CardDeckType.Shadow;
}