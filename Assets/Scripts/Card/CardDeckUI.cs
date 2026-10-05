using System;
using UnityEngine;

/// <summary>Отображает карту и счётчики. Состояние общей колоды принадлежит CardManager.</summary>
public class CardDeckUI : MonoBehaviour
{
    [Header("Колода")]
    [SerializeField] private TMPro.TextMeshProUGUI deckCountText;

    [Header("Карта события")]
    [SerializeField] private CardDisplay cardDisplayPrefab;
    [SerializeField] private RectTransform cardSpawnRoot;

    [Header("Панель событий")]
    [SerializeField] private EventPanelUI eventPanelUI;

    private CardDisplay _activeCard;
    public event Action<ICard> OnCardDrawn;
    public event Action<ICard> OnCardRevealed;
    public event Action<ICard> OnCardDiscarded;

    public int RemainingCount { get; private set; }
    public int DiscardCount { get; private set; }

    public void SetDeckCounts(int remaining, int discarded)
    {
        RemainingCount = remaining;
        DiscardCount = discarded;
        if (deckCountText != null) deckCountText.text = $"×{remaining}";
    }

    public void ShowCard(ICard card, Action onRevealed = null)
    {
        if (card == null) return;
        HideCurrentCard();
        OnCardDrawn?.Invoke(card);
        _activeCard = GetOrCreateCardDisplay();
        if (_activeCard != null) _activeCard.ShowCard(card);
        NotifyEventPanel(card);
        OnCardRevealed?.Invoke(card);
        onRevealed?.Invoke();
    }

    public void ShowCancelledCard(ICard card)
    {
        if (card != null) ShowCard(new CancelledCardWrapper(card));
    }

    public void DiscardCurrentCard()
    {
        var card = _activeCard != null ? _activeCard.GetCurrentCard() : null;
        if (card != null) OnCardDiscarded?.Invoke(card);
        HideCurrentCard();
    }

    public void HideCurrentCard()
    {
        if (_activeCard != null) _activeCard.HideCard();
        _activeCard = null;
    }

    private void NotifyEventPanel(ICard card)
    {
        if (eventPanelUI == null) return;
        switch (card)
        {
            case ShadowCardData shadow:
                eventPanelUI.DisplayShadowCard(shadow);
                break;
            case BattleCardData battle:
                eventPanelUI.DisplayBattleCard(battle);
                break;
            case CancelledCardWrapper cancelled:
                eventPanelUI.ClearAll();
                eventPanelUI.DisplayResult($"Карта «{cancelled.CardName}» отменена.", true);
                break;
        }
    }

    private CardDisplay GetOrCreateCardDisplay()
    {
        Transform parent = cardSpawnRoot != null ? cardSpawnRoot : transform;
        var existing = parent.GetComponentInChildren<CardDisplay>(includeInactive: true);
        if (existing != null) return existing;
        if (cardDisplayPrefab == null)
        {
            Debug.LogError("[CardDeckUI] cardDisplayPrefab не назначен.");
            return null;
        }
        return Instantiate(cardDisplayPrefab, parent);
    }
}

public class CancelledCardWrapper : ICard
{
    private readonly ICard _original;
    public CancelledCardWrapper(ICard original) => _original = original;
    public string CardName => _original.CardName;
    public string Description => _original.Description;
    public CardDeckType DeckType => _original.DeckType;
}
