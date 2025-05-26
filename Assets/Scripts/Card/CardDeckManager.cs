using System.Collections.Generic;
using UnityEngine;

public class CardDeckManager : MonoBehaviour
{
    public List<CardData> deck = new List<CardData>();
    public List<CardData> discardPile = new List<CardData>();
    public int handSize = 5;

    public List<CardData> CurrentHand = new List<CardData>();

    public void InitializeDeck()
    {
        // Перемешиваем колоду
        ShuffleDeck();
        DrawInitialHand();
    }

    private void ShuffleDeck()
    {
        for (int i = 0; i < deck.Count; i++)
        {
            CardData temp = deck[i];
            int randomIndex = Random.Range(i, deck.Count);
            deck[i] = deck[randomIndex];
            deck[randomIndex] = temp;
        }
    }

    private void DrawInitialHand()
    {
        for (int i = 0; i < handSize; i++)
        {
            if (deck.Count > 0)
            {
                CurrentHand.Add(deck[0]);
                deck.RemoveAt(0);
            }
        }
    }

    public CardData DrawCard()
    {
        if (deck.Count == 0)
        {
            ReshuffleDiscardPile();
            if (deck.Count == 0) return null;
        }

        CardData drawnCard = deck[0];
        deck.RemoveAt(0);
        return drawnCard;
    }

    private void ReshuffleDiscardPile()
    {
        deck = new List<CardData>(discardPile);
        discardPile.Clear();
        ShuffleDeck();
    }

    public void PlayCard(CardData card, PlayerInventory player)
    {
        card.ApplyEffects(player);
        discardPile.Add(card);
        CurrentHand.Remove(card);

        // Добираем новую карту
        if (deck.Count > 0)
        {
            CurrentHand.Add(DrawCard());
        }
    }
}