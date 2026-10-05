using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>Общая колода: взвешенный выбор без повторов до возврата сброса.</summary>
public sealed class CardDeck
{
    private readonly List<ICard> _cards = new List<ICard>();

    public int Count => _cards.Count;
    public bool IsEmpty => _cards.Count == 0;

    public void Add(ICard card)
    {
        if (card != null && !_cards.Contains(card)) _cards.Add(card);
    }

    public void AddRange(IEnumerable<ICard> cards)
    {
        foreach (var card in cards) Add(card);
    }

    public void Clear() => _cards.Clear();

    public void Shuffle()
    {
        for (int i = _cards.Count - 1; i > 0; i--)
        {
            int j = UnityEngine.Random.Range(0, i + 1);
            (_cards[i], _cards[j]) = (_cards[j], _cards[i]);
        }
    }

    /// <summary>
    /// Выбирает подходящую карту по весу и удаляет её из колоды.
    /// weightSelector позволяет позднее учитывать свойства текущей клетки.
    /// Нулевой вес исключает карту из розыгрыша.
    /// </summary>
    public ICard Draw(Func<ICard, bool> condition = null, Func<ICard, int> weightSelector = null)
    {
        var candidates = new List<(int Index, int Weight)>();
        long totalWeight = 0;

        for (int i = 0; i < _cards.Count; i++)
        {
            var card = _cards[i];
            if (condition != null && !condition(card)) continue;

            int weight = weightSelector != null ? weightSelector(card) : GetWeight(card);
            if (weight <= 0) continue;

            candidates.Add((i, weight));
            totalWeight += weight;
        }

        if (totalWeight == 0) return null;

        // Random.value может вернуть 1: ограничиваем последним допустимым весом.
        long roll = Math.Min((long)((double)UnityEngine.Random.value * totalWeight), totalWeight - 1);
        foreach (var candidate in candidates)
        {
            if (roll < candidate.Weight)
            {
                var card = _cards[candidate.Index];
                _cards.RemoveAt(candidate.Index);
                return card;
            }
            roll -= candidate.Weight;
        }

        return null;
    }

    public ICard Peek() => IsEmpty ? null : _cards[0];

    private static int GetWeight(ICard card) => card switch
    {
        ShadowCardData shadow => shadow.weight,
        BattleCardData battle => battle.weight,
        _ => 10
    };
}
