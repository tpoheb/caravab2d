using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class CardManager : MonoBehaviour
{
    public static CardManager Instance { get; private set; }

    [Header("Системы")]
    [SerializeField] private GameManager gameManager;
    [SerializeField] private BattleManager battleManager;
    [SerializeField] private ShadowEffectManager effectManager;

    [Header("Карты")]
    [SerializeField] private List<ShadowCardData> allShadowCards = new List<ShadowCardData>();
    [SerializeField] private List<BattleCardData> allBattleCards = new List<BattleCardData>();

    [Header("Условия розыгрыша")]
    [Tooltip("Базовый шанс Battle-карты (0-1). Остальное — Shadow.")]
    [Range(0f, 1f)]
    [SerializeField] private float baseBattleChance = 0.3f;
    [SerializeField] private AnimationCurve difficultyToBattleChance;

    [Header("UI")]
    [SerializeField] private CardDeckUI deckUI;

    private readonly CardDeck _deck = new CardDeck();
    private int _currentDifficulty;

    public int RemainingCards => _deck.Count;

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
    }

    private void Start() => RebuildDeck();

    // ── Публичный API ─────────────────────────────────────────────────────

    /// <summary>
    /// Вытянуть карту с условиями (вызывается каждый ход).
    /// </summary>
    public void DrawCard(int difficulty, System.Func<bool> extraConditions = null)
    {
        _currentDifficulty = difficulty;

        if (_deck.IsEmpty)
        {
            Debug.Log("[CardManager] Колода кончилась — пересобираем.");
            RebuildDeck();
        }

        // Отмена карты
        if (HandManager.Instance != null && HandManager.Instance.ConsumeCancelCard())
        {
            ICard cancelled = DrawWithConditions(difficulty, extraConditions);
            Debug.Log($"[CardManager] CancelCard: карта '{cancelled?.CardName}' отменена.");
            deckUI?.ShowCancelledCard(cancelled);
            gameManager.OnCardCancelled();
            return;
        }

        ICard card = DrawWithConditions(difficulty, extraConditions);
        if (card == null)
        {
            Debug.LogWarning("[CardManager] Нет подходящих карт!");
            return;
        }

        RouteCard(card);
    }

    /// <summary>Старый метод без условий (для совместимости).</summary>
    public void DrawCard() => DrawCard(difficulty: 0);

    public void HideEventCard() => deckUI?.HideCurrentCard();

    // ── Приватные методы ──────────────────────────────────────────────────

    private ICard DrawWithConditions(int difficulty, System.Func<bool> extraConditions)
    {
        bool drawBattle = ResolveDeckType(difficulty, extraConditions);

        if (drawBattle)
        {
            var valid = allBattleCards
                .Where(c => c.minDifficulty <= difficulty && c.maxDifficulty >= difficulty)
                .ToList();
            return valid.Count > 0 ? WeightedRandom(valid) : null;
        }
        else
        {
            var valid = allShadowCards
                .Where(c => c.minDifficulty <= difficulty && c.maxDifficulty >= difficulty)
                .ToList();
            return valid.Count > 0 ? WeightedRandom(valid) : null;
        }
    }

    private bool ResolveDeckType(int difficulty, System.Func<bool> extraConditions)
    {
        float chance = baseBattleChance;
        
        if (difficultyToBattleChance != null && difficultyToBattleChance.length > 0)
            chance = difficultyToBattleChance.Evaluate(difficulty);

        if (extraConditions != null && !extraConditions.Invoke())
            chance *= 0.5f;

        return UnityEngine.Random.value < chance;
    }

    private T WeightedRandom<T>(List<T> cards) where T : ICard
    {
        int totalWeight = 0;
        foreach (var c in cards)
        {
            totalWeight += GetWeight(c);
        }

        int roll = UnityEngine.Random.Range(0, totalWeight);
        int current = 0;

        foreach (var c in cards)
        {
            current += GetWeight(c);
            if (roll < current) return c;
        }
        return cards[0];
    }

    private int GetWeight(ICard card) => card switch
    {
        ShadowCardData s => s.weight,
        BattleCardData b => b.weight,
        _ => 10
    };

    private void RebuildDeck()
    {
        _deck.Clear();
        foreach (var c in allShadowCards) _deck.Add(c);
        foreach (var c in allBattleCards) _deck.Add(c);
        _deck.Shuffle();
        Debug.Log($"[CardManager] Колода собрана: {_deck.Count} карт.");
    }

    private void RouteCard(ICard card)
    {
        switch (card)
        {
            case ShadowCardData shadow:
                HandleShadowCard(shadow);
                break;
            case BattleCardData battle:
                HandleBattleCard(battle);
                break;
            default:
                Debug.LogError($"[CardManager] Неизвестный тип карты: {card?.GetType()}");
                break;
        }
    }

    private void HandleShadowCard(ShadowCardData card)
    {
        if (deckUI != null)
        {
            deckUI.AddCard(card);
            StartCoroutine(PlayCardWithAnimation(() =>
            {
                effectManager.ApplyCard(card);
                gameManager.OnShadowCardRevealed();
            }));
        }
        else
        {
            effectManager.ApplyCard(card);
            gameManager.OnShadowCardRevealed();
        }
    }

    private void HandleBattleCard(BattleCardData card)
    {
        if (deckUI != null)
        {
            deckUI.AddCard(card);
            StartCoroutine(PlayCardWithAnimation(() =>
            {
                battleManager.PrepareBattle(card);
                gameManager.OnBattleCardRevealed();
            }));
        }
        else
        {
            battleManager.PrepareBattle(card);
            gameManager.OnBattleCardRevealed();
        }
    }

    private IEnumerator PlayCardWithAnimation(System.Action onRevealed)
    {
        bool flipDone = false;
        deckUI.DrawAndShowTopCard(() => flipDone = true);
        yield return new WaitUntil(() => flipDone);
        onRevealed?.Invoke();
    }
}