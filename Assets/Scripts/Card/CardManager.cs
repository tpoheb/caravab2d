using System;
using System.Collections.Generic;
using UnityEngine;

public class CardManager : MonoBehaviour
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

    public int RemainingCards => _deck.Count;
    public int DiscardCount => _discard.Count;
    public ICard CurrentCard => _currentCard;

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        RebuildDeck();
    }

    public void DrawCard(int difficulty, Func<bool> extraConditions = null)
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
        _currentCard = _deck.Draw(card => IsAllowedAtDifficulty(card, difficulty));
        UpdateDeckCounts();
        if (_currentCard == null)
        {
            Debug.LogWarning("[CardManager] Нет подходящих карт в общей колоде.");
            gameManager.OnCardCancelled();
            return;
        }

        if (HandManager.Instance != null && HandManager.Instance.ConsumeCancelCard())
        {
            deckUI?.ShowCancelledCard(_currentCard);
            gameManager.OnCardCancelled();
            return;
        }

        RouteCard(_currentCard);
    }

    public void DrawCard() => DrawCard(difficulty: 0);

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
        _currentCard = null;
        foreach (var card in allShadowCards) _deck.Add(card);
        foreach (var card in allBattleCards) _deck.Add(card);
        UpdateDeckCounts();
    }

    private static bool IsAllowedAtDifficulty(ICard card, int difficulty) => card switch
    {
        ShadowCardData shadow => shadow.minDifficulty <= difficulty && shadow.maxDifficulty >= difficulty,
        BattleCardData battle => battle.minDifficulty <= difficulty && battle.maxDifficulty >= difficulty,
        _ => false
    };

    private void UpdateDeckCounts() => deckUI?.SetDeckCounts(_deck.Count, _discard.Count);

    private void RouteCard(ICard card)
    {
        Action onRevealed;
        switch (card)
        {
            case ShadowCardData shadow:
                onRevealed = () =>
                {
                    effectManager.ApplyCard(shadow);
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
