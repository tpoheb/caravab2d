using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CardManager : MonoBehaviour
{
    [Header("Системы")]
    [SerializeField] private GameManager gameManager;
    [SerializeField] private BattleManager battleManager;
    [SerializeField] private ShadowEffectManager effectManager;

    [Header("Единая колода (Shadow + Battle вперемешку)")]
    [SerializeField] private List<ShadowCardData> allShadowCards;
    [SerializeField] private List<BattleCardData> allBattleCards;

    [Header("UI колоды")]
    [SerializeField] private EventCardDeckUI deckUI;

    // --- Одиночка ---
    public static CardManager Instance { get; private set; }

    // Внутренняя перемешанная колода: хранит ShadowCardData или BattleCardData
    private List<object> _shuffledDeck = new List<object>();

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
    }

    private void Start()
    {
        BuildAndShuffleDeck();
    }

    // ─────────────────────────────────────────────────────────────────────
    // Сборка колоды
    // ─────────────────────────────────────────────────────────────────────

    private void BuildAndShuffleDeck()
    {
        _shuffledDeck.Clear();

        foreach (var c in allShadowCards) _shuffledDeck.Add(c);
        foreach (var c in allBattleCards) _shuffledDeck.Add(c);

        // Fisher-Yates shuffle
        for (int i = _shuffledDeck.Count - 1; i > 0; i--)
        {
            int j = Random.Range(0, i + 1);
            (_shuffledDeck[i], _shuffledDeck[j]) = (_shuffledDeck[j], _shuffledDeck[i]);
        }

        Debug.Log($"[CardManager] Колода собрана: {_shuffledDeck.Count} карт.");
    }

    // ─────────────────────────────────────────────────────────────────────
    // Вытянуть карту — вызывается из GameManager по кнопке
    // ─────────────────────────────────────────────────────────────────────

    public void DrawCard()
    {
        if (_shuffledDeck.Count == 0)
        {
            Debug.Log("[CardManager] Колода кончилась — пересобираем.");
            BuildAndShuffleDeck();
        }

        object topCard = _shuffledDeck[0];
        _shuffledDeck.RemoveAt(0);

        if (topCard is ShadowCardData shadow)
            DrawShadowCard(shadow);
        else if (topCard is BattleCardData battle)
            DrawBattleCard(battle);
    }

    public int RemainingCards => _shuffledDeck.Count;

    // ─────────────────────────────────────────────────────────────────────
    // Shadow Card
    // ─────────────────────────────────────────────────────────────────────

    private void DrawShadowCard(ShadowCardData card)
    {
        if (deckUI != null)
        {
            deckUI.AddCard(ShadowCardToEventCard(card));
            StartCoroutine(DrawWithAnimation(() =>
            {
                effectManager.ApplyCard(card);
                gameManager.OnShadowCardRevealed();
            }));
        }
        else
        {
            // Fallback без анимации
            effectManager.ApplyCard(card);
            gameManager.OnShadowCardRevealed();
        }
    }

    // ─────────────────────────────────────────────────────────────────────
    // Battle Card
    // ─────────────────────────────────────────────────────────────────────

    private void DrawBattleCard(BattleCardData card)
    {
        if (deckUI != null)
        {
            deckUI.AddCard(BattleCardToEventCard(card));
            StartCoroutine(DrawWithAnimation(() =>
            {
                // Карта открыта — запускаем подготовку боя и автобросок кубика
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
    // Coroutine: ждём конца флипа, затем вызываем логику
    // ─────────────────────────────────────────────────────────────────────

    private IEnumerator DrawWithAnimation(System.Action onRevealed)
    {
        bool flipDone = false;
        deckUI.DrawAndShowTopCard(onRevealedCallback: () => flipDone = true);
        yield return new WaitUntil(() => flipDone);
        onRevealed?.Invoke();
    }

    // ─────────────────────────────────────────────────────────────────────
    // Скрыть карту — вызывается из GameManager.RequestEndTurn
    // ─────────────────────────────────────────────────────────────────────

    public void HideEventCard()
    {
        deckUI?.HideCurrentCard();
    }

    // ─────────────────────────────────────────────────────────────────────
    // Конвертеры данных → EventCardData (только для анимации)
    // ─────────────────────────────────────────────────────────────────────

    private EventCardData ShadowCardToEventCard(ShadowCardData shadow)
    {
        var data         = ScriptableObject.CreateInstance<EventCardData>();
        data.cardTitle   = shadow.cardName;
        data.description = shadow.description;
        data.cardType    = EventCardType.Shadow;
        return data;
    }

    private EventCardData BattleCardToEventCard(BattleCardData battle)
    {
        var data         = ScriptableObject.CreateInstance<EventCardData>();
        data.cardTitle   = battle.enemyName;
        data.description = $"Требуемая атака: {battle.requiredAttack}\n"
                         + $"Победа: +{battle.rewardMoney} фелсов\n"
                         + $"Поражение: {battle.penaltyMoney} фелсов";
        data.cardType    = EventCardType.Battle;
        data.difficulty  = battle.requiredAttack;
        return data;
    }
}