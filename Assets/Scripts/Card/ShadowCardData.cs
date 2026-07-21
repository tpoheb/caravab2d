using UnityEngine;

[CreateAssetMenu(fileName = "NewShadowCard", menuName = "ThousandRoads/Cards/Shadow Card")]
public class ShadowCardData : ScriptableObject, ICard
{
    [Header("Идентификация")]
    [Tooltip("Уникальный числовой ID карты. Должен совпадать с ID в CSV.")]
    public int cardID;

    [Header("Основное")]
    public string cardName;

    [TextArea(3, 6)]
    public string description;

    [Header("Условия появления")]
    [Tooltip("Минимальная сложность хода для появления карты")]
    [Range(0, 10)]
    public int minDifficulty = 0;

    [Tooltip("Максимальная сложность хода для появления карты")]
    [Range(0, 10)]
    public int maxDifficulty = 10;

    [Tooltip("Вероятность появления относительно других карт (вес)")]
    [Range(1, 100)]
    public int weight = 10;

    [Header("Эффект")]
    public ShadowEffectType effectType;

    [Tooltip("Основное числовое значение эффекта. Отрицательное = дебафф.")]
    public int value;

    [Header("Временность")]
    [Tooltip("Длится ли эффект несколько ходов (false = мгновенный)")]
    public bool isTemporary;

    [Tooltip("Количество ходов действия (если isTemporary = true)")]
    [Range(1, 10)]
    public int duration = 1;

    [Header("Дополнительно")]
    [Tooltip("Для Confiscation: штраф в динарах помимо конфискации")]
    public int penaltyValue = 200;

    // ── ICard ────────────────────────────────────────────────────────────
    public string CardName       => cardName;
    public string Description    => description;
    public CardDeckType DeckType => CardDeckType.Shadow;
}