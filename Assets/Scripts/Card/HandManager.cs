using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// Управляет картами в руке игрока.
///
/// Торговые карты (SaleBonus, IgnoreTax, SalePriceBoost, PurchaseDiscount)
/// публикуют события через TradeCardEvents → TradeCardModifiers.
///
/// Метки фаз и применимость эффекта проверяются и для UI, и для прямого вызова UseCard.
/// Снижение штрафа накапливается в посткомбате и расходуется при завершении хода.
/// </summary>
public partial class HandManager : MonoBehaviour
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

    private float _pendingPenaltyReduction;
    private bool _cityHandOpen;
    private bool _wasInCity;
    public bool IsCityHandOpen => _cityHandOpen;
    public int CardCount => currentHand.Count;
    public float PendingPenaltyReduction => Mathf.Clamp01(_pendingPenaltyReduction);

    public float ConsumePenaltyReduction()
    {
        float reduction = Mathf.Clamp01(_pendingPenaltyReduction);
        _pendingPenaltyReduction = 0f;
        return reduction;
    }

    public void ClearEncounterEffects() => _pendingPenaltyReduction = 0f;

    public bool CanUseCard(int index)
    {
        if (PauseMenuController.IsPaused) return false;
        if (index < 0 || index >= currentHand.Count || currentHand[index] == null) return false;
        var game = GameManager.Instance;
        if (game == null || game.IsChoosingDice) return false;
        var card = currentHand[index];
        if ((card.playTiming & game.CurrentHandTiming) == 0) return false;

        // Метка задаёт окно; дополнительно проверяем, что эффект применим сейчас.
        switch (card.effectType)
        {
            case HandCardData.CardEffectType.Reroll:
                return game.State == GameState.InBattle && battleManager != null &&
                       battleManager.IsActive && battleManager.HasDiceRoll;
            case HandCardData.CardEffectType.AddBonus:
            case HandCardData.CardEffectType.EnemyAttackDebuff:
            case HandCardData.CardEffectType.EscapeBattle:
            case HandCardData.CardEffectType.ChooseDice:
                return (game.State == GameState.PreparingEncounter || game.State == GameState.InBattle) &&
                       battleManager != null && battleManager.IsActive;
            case HandCardData.CardEffectType.BattlePenaltyReduce:
                return game.State == GameState.PostEncounter && battleManager != null && battleManager.IsDefeat;
            case HandCardData.CardEffectType.CancelCard:
                return (game.State == GameState.PreparingEncounter || game.State == GameState.ResolvingEvent) &&
                       CardManager.Instance != null && CardManager.Instance.CurrentCard != null;
            case HandCardData.CardEffectType.CapacityBoost:
            case HandCardData.CardEffectType.GoldBoost:
                return effectManager != null;
            case HandCardData.CardEffectType.DoubleGoods:
                return playerInventory != null && playerInventory.Items.Count > 0;
            default:
                return true;
        }
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

        if (!CanUseCard(index))
        {
            Debug.LogWarning("[HandManager] Карта недоступна в текущей фазе.");
            return;
        }
        ExecuteCardEffect(currentHand[index], index);
        RefreshUI();
    }

    public void DiscardCard(int index)
    {
        if (PauseMenuController.IsPaused) return;
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

    private void ExecuteCardEffect(HandCardData card, int index)
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
                ConsumeCard(index);
                GameManager.Instance.PromptDiceChoice();
                Debug.Log("[HandManager] ChooseDice: игрок выбирает значение кубика.");
                break;

            case HandCardData.CardEffectType.EscapeBattle:
                ConsumeCard(index);
                battleManager.ForceEndBattle(escaped: true);
                Debug.Log("[HandManager] EscapeBattle: бой избегнут.");
                break;

            case HandCardData.CardEffectType.CancelCard:
                ConsumeCard(index);
                CardManager.Instance.CancelCurrentCard();
                Debug.Log("[HandManager] CancelCard: текущая карта отменена.");
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
                ConsumeCard(index);
                battleManager.AddEnemyAttackDebuff(card.value);
                Debug.Log($"[HandManager] EnemyDebuff: -{card.value} к атаке противника.");
                break;

            case HandCardData.CardEffectType.BattlePenaltyReduce:
                _pendingPenaltyReduction += card.value * 0.01f;
                ConsumeCard(index);
                battleManager.RefreshPostCombatPreview();
                Debug.Log($"[HandManager] PenaltyReduce: -{card.value}% штрафа после поражения.");
                break;

            default:
                Debug.LogWarning($"[HandManager] Неизвестный тип эффекта: {card.effectType}");
                break;
        }
    }

    // ── UI ────────────────────────────────────────────────────────────────

    public void ToggleCityHand()
    {
        if (PauseMenuController.IsPaused || GameManager.Instance == null ||
            GameManager.Instance.State != GameState.InCity) return;
        _cityHandOpen = !_cityHandOpen;
        RefreshUI();
    }

    public void RefreshUI()
    {
        bool inCity = GameManager.Instance != null && GameManager.Instance.State == GameState.InCity;
        // Каждый приезд начинает с закрытой руки; обновления карт не закрывают её повторно.
        if (!inCity || !_wasInCity) _cityHandOpen = false;
        _wasInCity = inCity;
        if (handTransform == null) return;

        handTransform.gameObject.SetActive(!inCity || _cityHandOpen);
        if (cardPrefab == null) return;

        foreach (Transform child in handTransform)
        {
            child.gameObject.SetActive(false);
            Destroy(child.gameObject);
        }

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
