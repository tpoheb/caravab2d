using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// Управляет картами в руке игрока.
///
/// Торговые карты (SaleBonus, IgnoreTax, SalePriceBoost, PurchaseDiscount)
/// публикуют события через TradeCardEvents → TradeCardModifiers.
///
/// Боевые карты используют Consume-паттерн:
/// BattleManager опрашивает ConsumeEnemyDebuff() и ConsumePenaltyReduction().
/// </summary>
public class HandManager : MonoBehaviour
{
    public static HandManager Instance { get; private set; }

    [Header("Зависимости")]
    [SerializeField] private BattleManager       battleManager;
    [SerializeField] private ShadowEffectManager effectManager;
    [SerializeField] private PlayerInventory     playerInventory;

    [Header("Настройки руки")]
    [SerializeField] private int                maxHandSize = 5;
    [SerializeField] private List<HandCardData> currentHand = new List<HandCardData>();

    [Header("Пул наград")]
    [SerializeField] private List<HandCardData> rewardPool = new List<HandCardData>();

    [Header("UI")]
    [SerializeField] private Transform  handTransform;
    [SerializeField] private GameObject cardPrefab;

    // ── Флаги отложенных эффектов ────────────────────────────────────────

    // Существующие
    private bool _chooseDiceActive   = false;
    private bool _cancelNextCard     = false;
    private bool _escapeBattleActive = false;

    // Новые боевые
    private int   _pendingEnemyDebuff      = 0;   // Пыль в глаза
    private float _pendingPenaltyReduction = 0f;  // Клятва Пути (0..1)

    // ── Consume-методы (существующие) ────────────────────────────────────

    public bool ConsumeDiceChoice()
    {
        if (!_chooseDiceActive) return false;
        _chooseDiceActive = false;
        return true;
    }

    public bool ConsumeCancelCard()
    {
        if (!_cancelNextCard) return false;
        _cancelNextCard = false;
        Debug.Log("[HandManager] CancelCard: следующая карта отменена.");
        return true;
    }

    public bool ConsumeEscapeBattle()
    {
        if (!_escapeBattleActive) return false;
        _escapeBattleActive = false;
        Debug.Log("[HandManager] EscapeBattle: бой пропущен без штрафов.");
        return true;
    }

    // ── Consume-методы (новые боевые) ────────────────────────────────────

    /// <summary>
    /// BattleManager опрашивает до сравнения атак.
    /// Возвращает накопленный дебафф атаки противника и сбрасывает его.
    /// </summary>
    public int ConsumeEnemyDebuff()
    {
        int v = _pendingEnemyDebuff;
        _pendingEnemyDebuff = 0;
        if (v != 0) Debug.Log($"[HandManager] EnemyDebuff применён: -{v} к атаке врага.");
        return v;
    }

    /// <summary>
    /// BattleManager опрашивает после поражения, до начисления штрафа.
    /// Возвращает множитель снижения (0..1) и сбрасывает его.
    /// </summary>
    public float ConsumePenaltyReduction()
    {
        float v = _pendingPenaltyReduction;
        _pendingPenaltyReduction = 0f;
        if (v > 0f) Debug.Log($"[HandManager] PenaltyReduction применён: -{v:P0} штрафа.");
        return v;
    }

    // ── Lifecycle ─────────────────────────────────────────────────────────

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
    }

    private void Start() => RefreshUI();

    // ── Управление картами ────────────────────────────────────────────────

    public bool AddCard(HandCardData card)
    {
        if (card == null) return false;
        if (currentHand.Count >= maxHandSize)
        {
            Debug.LogWarning("[HandManager] Рука полна!");
            return false;
        }
        currentHand.Add(card);
        Debug.Log($"[HandManager] Добавлена карта '{card.cardName}'.");
        RefreshUI();
        return true;
    }

    public void UseCard(int index)
    {
        if (index < 0 || index >= currentHand.Count) return;

        GameState state = GameManager.Instance.State;
        if (state != GameState.InBattle && state != GameState.ResolvingEvent && state != GameState.InCity)
        {
            Debug.LogWarning($"[HandManager] Нельзя сыграть карту в состоянии {state}.");
            return;
        }

        ExecuteCardEffect(currentHand[index], index, state);
    }

    public void DiscardCard(int index)
    {
        if (index < 0 || index >= currentHand.Count) return;
        Debug.Log($"[HandManager] Сброшена карта '{currentHand[index].cardName}'.");
        currentHand.RemoveAt(index);
        RefreshUI();
    }

    public void GiveRandomReward()
    {
        if (rewardPool == null || rewardPool.Count == 0)
        {
            Debug.LogError("[HandManager] Пул наград пуст!");
            return;
        }
        AddCard(rewardPool[Random.Range(0, rewardPool.Count)]);
    }

    // ── Диспетчер эффектов ────────────────────────────────────────────────

    private void ExecuteCardEffect(HandCardData card, int index, GameState state)
    {
        switch (card.effectType)
        {
            // ── Существующие боевые/тактические ──────────────────────────

            case HandCardData.CardEffectType.Reroll:
                ConsumeCard(index);
                battleManager.RequestNewRoll();
                break;

            case HandCardData.CardEffectType.AddBonus:
                ConsumeCard(index);
                battleManager.AddAttackBonus(card.value);
                Debug.Log($"[HandManager] AddBonus: +{card.value} к атаке команды.");
                break;

            case HandCardData.CardEffectType.CapacityBoost:
                ConsumeCard(index);
                effectManager.ApplyTransientCard(ShadowEffectType.Capacity, card.value, duration: 1);
                Debug.Log($"[HandManager] CapacityBoost: +{card.value} к грузоподъёмности на 1 ход.");
                break;

            case HandCardData.CardEffectType.GoldBoost:
                ConsumeCard(index);
                effectManager.ApplyTransientCard(ShadowEffectType.BonusTrade, card.value, duration: 0);
                Debug.Log($"[HandManager] GoldBoost: +{card.value}% к следующей сделке.");
                break;

            case HandCardData.CardEffectType.ChooseDice:
                _chooseDiceActive = true;
                ConsumeCard(index);
                GameManager.Instance.PromptDiceChoice();
                Debug.Log("[HandManager] ChooseDice: игрок выбирает значение кубика.");
                break;

            case HandCardData.CardEffectType.EscapeBattle:
                _escapeBattleActive = true;
                ConsumeCard(index);
                if (state == GameState.InBattle)
                    battleManager.ForceEndBattle(escaped: true);
                Debug.Log("[HandManager] EscapeBattle: бой избегнут.");
                break;

            case HandCardData.CardEffectType.CancelCard:
                _cancelNextCard = true;
                ConsumeCard(index);
                Debug.Log("[HandManager] CancelCard: следующая карта отменена.");
                break;

            case HandCardData.CardEffectType.DoubleGoods:
                ConsumeCard(index);
                GameManager.Instance.PromptDoubleGoods();
                Debug.Log("[HandManager] DoubleGoods: выбор товара для удвоения.");
                break;

            // ── Новые торговые (публикуют событие) ───────────────────────

            case HandCardData.CardEffectType.SaleBonus:
                ConsumeCard(index);
                TradeCardEvents.SaleBonusActivated(card.value);
                Debug.Log($"[HandManager] SaleBonus: +{card.value} монет к продаже.");
                break;

            case HandCardData.CardEffectType.IgnoreTax:
                ConsumeCard(index);
                TradeCardEvents.IgnoreTaxActivated();
                Debug.Log("[HandManager] IgnoreTax: пошлина будет проигнорирована.");
                break;

            case HandCardData.CardEffectType.SalePriceBoost:
                ConsumeCard(index);
                TradeCardEvents.SalePriceBoostActivated(card.value * 0.01f);
                Debug.Log($"[HandManager] SalePriceBoost: +{card.value}% к цене продажи.");
                break;

            case HandCardData.CardEffectType.PurchaseDiscount:
                ConsumeCard(index);
                TradeCardEvents.PurchaseDiscountActivated(card.value * 0.01f);
                Debug.Log($"[HandManager] PurchaseDiscount: -{card.value}% к цене покупки.");
                break;

            // ── Новые боевые (Consume-паттерн) ───────────────────────────

            case HandCardData.CardEffectType.EnemyAttackDebuff:
                _pendingEnemyDebuff += card.value;
                ConsumeCard(index);
                Debug.Log($"[HandManager] EnemyDebuff: -{card.value} к атаке противника.");
                break;

            case HandCardData.CardEffectType.BattlePenaltyReduce:
                _pendingPenaltyReduction += card.value * 0.01f;
                ConsumeCard(index);
                Debug.Log($"[HandManager] PenaltyReduce: -{card.value}% штрафа после поражения.");
                break;

            default:
                Debug.LogWarning($"[HandManager] Неизвестный тип эффекта: {card.effectType}");
                break;
        }
    }

    // ── UI ────────────────────────────────────────────────────────────────

    public void RefreshUI()
    {
        if (handTransform == null || cardPrefab == null) return;

        bool isCity = GameManager.Instance != null && GameManager.Instance.State == GameState.InCity;
        handTransform.gameObject.SetActive(!isCity);
        if (isCity) return;

        foreach (Transform child in handTransform)
            Destroy(child.gameObject);

        for (int i = 0; i < currentHand.Count; i++)
        {
            GameObject obj  = Instantiate(cardPrefab, handTransform);
            var        slot = obj.GetComponent<CardSlotUI>();
            slot?.Setup(currentHand[i], i);
        }
    }

    // ── Вспомогательные ───────────────────────────────────────────────────

    private void ConsumeCard(int index)
    {
        currentHand.RemoveAt(index);
        RefreshUI();
    }
}