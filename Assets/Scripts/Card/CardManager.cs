using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CardManager : MonoBehaviour
{
    [Header("Системы")]
    [SerializeField] private GameManager gameManager;
    [SerializeField] private BattleUIManager uiManager;
    [SerializeField] private ShadowEffectManager effectManager;

    [Header("Карты данных")]
    [SerializeField] private List<ShadowCardData> allShadowCards;
    [SerializeField] private List<BattleCardData> allBattleCards;

    [Header("Анимация карт событий")]
    [SerializeField] private EventCardDeckUI shadowDeckUI;  // колода для ShadowCard
    [SerializeField] private EventCardDeckUI battleDeckUI;  // колода для BattleCard (опционально)

    // --- Одиночка ---
    public static CardManager Instance { get; private set; }

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
    }

    // ─────────────────────────────────────────────────────────────────────
    // Shadow Cards
    // ─────────────────────────────────────────────────────────────────────

    /// <summary>
    /// Вызывается из GameManager при DiceEventType.ShadowInfluence.
    /// Показывает анимированную карту, применяет эффект ПОСЛЕ флипа.
    /// </summary>
    public void DrawCard()
    {
        if (allShadowCards.Count == 0) return;

        ShadowCardData selectedCard = allShadowCards[Random.Range(0, allShadowCards.Count)];

        // Если колода UI назначена — показываем с анимацией
        if (shadowDeckUI != null)
        {
            // Кладём карту в колоду и тянем с анимацией
            EventCardData eventData = ShadowCardToEventCard(selectedCard);
            shadowDeckUI.AddCard(eventData);

            StartCoroutine(DrawWithAnimation(shadowDeckUI, () =>
            {
                // Эффект применяется только после завершения флипа
                effectManager.ApplyCard(selectedCard);
                uiManager.DisplayShadowCard(selectedCard);
                OnEventCardShown();
            }));
        }
        else
        {
            // Fallback: старое поведение без анимации
            effectManager.ApplyCard(selectedCard);
            uiManager.DisplayShadowCard(selectedCard);
        }
    }

    // ─────────────────────────────────────────────────────────────────────
    // Battle Cards
    // ─────────────────────────────────────────────────────────────────────

    public BattleCardData GetRandomBattleCard()
    {
        if (allBattleCards.Count == 0) return null;
        return allBattleCards[Random.Range(0, allBattleCards.Count)];
    }

    /// <summary>
    /// Опционально: показать карту битвы с анимацией.
    /// Вызывать из GameManager вместо прямого battleManager.PrepareBattle(),
    /// если хочешь анимацию и для BattleCard.
    /// </summary>
    public void ShowBattleCardAnimated(BattleCardData card, System.Action onShown)
    {
        if (battleDeckUI == null || card == null)
        {
            onShown?.Invoke();
            return;
        }

        EventCardData eventData = BattleCardToEventCard(card);
        battleDeckUI.AddCard(eventData);

        StartCoroutine(DrawWithAnimation(battleDeckUI, onShown));
    }

    // ─────────────────────────────────────────────────────────────────────
    // Coroutine: ждём окончания флипа
    // ─────────────────────────────────────────────────────────────────────

    private IEnumerator DrawWithAnimation(EventCardDeckUI deckUI, System.Action onRevealed)
    {
        bool flipDone = false;

        // Тянем карту — внутри DeckUI запустит ShowCard() с анимацией
        EventCardDisplay display = deckUI.DrawAndShowTopCard(onRevealedCallback: () =>
        {
            flipDone = true;
        });

        // Ждём конца флипа
        yield return new WaitUntil(() => flipDone);

        // Вызываем логику (применение эффекта, показ текста и т.д.)
        onRevealed?.Invoke();
    }

    // ─────────────────────────────────────────────────────────────────────
    // Конвертеры: игровые данные → EventCardData для DeckUI
    // ─────────────────────────────────────────────────────────────────────

    /// <summary>
    /// Создаёт временный EventCardData из ShadowCardData на лету.
    /// Не создаёт asset на диске — только в памяти.
    /// </summary>
    private EventCardData ShadowCardToEventCard(ShadowCardData shadow)
    {
        var data = ScriptableObject.CreateInstance<EventCardData>();
        data.cardTitle       = shadow.cardName;
        data.description     = shadow.description;
        data.cardType        = EventCardType.Shadow;
        // Спрайт: назначь в инспекторе через shadowDeckUI.DefaultBackSprite
        // или добавь поле Sprite в ShadowCardData позже
        return data;
    }

    private EventCardData BattleCardToEventCard(BattleCardData battle)
    {
        var data = ScriptableObject.CreateInstance<EventCardData>();
        data.cardTitle   = battle.enemyName;
        data.description = $"Требуемая атака: {battle.requiredAttack}\n" +
                           $"Победа: +{battle.rewardMoney} фелсов\n" +
                           $"Поражение: {battle.penaltyMoney} фелсов";
        data.cardType    = EventCardType.Battle;
        data.difficulty  = battle.requiredAttack;
        return data;
    }

    // ─────────────────────────────────────────────────────────────────────
    // После показа карты
    // ─────────────────────────────────────────────────────────────────────

    private void OnEventCardShown()
    {
        // EndTurnButton уже показывается в DisplayShadowCard через uiManager.
        // Карта остаётся видимой — скрывается при RequestEndTurn (см. ниже).
        Debug.Log("CardManager: карта показана, ожидаем EndTurn.");
    }

    /// <summary>
    /// Вызывать из GameManager.RequestEndTurn() перед переходом к движению.
    /// Скрывает анимированную карту.
    /// </summary>
    public void HideEventCard()
    {
        shadowDeckUI?.HideCurrentCard();
        battleDeckUI?.HideCurrentCard();
    }

    // Обратная совместимость
    private void ApplyCardEffectAndComplete()
    {
        Debug.Log("CardManager: Эффект карты применен. Фаза события завершена.");
        gameManager.CompleteEventPhase();
    }
}