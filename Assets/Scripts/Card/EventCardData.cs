using UnityEngine;

[CreateAssetMenu(menuName = "ThousandRoads/Event Card", fileName = "NewEventCard")]
public class EventCardData : ScriptableObject
{
    [Header("Основное")]
    public string cardTitle = "Событие";

    [TextArea(3, 6)]
    public string description = "Описание события...";

    [Header("Визуал")]
    public Sprite cardFaceSprite;
    public Sprite cardBackSprite;

    [Header("Тип события")]
    public EventCardType cardType = EventCardType.Battle;

    [Header("Параметры")]
    public int difficulty = 0;
}

public enum EventCardType
{
    Battle,
    Shadow,
    Trade,
    Encounter,
    Treasure,
    Hazard
}