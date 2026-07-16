// =============================================================================
// BattleCardData.cs
// ScriptableObject для одной карты битвы.
//
// Создать вручную: Assets → Create → Battle Cards → Battle Card Data
// Или заполнить через импортёр: Tools → Battle Cards → Import from CSV
// =============================================================================

using UnityEngine;

[CreateAssetMenu(fileName = "Card_0_New", menuName = "Battle Cards/Battle Card Data")]
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

    // ── Реализация ICard ─────────────────────────────────────────────────
    // CardName  → enemyName (имя врага и есть «название» карты в колоде)
    // Description → поле description выше
    // DeckType  → всегда Battle для этого типа карт
    public string       CardName    => enemyName;
    public string       Description => description;
    public CardDeckType DeckType    => CardDeckType.Battle;

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
}