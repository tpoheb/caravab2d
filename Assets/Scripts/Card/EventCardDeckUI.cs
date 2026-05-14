using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Менеджер колоды карт событий на Canvas.
///
/// Иерархия сцены:
/// Canvas
///   └─ ShadowDeckUI  (этот компонент)
///        ├─ DeckAnchor      ← Image рубашки + счётчик (привязать к углу)
///        │    └─ DeckCountText (TextMeshProUGUI, опционально)
///        └─ CardSpawnRoot   ← пустой RectTransform по центру Canvas
/// </summary>
public class EventCardDeckUI : MonoBehaviour
{
    [Header("Ссылки")]
    [SerializeField] private EventCardDisplay cardDisplayPrefab;
    [SerializeField] private RectTransform cardSpawnRoot;
    [SerializeField] private Image deckVisual;
    [SerializeField] private TMPro.TextMeshProUGUI deckCountText;

    [Header("Рубашка по умолчанию")]
    [Tooltip("Спрайт рубашки, если у EventCardData своей нет")]
    [SerializeField] private Sprite defaultBackSprite;
    public Sprite DefaultBackSprite => defaultBackSprite;

    [Header("Настройки")]
    [SerializeField] private bool shuffleOnStart = false;

    // ── состояние ──────────────────────────────────────────────────────────
    private Queue<EventCardData> _deck = new Queue<EventCardData>();
    private EventCardDisplay _activeCard;

    // ──────────────────────────────────────────────────────────────────────
    private void Start()
    {
        if (shuffleOnStart) ShuffleDeck();
        UpdateDeckVisual();
    }

    // ──────────────────────────────────────────────────────────────────────
    // Public API
    // ──────────────────────────────────────────────────────────────────────

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
    /// Вытаскивает верхнюю карту и показывает с анимацией.
    /// <para>onRevealedCallback — вызывается ровно по завершении флипа (лицо открыто).</para>
    /// Возвращает EventCardDisplay или null если колода пуста.
    /// </summary>
    public EventCardDisplay DrawAndShowTopCard(Action onRevealedCallback = null)
    {
        if (_deck.Count == 0)
        {
            Debug.LogWarning("[EventCardDeckUI] Колода пуста!");
            return null;
        }

        // Убрать предыдущую карту мгновенно
        if (_activeCard != null)
        {
            _activeCard.OnCardRevealed -= OnCardRevealedInternal;
            _activeCard.HideCard(immediate: true);
        }

        EventCardData drawnCard = _deck.Dequeue();
        UpdateDeckVisual();

        _activeCard = GetOrCreateCardDisplay();
        _activeCard.Setup(drawnCard, defaultBackSprite);

        // Постоянная подписка (лог)
        _activeCard.OnCardRevealed += OnCardRevealedInternal;

        // Одноразовый callback для coroutine в CardManager
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

    /// <summary>Скрыть текущую карту (плавно).</summary>
    public void HideCurrentCard()
    {
        if (_activeCard == null) return;
        _activeCard.OnCardRevealed -= OnCardRevealedInternal;
        _activeCard.HideCard(immediate: false);
        _activeCard = null;
    }

    public int RemainingCount => _deck.Count;

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

    // ──────────────────────────────────────────────────────────────────────
    // Приватные методы
    // ──────────────────────────────────────────────────────────────────────

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
        if (deckVisual != null)    deckVisual.gameObject.SetActive(hasCards);
        if (deckCountText != null) deckCountText.text = hasCards ? $"×{_deck.Count}" : "";
    }

    private void OnCardRevealedInternal()
    {
        Debug.Log("[EventCardDeckUI] Флип завершён — карта открыта.");
    }

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