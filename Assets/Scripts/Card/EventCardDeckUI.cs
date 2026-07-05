using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Менеджер колоды карт событий на Canvas.
/// Отвечает только за визуальный слой: очередь EventCardData → анимация флипа.
///
/// Иерархия сцены:
/// Canvas
///   └─ ShadowDeckUI  (этот компонент)
///        ├─ DeckAnchor      ← Image рубашки + счётчик
///        │    └─ DeckCountText (TextMeshProUGUI, опционально)
///        └─ CardSpawnRoot   ← пустой RectTransform по центру Canvas
/// </summary>
public class EventCardDeckUI : MonoBehaviour
{
    [Header("Ссылки")]
    [SerializeField] private EventCardDisplay         cardDisplayPrefab;
    [SerializeField] private RectTransform            cardSpawnRoot;
    [SerializeField] private Image                    deckVisual;
    [SerializeField] private TMPro.TextMeshProUGUI    deckCountText;

    [Header("Рубашка по умолчанию")]
    [Tooltip("Используется, если у EventCardData своей рубашки нет")]
    [SerializeField] private Sprite defaultBackSprite;
    public Sprite DefaultBackSprite => defaultBackSprite;

    [Header("Настройки")]
    [SerializeField] private bool shuffleOnStart = false;

    // ── Состояние ─────────────────────────────────────────────────────────
    private readonly Queue<EventCardData> _deck = new Queue<EventCardData>();
    private EventCardDisplay              _activeCard;

    // ── Lifecycle ─────────────────────────────────────────────────────────

    private void Start()
    {
        if (shuffleOnStart) ShuffleDeck();
        UpdateDeckVisual();
    }

    // ── Публичный API ─────────────────────────────────────────────────────

    public void AddCard(EventCardData card)
    {
        if (card == null) return;
        _deck.Enqueue(card);
        UpdateDeckVisual();
    }

    public void AddCards(IEnumerable<EventCardData> cards)
    {
        foreach (var c in cards) AddCard(c);
    }

    /// <summary>
    /// Вытащить верхнюю карту и показать с анимацией флипа.
    /// onRevealedCallback вызывается ровно по завершении флипа.
    /// </summary>
    public EventCardDisplay DrawAndShowTopCard(Action onRevealedCallback = null)
    {
        if (_deck.Count == 0)
        {
            Debug.LogWarning("[EventCardDeckUI] Колода пуста!");
            return null;
        }

        // Убираем предыдущую карту мгновенно
        DismissActiveCard(immediate: true);

        EventCardData drawnCard = _deck.Dequeue();
        UpdateDeckVisual();

        _activeCard = GetOrCreateCardDisplay();
        _activeCard.Setup(drawnCard, defaultBackSprite);
        _activeCard.OnCardRevealed += OnCardRevealedInternal;

        if (onRevealedCallback != null)
        {
            Action oneTime = null;
            oneTime = () =>
            {
                onRevealedCallback.Invoke();
                _activeCard.OnCardRevealed -= oneTime;
            };
            _activeCard.OnCardRevealed += oneTime;
        }

        _activeCard.ShowCard();
        return _activeCard;
    }

    /// <summary>
    /// Показать карту с особым оформлением «отменено» (эффект CancelCard).
    /// Карта не приносит эффекта — только анимация для обратной связи с игроком.
    /// </summary>
    public void ShowCancelledCard(ICard card)
    {
        if (card == null) return;
        var data         = card.ToEventCardData();
        data.cardTitle   = $"[Отменено] {data.cardTitle}";
        AddCard(data);
        DrawAndShowTopCard();
    }

    /// <summary>Скрыть текущую карту (плавно).</summary>
    public void HideCurrentCard()
    {
        DismissActiveCard(immediate: false);
        _activeCard = null;
    }

    public int  RemainingCount => _deck.Count;

    public void ShuffleDeck()
    {
        var list = new List<EventCardData>(_deck);
        _deck.Clear();
        for (int i = list.Count - 1; i > 0; i--)
        {
            int j = UnityEngine.Random.Range(0, i + 1);
            (list[i], list[j]) = (list[j], list[i]);
        }
        foreach (var c in list) _deck.Enqueue(c);
        UpdateDeckVisual();
    }

    // ── Приватные методы ──────────────────────────────────────────────────

    private void DismissActiveCard(bool immediate)
    {
        if (_activeCard == null) return;
        _activeCard.OnCardRevealed -= OnCardRevealedInternal;
        _activeCard.HideCard(immediate);
    }

    private EventCardDisplay GetOrCreateCardDisplay()
    {
        var existing = cardSpawnRoot.GetComponentInChildren<EventCardDisplay>(includeInactive: true);
        if (existing != null) return existing;

        var instance = Instantiate(cardDisplayPrefab, cardSpawnRoot);
        instance.gameObject.SetActive(false);
        return instance;
    }

    private void UpdateDeckVisual()
    {
        bool hasCards = _deck.Count > 0;
        if (deckVisual   != null) deckVisual.gameObject.SetActive(hasCards);
        if (deckCountText != null) deckCountText.text = hasCards ? $"×{_deck.Count}" : "";
    }

    private void OnCardRevealedInternal()
    {
        Debug.Log("[EventCardDeckUI] Флип завершён — карта открыта.");
    }

    // ── Тест (только в редакторе) ─────────────────────────────────────────
#if UNITY_EDITOR
    [Header("— Тест (Play Mode) —")]
    [SerializeField] private EventCardData[] testCards;

    private void Update()
    {
        var keyboard = UnityEngine.InputSystem.Keyboard.current;
        if (keyboard == null) return;

        if (keyboard.dKey.wasPressedThisFrame) DrawAndShowTopCard();
        if (keyboard.hKey.wasPressedThisFrame) HideCurrentCard();
        if (keyboard.rKey.wasPressedThisFrame) { AddCards(testCards); ShuffleDeck(); }
    }
#endif
}
