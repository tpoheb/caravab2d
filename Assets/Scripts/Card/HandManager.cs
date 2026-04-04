using UnityEngine;
using System.Collections.Generic;

public class HandManager : MonoBehaviour
{
    public static HandManager Instance { get; private set; }

    [SerializeField] private BattleManager battleManager;

    [Header("Настройки руки")]
    [SerializeField] private int maxHandSize = 5;
    [SerializeField] private List<HandCardData> currentHand = new List<HandCardData>();

    [Header("Пул наград")]
    [SerializeField] private List<HandCardData> rewardPool = new List<HandCardData>();

    [Header("UI")]
    [SerializeField] private Transform handTransform;
    [SerializeField] private GameObject cardPrefab;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }

    private void Start() => RefreshUI();

    // --------------------
    // ЛОГИКА КАРТ
    // --------------------

    public bool AddCard(HandCardData card)
    {
        if (card == null) return false;

        if (currentHand.Count >= maxHandSize)
        {
            Debug.LogWarning("HandManager: Рука полна!");
            return false;
        }

        currentHand.Add(card);
        Debug.Log($"HandManager: Добавлена карта {card.cardName}");
        RefreshUI();
        return true;
    }

    /// <summary>
    /// Вызывается при нажатии игрока на карту в руке.
    /// Карты можно играть в любой момент хода (ResolvingEvent или InBattle).
    /// </summary>
    public void UseCard(int index)
    {
        if (index < 0 || index >= currentHand.Count) return;

        GameState state = GameManager.Instance.State;

        // Карты руки доступны в бою и после его завершения (пока ход не закрыт)
        bool canUseCard = state == GameState.InBattle || state == GameState.ResolvingEvent;
        if (!canUseCard)
        {
            Debug.LogWarning($"HandManager: Карту нельзя сыграть в состоянии {state}");
            return;
        }

        HandCardData card = currentHand[index];

        switch (card.effectType)
        {
            case HandCardData.CardEffectType.Reroll:
                // Удаляем карту из руки и запрашиваем переброс
                currentHand.RemoveAt(index);
                RefreshUI();
                battleManager.RequestNewRoll();
                break;

            // Сюда добавляем новые эффекты по мере расширения:
            // case HandCardData.CardEffectType.AddBonus:
            //     ApplyAttackBonus(card.value);
            //     currentHand.RemoveAt(index);
            //     RefreshUI();
            //     break;
        }
    }

    public void DiscardCard(int index)
    {
        if (index < 0 || index >= currentHand.Count) return;

        Debug.Log($"HandManager: Сброшена карта {currentHand[index].cardName}");
        currentHand.RemoveAt(index);
        RefreshUI();
    }

    public void GiveRandomReward()
    {
        if (rewardPool == null || rewardPool.Count == 0)
        {
            Debug.LogError("HandManager: Reward Pool пуст!");
            return;
        }

        AddCard(rewardPool[Random.Range(0, rewardPool.Count)]);
    }

    // --------------------
    // UI
    // --------------------

    public void RefreshUI()
    {
        if (handTransform == null || cardPrefab == null) return;

        // Скрываем руку в городе
        bool isCity = GameManager.Instance.State == GameState.InCity;
        handTransform.gameObject.SetActive(!isCity);
        if (isCity) return;

        // Пересоздаём карты
        foreach (Transform child in handTransform)
            Destroy(child.gameObject);

        for (int i = 0; i < currentHand.Count; i++)
        {
            GameObject obj = Instantiate(cardPrefab, handTransform);
            CardSlotUI slot = obj.GetComponent<CardSlotUI>();
            slot?.Setup(currentHand[i], i);
        }
    }
}