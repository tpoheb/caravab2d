using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class CardDeckUI : MonoBehaviour
{
    [Header("Ссылки")]
    [SerializeField] private CardDisplay cardDisplayPrefab;
    [SerializeField] private RectTransform cardSpawnRoot;
    [SerializeField] private Image deckVisual;
    [SerializeField] private TMPro.TextMeshProUGUI deckCountText;

    [Header("Настройки")]
    [SerializeField] private bool shuffleOnStart = false;

    private readonly Queue<ICard> _deck = new Queue<ICard>();
    private readonly List<ICard> _discardPile = new List<ICard>();
    private CardDisplay _activeCard;

    public event Action<ICard> OnCardDrawn;
    public event Action<ICard> OnCardRevealed;
    public event Action<ICard> OnCardDiscarded;

    private void Start()
    {
        if (shuffleOnStart) ShuffleDeck();
        UpdateDeckVisual();
    }

    public void AddCard(ICard card)
    {
        if (card == null) return;
        _deck.Enqueue(card);
        UpdateDeckVisual();
    }

    public void AddCards(IEnumerable<ICard> cards)
    {
        foreach (var c in cards) AddCard(c);
    }

    public CardDisplay DrawAndShowTopCard(Action onRevealedCallback = null)
    {
        if (_deck.Count == 0)
        {
            Debug.LogWarning("[CardDeckUI] Колода пуста!");
            return null;
        }

        DismissActiveCard(immediate: true);

        ICard drawnCard = _deck.Dequeue();
        OnCardDrawn?.Invoke(drawnCard);
        UpdateDeckVisual();

        _activeCard = GetOrCreateCardDisplay();
        _activeCard.Setup(drawnCard);
        _activeCard.OnCardRevealed += () =>
        {
            OnCardRevealed?.Invoke(drawnCard);
            onRevealedCallback?.Invoke();
        };

        _activeCard.ShowCard();
        return _activeCard;
    }

    public void ShowCancelledCard(ICard card)
    {
        if (card == null) return;
        var cancelled = new CancelledCardWrapper(card);
        AddCard(cancelled);
        DrawAndShowTopCard();
    }

    public void DiscardCurrentCard()
    {
        if (_activeCard == null) return;
        
        var card = _activeCard.GetCurrentCard();
        _discardPile.Add(card);
        OnCardDiscarded?.Invoke(card);
        
        HideCurrentCard();
    }

    public void HideCurrentCard()
    {
        DismissActiveCard(immediate: false);
        _activeCard = null;
    }

    public int RemainingCount => _deck.Count;
    public int DiscardCount => _discardPile.Count;

    public void ShuffleDeck()
    {
        var list = new List<ICard>(_deck);
        _deck.Clear();
        ShuffleList(list);
        foreach (var c in list) _deck.Enqueue(c);
        UpdateDeckVisual();
    }

    public void ReshuffleDiscardIntoDeck()
    {
        foreach (var c in _discardPile) _deck.Enqueue(c);
        _discardPile.Clear();
        ShuffleDeck();
    }

    private void DismissActiveCard(bool immediate)
    {
        if (_activeCard == null) return;
        _activeCard.HideCard(immediate);
    }

    private CardDisplay GetOrCreateCardDisplay()
    {
        var existing = cardSpawnRoot.GetComponentInChildren<CardDisplay>(includeInactive: true);
        if (existing != null) return existing;

        var instance = Instantiate(cardDisplayPrefab, cardSpawnRoot);
        instance.gameObject.SetActive(false);
        return instance;
    }

    private void UpdateDeckVisual()
    {
        bool hasCards = _deck.Count > 0;
        if (deckVisual != null) deckVisual.gameObject.SetActive(hasCards);
        if (deckCountText != null) deckCountText.text = hasCards ? $"×{_deck.Count}" : "";
    }

    private static void ShuffleList<T>(List<T> list)
    {
        for (int i = list.Count - 1; i > 0; i--)
        {
            int j = UnityEngine.Random.Range(0, i + 1);
            (list[i], list[j]) = (list[j], list[i]);
        }
    }

#if UNITY_EDITOR
    [Header("— Тест (Play Mode) —")]
    [SerializeField] private ScriptableObject[] testCards;

    private void Update()
    {
        var keyboard = UnityEngine.InputSystem.Keyboard.current;
        if (keyboard == null) return;

        if (keyboard.dKey.wasPressedThisFrame)
        {
            foreach (var so in testCards)
            {
                if (so is ICard card) AddCard(card);
            }
            DrawAndShowTopCard();
        }
        if (keyboard.hKey.wasPressedThisFrame) HideCurrentCard();
        if (keyboard.rKey.wasPressedThisFrame) { ShuffleDeck(); }
    }
#endif
}

public class CancelledCardWrapper : ICard
{
    private readonly ICard _original;
    
    public CancelledCardWrapper(ICard original)
    {
        _original = original;
    }

    public string CardName       => $"[Отменено] {_original.CardName}";
    public string Description    => _original.Description;
    public CardDeckType DeckType => _original.DeckType;
}