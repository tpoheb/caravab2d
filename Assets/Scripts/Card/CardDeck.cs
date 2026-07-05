using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Типизированная тасуемая колода карт.
/// Заменяет List&lt;object&gt; в CardManager — исключает боксинг и небезопасные касты.
/// </summary>
public sealed class CardDeck
{
    private readonly List<ICard> _cards = new List<ICard>();

    public int Count => _cards.Count;
    public bool IsEmpty => _cards.Count == 0;

    // ── Наполнение ───────────────────────────────────────────────────────

    public void Add(ICard card)
    {
        if (card != null) _cards.Add(card);
    }

    public void AddRange(IEnumerable<ICard> cards)
    {
        foreach (var c in cards)
            Add(c);
    }

    public void Clear() => _cards.Clear();

    // ── Тасовка (Fisher-Yates) ───────────────────────────────────────────

    public void Shuffle()
    {
        for (int i = _cards.Count - 1; i > 0; i--)
        {
            int j = Random.Range(0, i + 1);
            (_cards[i], _cards[j]) = (_cards[j], _cards[i]);
        }
    }

    // ── Вытянуть верхнюю карту ───────────────────────────────────────────

    /// <summary>
    /// Возвращает верхнюю карту и удаляет её из колоды.
    /// Возвращает null, если колода пуста.
    /// </summary>
    public ICard Draw()
    {
        if (IsEmpty) return null;
        var top = _cards[0];
        _cards.RemoveAt(0);
        return top;
    }

    // ── Peek без удаления ────────────────────────────────────────────────

    public ICard Peek() => IsEmpty ? null : _cards[0];
}
