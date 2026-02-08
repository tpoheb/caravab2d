using UnityEngine;
using System.Collections.Generic;

public class HandManager : MonoBehaviour
{
    public static HandManager Instance { get; private set; }

    [Header("Настройки руки")]
    [SerializeField] private int maxHandSize = 5;
    [SerializeField] private List<HandCardData> currentHand = new List<HandCardData>();
    
    [Header("Пул наград (заполнить в инспекторе)")]
    [SerializeField] private List<HandCardData> rewardPool = new List<HandCardData>();

    [Header("UI Интеграция")]
    [SerializeField] private Transform handTransform; // Объект с Horizontal Layout Group
    [SerializeField] private GameObject cardPrefab;    // Префаб карты с компонентом CardSlotUI

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }

    private void Start()
    {
        RefreshUI();
    }

    // --------------------
    // ЛОГИКА КАРТ
    // --------------------

    public bool AddCard(HandCardData card)
    {
        if (card == null) return false;

        if (currentHand.Count >= maxHandSize)
        {
            Debug.LogWarning("HandManager: Рука полна! Сбросьте карту.");
            // Тут можно вызвать UI-сообщение для игрока
            return false;
        }

        currentHand.Add(card);
        Debug.Log($"HandManager: Добавлена карта {card.cardName}");
        
        RefreshUI();
        return true;
    }

    public void UseCard(int index)
    {
        if (index < 0 || index >= currentHand.Count) return;

        HandCardData card = currentHand[index];
        bool wasUsed = false;

        // Обработка эффектов
        switch (card.effectType)
        {
            case HandCardData.CardEffectType.Reroll:
                if (GameManager.Instance.State == GameState.InBattle)
                {
                    Debug.Log("HandManager: Применен ПЕРЕБРОС");
                    GameManager.Instance.RequestBattleDiceRoll();
                    wasUsed = true;
                }
                else
                {
                    Debug.Log("HandManager: Переброс доступен только в бою!");
                }
                break;

            // Сюда добавим новые кейсы (AddBonus, GoldBoost и т.д.)
        }

        if (wasUsed)
        {
            currentHand.RemoveAt(index);
            RefreshUI();
        }
    }

    public void DiscardCard(int index)
    {
        if (index >= 0 && index < currentHand.Count)
        {
            Debug.Log($"HandManager: Сброшена карта {currentHand[index].cardName}");
            currentHand.RemoveAt(index);
            RefreshUI();
        }
    }

    // Метод для BattleManager
    public void GiveRandomReward()
    {
        if (rewardPool == null || rewardPool.Count == 0)
        {
            Debug.LogError("HandManager: Список Reward Pool пуст!");
            return;
        }

        int randomIndex = Random.Range(0, rewardPool.Count);
        AddCard(rewardPool[randomIndex]);
    }

    // --------------------
    // ОБНОВЛЕНИЕ UI
    // --------------------

    public void RefreshUI()
    {
        if (handTransform == null || cardPrefab == null)
        {
            Debug.LogWarning("HandManager: Ссылки на UI не назначены в инспекторе.");
            return;
        }

        // Очищаем старые префабы
        foreach (Transform child in handTransform)
        {
            Destroy(child.gameObject);
        }

        // Создаем новые префабы для текущих карт
        for (int i = 0; i < currentHand.Count; i++)
        {
            GameObject newCardObj = Instantiate(cardPrefab, handTransform);
            CardSlotUI slot = newCardObj.GetComponent<CardSlotUI>();
            
            if (slot != null)
            {
                slot.Setup(currentHand[i], i);
            }
        }
    }
}