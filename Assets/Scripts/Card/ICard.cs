using UnityEngine;

public enum CardDeckType
{
    Shadow,
    Battle,
}

public interface ICard
{
    string CardName        { get; }
    string Description     { get; }
    CardDeckType DeckType  { get; }
}