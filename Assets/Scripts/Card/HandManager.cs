using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// Управляет картами в руке игрока.
/// Карты можно сыграть в состояниях InBattle и ResolvingEvent.
///
/// Изменения по сравнению с исходником:
/// — Убрано GameManager.Instance.GetComponent&lt;ShadowEffectManager&gt;() — прямая ссылка.
/// — Флаги отложенных эффектов вынесены в отдельный регион для читаемости.
/// — UseCard разбит на небольшие методы по одному эффекту.
/// </summary>
public class HandManager : MonoBehaviour
{
    public static HandManager Instance { get; private set; }

    [Header("Зависимости")]
    [SerializeField] private BattleManager       battleManager;
    [SerializeField] private ShadowEffectManager effectManager;   // ← прямая ссылка (было GetComponent)
    [SerializeField] private PlayerInventory     playerInventory;

    [Header("Настройки руки")]
    [SerializeField] private int                  maxHandSize = 5;
    [SerializeField] private List<HandCardData>   currentHand = new List<HandCardData>();

    [Header("Пул наград (карты после победы в бою)")]
    [SerializeField] private List<HandCardData>   rewardPool  = new List<HandCardData>();

    [Header("UI")]
    [SerializeField] private Transform  handTransform;
    [SerializeField] private GameObject cardPrefab;

    // ── Флаги отложенных эффектов ─────────────────────────────────────────

    private bool _chooseDiceActive    = false;
    private bool _cancelNextCard      = false;
    private bool _escapeBattleActive  = false;

    /// <summary>Игрок активировал ChooseDice — GameManager покажет UI выбора числа.</summary>
    public bool ConsumeDiceChoice()
    {
        if (!_chooseDiceActive) return false;
        _chooseDiceActive = false;
        return true;
    }

    /// <summary>CardManager опрашивает перед применением вытянутой карты.</summary>
    public bool ConsumeCancelCard()
    {
        if (!_cancelNextCard) return false;
        _cancelNextCard = false;
        Debug.Log("[HandManager] CancelCard: следующая карта отменена.");
        return true;
    }

    /// <summary>BattleManager опрашивает при старте боя.</summary>
    public bool ConsumeEscapeBattle()
    {
        if (!_escapeBattleActive) return false;
        _escapeBattleActive = false;
        Debug.Log("[HandManager] EscapeBattle: бой пропущен без штрафов.");
        return true;
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

    /// <summary>Вызывается при нажатии на карту в руке.</summary>
    public void UseCard(int index)
    {
        if (index < 0 || index >= currentHand.Count) return;

        GameState state = GameManager.Instance.State;
        if (state != GameState.InBattle && state != GameState.ResolvingEvent)
        {
            Debug.LogWarning($"[HandManager] Нельзя сыграть карту в состоянии {state}.");
            return;
        }

        HandCardData card = currentHand[index];
        ExecuteCardEffect(card, index, state);
    }

    public void DiscardCard(int index)
    {
        if (index < 0 || index >= currentHand.Count) return;
        Debug.Log($"[HandManager] Сброшена карта '{currentHand[index].cardName}'.");
        currentHand.RemoveAt(index);
        RefreshUI();
    }

    /// <summary>Выдать случайную карту из пула наград (вызывается после победы в бою).</summary>
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
            case HandCardData.CardEffectType.Reroll:
                ConsumeCard(index);
                battleManager.RequestNewRoll();
                break;

            case HandCardData.CardEffectType.AddBonus:
                ConsumeCard(index);
                battleManager.AddAttackBonus(card.value);
                Debug.Log($"[HandManager] AddBonus: +{card.value} к атаке в текущем бою.");
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
                Debug.Log("[HandManager] EscapeBattle: дымовая завеса активирована.");
                break;

            case HandCardData.CardEffectType.CancelCard:
                _cancelNextCard = true;
                ConsumeCard(index);
                Debug.Log("[HandManager] CancelCard: следующая карта Тени/Битвы будет отменена.");
                break;

            case HandCardData.CardEffectType.DoubleGoods:
                ConsumeCard(index);
                GameManager.Instance.PromptDoubleGoods();
                Debug.Log("[HandManager] DoubleGoods: игрок выбирает товар для удвоения.");
                break;

            default:
                Debug.LogWarning($"[HandManager] Неизвестный тип эффекта: {card.effectType}");
                break;
        }
    }

    // ── UI ───────────────────────────────────────────────────────────────

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

    // ── Вспомогательные ──────────────────────────────────────────────────

    private void ConsumeCard(int index)
    {
        currentHand.RemoveAt(index);
        RefreshUI();
    }
}
