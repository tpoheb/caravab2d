using UnityEngine;

[CreateAssetMenu(fileName = "NewHandCard", menuName = "Game/Cards/HandCard")]
public class HandCardData : ScriptableObject
{
    public string cardName;
    [TextArea] public string description;
    public Sprite icon;
    
    public CardCategory category; // Тактическая, Логистическая, Экономическая
    public CardEffectType effectType;
    public int value;

    public enum CardCategory { Tactical, Logistic, Economic }
    public enum CardEffectType { Reroll, AddBonus, CapacityBoost, GoldBoost }
}