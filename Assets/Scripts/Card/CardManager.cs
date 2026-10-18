using System;
using System.Collections.Generic;
using UnityEngine;

public partial class CardManager : MonoBehaviour
{
    public static CardManager Instance { get; private set; }

    [Header("Системы")]
    [SerializeField] private GameManager gameManager;
    [SerializeField] private BattleManager battleManager;
    [SerializeField] private ShadowEffectManager effectManager;

    // Сохраняем поля каталогов, чтобы не потерять ссылки существующих сцен.
    // Во время игры оба каталога образуют одну расходуемую колоду.
    [Header("Карты общей колоды")]
    [SerializeField] private List<ShadowCardData> allShadowCards = new List<ShadowCardData>();
    [SerializeField] private List<BattleCardData> allBattleCards = new List<BattleCardData>();

    [Header("UI")]
    [SerializeField] private CardDeckUI deckUI;

    private readonly CardDeck _deck = new CardDeck();
    private readonly List<ICard> _discard = new List<ICard>();
    private ICard _currentCard;
    private bool _eventResolved;
    private bool _eventPenaltyUndone, _eventCancelled;
    private ICard _previewCard;
    public bool CanPreview => _currentCard == null && _previewCard == null && (_deck.Count > 0 || _discard.Count > 0);
    public bool CanUndoEventPenalty => _eventResolved && !_eventPenaltyUndone && !_eventCancelled &&
        _currentCard is ShadowCardData shadow && shadow.effectType == ShadowEffectType.Attack && shadow.value < 0;

    public void PreviewNextCard()
    {
        if (!CanPreview) return;
        RefillIfEmpty();
        var copy = new CardDeck(); copy.AddRange(_deck.Cards);
        _previewCard = copy.Draw();
        deckUI?.ShowCard(_previewCard);
        HandManager.Instance?.RefreshUI();
    }

    public void RedrawEvent()
    {
        if (!(_currentCard is ShadowCardData) || _eventResolved) return;
        HideEventCard(); DrawCard();
    }

    public void UndoEventPenalty()
    {
        if (!CanUndoEventPenalty || !effectManager.UndoAttackPenalty((ShadowCardData)_currentCard)) return;
        _eventPenaltyUndone = true;
        deckUI?.ShowPenaltyCancelled();
        HandManager.Instance?.RefreshUI();
    }

    private void RefillIfEmpty()
    {
        if (!_deck.IsEmpty || _discard.Count == 0) return;
        _deck.AddRange(_discard); _discard.Clear();
    }

    public int RemainingCards => _deck.Count;
    public int DiscardCount => _discard.Count;
    public ICard CurrentCard => _currentCard;

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        RebuildDeck();
    }

    public void DrawCard(Func<bool> extraConditions)
    {
        if (_currentCard != null)
        {
            Debug.LogWarning("[CardManager] Текущая карта ещё не завершена.");
            return;
        }

        if (_deck.IsEmpty && _discard.Count > 0)
        {
            _deck.AddRange(_discard);
            _discard.Clear();
            Debug.Log("[CardManager] Сброс возвращён в общую колоду.");
        }

        if (extraConditions != null && !extraConditions())
        {
            UpdateDeckCounts();
            gameManager.OnCardCancelled();
            return;
        }

        // Битвы и события участвуют в одном выборе: отдельного шанса битвы нет.
        if (_previewCard != null && _deck.Take(_previewCard)) _currentCard = _previewCard;
        else _currentCard = _deck.Draw();
        _previewCard = null;
        _eventResolved = false;
        _eventPenaltyUndone = _eventCancelled = false;
        UpdateDeckCounts();
        if (_currentCard == null)
        {
            Debug.LogWarning("[CardManager] Нет подходящих карт в общей колоде.");
            gameManager.OnCardCancelled();
            return;
        }

        RouteCard(_currentCard);
    }

    public void DrawCard() => DrawCard(extraConditions: null);

    /// <summary>Эффект применяется после окна розыгрыша «во время события».</summary>
    public void ResolveCurrentEvent()
    {
        if (_eventResolved || !(_currentCard is ShadowCardData shadow)) return;
        _eventResolved = true;
        effectManager.ApplyCard(shadow);
    }

    public void CancelCurrentCard()
    {
        if (_currentCard == null || _eventResolved) return;
        _eventResolved = true;
        _eventCancelled = true;
        deckUI?.ShowCancelledCard(_currentCard);
        if (_currentCard is BattleCardData) battleManager.CancelBattle();
        gameManager.OnCardCancelled();
    }

    /// <summary>Завершённая или отменённая карта уходит в сброс ровно один раз.</summary>
    public void HideEventCard()
    {
        if (_currentCard != null)
        {
            _discard.Add(_currentCard);
            _currentCard = null;
            deckUI?.DiscardCurrentCard();
        }
        else
        {
            deckUI?.HideCurrentCard();
        }
        UpdateDeckCounts();
    }

    private void RebuildDeck()
    {
        _deck.Clear();
        _discard.Clear();
        _currentCard = _previewCard = null;
        _eventPenaltyUndone = _eventCancelled = false;
        foreach (var card in allShadowCards) _deck.Add(card);
        foreach (var card in allBattleCards) _deck.Add(card);
        UpdateDeckCounts();
    }

    private void UpdateDeckCounts() => deckUI?.SetDeckCounts(_deck.Count, _discard.Count);

    private void RouteCard(ICard card)
    {
        Action onRevealed;
        switch (card)
        {
            case ShadowCardData shadow:
                onRevealed = () =>
                {
                    gameManager.OnShadowCardRevealed();
                };
                break;
            case BattleCardData battle:
                onRevealed = () =>
                {
                    if (battleManager.PrepareBattle(battle))
                        gameManager.OnBattleCardRevealed();
                    else
                        gameManager.OnBattleForceEnded();
                };
                break;
            default:
                Debug.LogError($"[CardManager] Неизвестный тип карты: {card?.GetType()}");
                gameManager.OnCardCancelled();
                return;
        }

        if (deckUI != null) deckUI.ShowCard(card, onRevealed);
        else onRevealed();
    }
}
