using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Управляет колодой карт и показом CardDisplay.
///
/// Иерархия в сцене:
///   EventPanelUI
///   ├── DeckImage          ← Image со спрайтом рубашки (статичный, всегда виден)
///   ├── CardSpawnRoot      ← сюда назначить cardSpawnRoot; сюда спавнится CardDisplay
///   └── ...текстовые поля EventPanelUI
/// </summary>
public class CardDeckUI : MonoBehaviour
{
    [Header("Колода")]
    [SerializeField] private TMPro.TextMeshProUGUI deckCountText;

    [Header("Карта события")]
    [Tooltip("Префаб с компонентом CardDisplay. Не содержит рубашку и анимацию.")]
    [SerializeField] private CardDisplay cardDisplayPrefab;
    [Tooltip("RectTransform, куда спавнится CardDisplay (поверх DeckImage).")]
    [SerializeField] private RectTransform cardSpawnRoot;

    [Header("Панель событий")]
    [SerializeField] private EventPanelUI eventPanelUI;

    private readonly Queue<ICard> _deck    = new Queue<ICard>();
    private readonly List<ICard>  _discard = new List<ICard>();
    private CardDisplay _activeCard;

    public event Action<ICard> OnCardDrawn;
    public event Action<ICard> OnCardRevealed;
    public event Action<ICard> OnCardDiscarded;

    public int RemainingCount => _deck.Count;
    public int DiscardCount   => _discard.Count;

    // ── Unity ─────────────────────────────────────────────────────────────

    private void Start()
    {
        UpdateDeckVisual();

        if (cardDisplayPrefab == null)
            Debug.LogError("[CardDeckUI] cardDisplayPrefab не назначен!");
        if (eventPanelUI == null)
            Debug.LogWarning("[CardDeckUI] eventPanelUI не назначен — детали карт не будут показаны.");
    }

    // ── Публичный API — колода ────────────────────────────────────────────

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

    /// <summary>
    /// Вытянуть верхнюю карту и показать мгновенно.
    /// После показа уведомляет EventPanelUI и вызывает onRevealedCallback.
    /// </summary>
    public CardDisplay DrawAndShowTopCard(Action onRevealedCallback = null)
    {
        if (_deck.Count == 0)
        {
            Debug.LogWarning("[CardDeckUI] Колода пуста!");
            return null;
        }

        // Скрываем предыдущую карту если была
        HideActiveCard();

        ICard drawnCard = _deck.Dequeue();
        OnCardDrawn?.Invoke(drawnCard);
        UpdateDeckVisual();

        // Получаем или создаём CardDisplay
        _activeCard = GetOrCreateCardDisplay();

        // Подписываемся на OnCardRevealed до ShowCard
        _activeCard.OnCardRevealed += OnRevealed;

        // Показываем карту — мгновенно, OnCardRevealed сработает внутри ShowCard
        _activeCard.ShowCard(drawnCard);

        return _activeCard;

        void OnRevealed()
        {
            _activeCard.OnCardRevealed -= OnRevealed;
            NotifyEventPanel(drawnCard);
            OnCardRevealed?.Invoke(drawnCard);
            onRevealedCallback?.Invoke();
        }
    }

    /// <summary>
    /// Показать отменённую карту с пометкой.
    /// </summary>
    public void ShowCancelledCard(ICard card)
    {
        if (card == null) return;
        DrawAndShowTopCard();
    }

    /// <summary>
    /// Сбросить текущую карту в отбой.
    /// </summary>
    public void DiscardCurrentCard()
    {
        if (_activeCard == null) return;

        var card = _activeCard.GetCurrentCard();
        if (card != null)
        {
            _discard.Add(card);
            OnCardDiscarded?.Invoke(card);
        }

        HideCurrentCard();
    }

    /// <summary>
    /// Скрыть текущую карту без сброса в отбой.
    /// </summary>
    public void HideCurrentCard()
    {
        HideActiveCard();
        _activeCard = null;
    }

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
        foreach (var c in _discard) _deck.Enqueue(c);
        _discard.Clear();
        ShuffleDeck();
    }

    // ── Приватные методы ──────────────────────────────────────────────────

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
                eventPanelUI.DisplayResult($"Карта «{cancelled.CardName}» отменена.", isPositive: true);
                break;
        }
    }

    private void HideActiveCard()
    {
        if (_activeCard == null) return;
        _activeCard.HideCard();
    }

    private CardDisplay GetOrCreateCardDisplay()
    {
        Transform parent = cardSpawnRoot != null ? cardSpawnRoot : transform;

        // Переиспользуем существующий инстанс если есть
        var existing = parent.GetComponentInChildren<CardDisplay>(includeInactive: true);
        if (existing != null) return existing;

        var instance = Instantiate(cardDisplayPrefab, parent);
        instance.gameObject.SetActive(false);
        return instance;
    }

    private void UpdateDeckVisual()
    {
        if (deckCountText != null)
            deckCountText.text = _deck.Count > 0 ? $"×{_deck.Count}" : "";
    }

    private static void ShuffleList<T>(List<T> list)
    {
        for (int i = list.Count - 1; i > 0; i--)
        {
            int j = UnityEngine.Random.Range(0, i + 1);
            (list[i], list[j]) = (list[j], list[i]);
        }
    }
}

// ── CancelledCardWrapper ──────────────────────────────────────────────────────

public class CancelledCardWrapper : ICard
{
    private readonly ICard _original;
    public CancelledCardWrapper(ICard original) => _original = original;

    public string CardName   => $"[Отменено] {_original.CardName}";
    public string Description => _original.Description;
    public CardDeckType DeckType => _original.DeckType;
}