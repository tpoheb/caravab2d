/// <summary>
/// Общий контракт для любой карты, которая может лежать в колоде.
/// </summary>
public interface ICard
{
    string CardName    { get; }
    string Description { get; }
    CardDeckType DeckType  { get; }
}

public enum CardDeckType
{
    Shadow,
    Battle,
}
