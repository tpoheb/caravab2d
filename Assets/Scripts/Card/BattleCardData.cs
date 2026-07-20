using UnityEngine;

[CreateAssetMenu(fileName = "Card_0_New", menuName = "ThousandRoads/Cards/Battle Card")]
public class BattleCardData : ScriptableObject, ICard
{
    [Header("Идентификация")]
    [Tooltip("Уникальный числовой ID карты. Должен совпадать с ID в CSV.")]
    public int cardID;

    [Tooltip("Имя врага или название события на карте.")]
    public string enemyName;

    [Header("Описание карты")]
    [Tooltip("Текст описания, отображаемый на карте в UI.")]
    [TextArea(2, 4)]
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

    [Header("Условие победы")]
    [Tooltip("Минимальная суммарная атака каравана для победы в бою.")]
    public int requiredAttack;

    [Header("Награда при победе")]
    [Tooltip("Количество золота, получаемого при победе.")]
    public int rewardMoney;

    [Tooltip(
        "ID карты руки (HandCardData), добавляемой в руку игрока при победе.\n" +
        "0 = карта руки не выдаётся.\n" +
        "Фактический ассет HandCardData должен быть найден во время выполнения " +
        "через реестр карт или Resources.Load."
    )]
    public int rewardHandCardID;

    [Header("Штраф при поражении")]
    [Tooltip("Количество золота, теряемого при поражении.")]
    public int penaltyMoney;

    [Tooltip(
        "Количество случайных членов команды, теряемых при поражении.\n" +
        "0 = потерь в команде нет."
    )]
    public int crewLoss;

    // ── ICard ────────────────────────────────────────────────────────────
    public string CardName       => enemyName;
    public string Description    => description;
    public CardDeckType DeckType => CardDeckType.Battle;
}