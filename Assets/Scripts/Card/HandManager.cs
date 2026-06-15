using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// Управляет картами в руке игрока.
/// Карты можно сыграть в состояниях InBattle и ResolvingEvent.
/// </summary>
public class HandManager : MonoBehaviour
{
    public static HandManager Instance { get; private set; }

    [Header("Зависимости")]
    [SerializeField] private BattleManager battleManager;
    [SerializeField] private PlayerInventory playerInventory;

    [Header("Настройки руки")]
    [SerializeField] private int maxHandSize = 5;
    [SerializeField] private List<HandCardData> currentHand = new List<HandCardData>();

    [Header("Пул наград")]
    [SerializeField] private List<HandCardData> rewardPool = new List<HandCardData>();

    [Header("UI")]
    [SerializeField] private Transform handTransform;
    [SerializeField] private GameObject cardPrefab;

    // ── Флаги отложенных эффектов ─────────────────────────────────────

    // ChooseDice: GameManager опрашивает этот флаг перед броском кубика пути
    private bool _chooseDiceActive = false;
    public bool ConsumeDiceChoice()
    {
        if (!_chooseDiceActive) return false;
        _chooseDiceActive = false;
        return true;
    }

    // CancelCard: CardManager опрашивает этот флаг перед применением карты
    private bool _cancelNextCard = false;
    public bool ConsumeCancelCard()
    {
        if (!_cancelNextCard) return false;
        _cancelNextCard = false;
        Debug.Log("[HandManager] CancelCard: следующая карта отменена.");
        return true;
    }

    // EscapeBattle: BattleManager опрашивает при старте боя
    private bool _escapeBattleActive = false;
    public bool ConsumeEscapeBattle()
    {
        if (!_escapeBattleActive) return false;
        _escapeBattleActive = false;
        Debug.Log("[HandManager] EscapeBattle: бой пропущен без штрафов.");
        return true;
    }

    // ──────────────────────────────────────────────────────────────────
    // Lifecycle
    // ──────────────────────────────────────────────────────────────────

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
    }

    private void Start() => RefreshUI();

    // ──────────────────────────────────────────────────────────────────
    // Управление картами
    // ──────────────────────────────────────────────────────────────────

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

    /// <summary>
    /// Вызывается при нажатии на карту в руке.
    /// </summary>
    public void UseCard(int index)
    {
        if (index < 0 || index >= currentHand.Count) return;

        GameState state = GameManager.Instance.State;
        bool canUse = state == GameState.InBattle || state == GameState.ResolvingEvent;
        if (!canUse)
        {
            Debug.LogWarning($"[HandManager] Нельзя сыграть карту в состоянии {state}.");
            return;
        }

        HandCardData card = currentHand[index];

        switch (card.effectType)
        {
            // ── Существующие ─────────────────────────────────────────
            case HandCardData.CardEffectType.Reroll:
                RemoveAndRefresh(index);
                battleManager.RequestNewRoll();
                break;

            case HandCardData.CardEffectType.AddBonus:
                RemoveAndRefresh(index);
                battleManager.AddAttackBonus(card.value);
                Debug.Log($"[HandManager] AddBonus: +{card.value} к атаке в этом бою.");
                break;

            case HandCardData.CardEffectType.CapacityBoost:
                RemoveAndRefresh(index);
                // Применяем через ShadowEffectManager или напрямую через PlayerStats
                GameManager.Instance.GetComponent<ShadowEffectManager>()?.ApplyCard(
                    CreateTempShadowCard(ShadowEffectType.Capacity, card.value, 1));
                break;

            case HandCardData.CardEffectType.GoldBoost:
                RemoveAndRefresh(index);
                Debug.Log($"[HandManager] GoldBoost: +{card.value}% к следующей сделке.");
                break;

            // ── Новые ────────────────────────────────────────────────

            case HandCardData.CardEffectType.ChooseDice:
                // Активируем флаг; GameManager покажет UI выбора числа перед броском
                _chooseDiceActive = true;
                RemoveAndRefresh(index);
                GameManager.Instance.PromptDiceChoice(); // реализовать в GameManager
                Debug.Log("[HandManager] ChooseDice: игрок выбирает значение кубика.");
                break;

            case HandCardData.CardEffectType.EscapeBattle:
                _escapeBattleActive = true;
                RemoveAndRefresh(index);
                // BattleManager проверит флаг при следующем PrepareBattle
                // Если бой уже идёт — завершаем немедленно
                if (state == GameState.InBattle)
                    battleManager.ForceEndBattle(escaped: true);
                Debug.Log("[HandManager] EscapeBattle: дымовая завеса активирована.");
                break;

            case HandCardData.CardEffectType.CancelCard:
                _cancelNextCard = true;
                RemoveAndRefresh(index);
                Debug.Log("[HandManager] CancelCard: следующая карта Тени/Битвы будет отменена.");
                break;

            case HandCardData.CardEffectType.DoubleGoods:
                RemoveAndRefresh(index);
                // Показываем UI выбора товара для удвоения
                GameManager.Instance.PromptDoubleGoods(); // реализовать в GameManager
                Debug.Log("[HandManager] DoubleGoods: игрок выбирает товар для удвоения.");
                break;
        }
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

    // ──────────────────────────────────────────────────────────────────
    // UI
    // ──────────────────────────────────────────────────────────────────

    public void RefreshUI()
    {
        if (handTransform == null || cardPrefab == null) return;

        bool isCity = GameManager.Instance.State == GameState.InCity;
        handTransform.gameObject.SetActive(!isCity);
        if (isCity) return;

        foreach (Transform child in handTransform)
            Destroy(child.gameObject);

        for (int i = 0; i < currentHand.Count; i++)
        {
            GameObject obj = Instantiate(cardPrefab, handTransform);
            var slot = obj.GetComponent<CardSlotUI>();
            slot?.Setup(currentHand[i], i);
        }
    }

    // ──────────────────────────────────────────────────────────────────
    // Вспомогательные
    // ──────────────────────────────────────────────────────────────────

    private void RemoveAndRefresh(int index)
    {
        currentHand.RemoveAt(index);
        RefreshUI();
    }

    /// <summary>
    /// Создаёт временный ShadowCardData на лету для применения через ShadowEffectManager.
    /// Нужен для CapacityBoost и подобных — чтобы не дублировать логику откатов.
    /// </summary>
    private ShadowCardData CreateTempShadowCard(ShadowEffectType type, int value, int duration)
    {
        var card = ScriptableObject.CreateInstance<ShadowCardData>();
        card.cardName = $"[HandCard] {type}";
        card.effectType = type;
        card.value = value;
        card.isTemporary = duration > 0;
        card.duration = duration;
        return card;
    }
}