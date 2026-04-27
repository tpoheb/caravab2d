using UnityEngine;

public enum ShadowEffectType { Money, Attack, Capacity, Bargain }

[CreateAssetMenu(fileName = "NewShadowCard", menuName = "Event System/Card")]
public class ShadowCardData : ScriptableObject
{
    public string cardName;
    [TextArea] public string description;
    public ShadowEffectType effectType;
    public int value; // Может быть отрицательным (дебафф) или положительным (бафф)
    public bool isTemporary = true; // Будет ли длиться 10 ходов
    public int duration = 10;
}