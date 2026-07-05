using UnityEngine;

/// <summary>
/// ScriptableObject с данными карты битвы. Реализует ICard для единой колоды.
/// </summary>
[CreateAssetMenu(fileName = "NewBattleCard", menuName = "Battle Card/Battle Card")]
public class BattleCardData : ScriptableObject, ICard
{
    [Header("Идентификация")]
    public int    cardID;
    public string enemyName = "Речные пираты";

    [Header("Боевые характеристики")]
    [Tooltip("Требуемая суммарная атака для победы")]
    public int requiredAttack = 3;

    [Header("Награды / Штрафы")]
    [Tooltip("Деньги при победе (положительное значение)")]
    public int rewardMoney  =  60;
    [Tooltip("Деньги при поражении (отрицательное значение)")]
    public int penaltyMoney = -50;

    // ── ICard ────────────────────────────────────────────────────────────
    string ICard.CardName    => enemyName;
    string ICard.Description => $"Требуемая атака: {requiredAttack}\n"
                                + $"Победа: +{rewardMoney} фелсов\n"
                                + $"Поражение: {penaltyMoney} фелсов";
    CardDeckType ICard.DeckType   => CardDeckType.Battle;
}