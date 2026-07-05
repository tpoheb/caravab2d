using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Управляет единой перемешанной колодой из Shadow- и Battle-карт.
/// Вытягивает карты по запросу GameManager и маршрутизирует их
/// в ShadowEffectManager или BattleManager соответственно.
/// </summary>
public class CardManager : MonoBehaviour
{
    // ── Одиночка ─────────────────────────────────────────────────────────
    public static CardManager Instance { get; private set; }

    [Header("Системы")]
    [SerializeField] private GameManager          gameManager;
    [SerializeField] private BattleManager        battleManager;
    [SerializeField] private ShadowEffectManager  effectManager;

    [Header("Карты")]
    [SerializeField] private List<ShadowCardData> allShadowCards = new List<ShadowCardData>();
    [SerializeField] private List<BattleCardData> allBattleCards = new List<BattleCardData>();

    [Header("UI")]
    [SerializeField] private EventCardDeckUI deckUI;

    // ── Состояние ────────────────────────────────────────────────────────
    private readonly CardDeck _deck = new CardDeck();

    public int RemainingCards => _deck.Count;

    // ─────────────────────────────────────────────────────────────────────
    // Lifecycle
    // ─────────────────────────────────────────────────────────────────────

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
    }

    private void Start() => RebuildDeck();

    // ─────────────────────────────────────────────────────────────────────
    // Сборка колоды
    // ─────────────────────────────────────────────────────────────────────

    private void RebuildDeck()
    {
        _deck.Clear();
        foreach (var c in allShadowCards) _deck.Add(c);
        foreach (var c in allBattleCards) _deck.Add(c);
        _deck.Shuffle();
        Debug.Log($"[CardManager] Колода собрана: {_deck.Count} карт.");
    }

    // ─────────────────────────────────────────────────────────────────────
    // Публичный API
    // ─────────────────────────────────────────────────────────────────────

    /// <summary>
    /// Вытянуть карту. Вызывается из GameManager по кнопке.
    /// Если колода кончилась — пересобирается автоматически.
    /// </summary>
    public void DrawCard()
    {
        if (_deck.IsEmpty)
        {
            Debug.Log("[CardManager] Колода кончилась — пересобираем.");
            RebuildDeck();
        }

        // Проверяем флаг отмены карты (карта Руки CancelCard)
        if (HandManager.Instance != null && HandManager.Instance.ConsumeCancelCard())
        {
            // Карту «поглощаем» без эффекта, но анимируем рубашку
            ICard cancelled = _deck.Draw();
            Debug.Log($"[CardManager] CancelCard: карта '{cancelled?.CardName}' отменена.");
            deckUI?.ShowCancelledCard(cancelled);
            gameManager.OnCardCancelled();
            return;
        }

        ICard card = _deck.Draw();
        RouteCard(card);
    }

    /// <summary>Скрыть текущую карту. Вызывается из GameManager.RequestEndTurn.</summary>
    public void HideEventCard() => deckUI?.HideCurrentCard();

    // ─────────────────────────────────────────────────────────────────────
    // Маршрутизация
    // ─────────────────────────────────────────────────────────────────────

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
            deckUI.AddCard(card.ToEventCardData());
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
            deckUI.AddCard(card.ToEventCardData());
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

    // ─────────────────────────────────────────────────────────────────────
    // Анимация
    // ─────────────────────────────────────────────────────────────────────

    private IEnumerator PlayCardWithAnimation(System.Action onRevealed)
    {
        bool flipDone = false;
        deckUI.DrawAndShowTopCard(onRevealedCallback: () => flipDone = true);
        yield return new WaitUntil(() => flipDone);
        onRevealed?.Invoke();
    }
}
