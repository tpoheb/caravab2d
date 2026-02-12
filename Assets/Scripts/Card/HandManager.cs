using UnityEngine;
using System.Collections.Generic;

public class HandManager : MonoBehaviour
{
    public static HandManager Instance { get; private set; }
    [SerializeField] private BattleManager battleManager;

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
                    // Удаляем карту
                    currentHand.RemoveAt(index);
                    RefreshUI();

                    // Просим BattleManager бросить кубик ЗАНОВО
                    // Это обновит lastDiceRoll и пересчитает предварительный итог
                    battleManager.RequestNewRoll(); 
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
        if (handTransform == null || cardPrefab == null) return;

        // 1. Проверяем состояние: если мы в городе, скрываем всю панель
        bool isCity = GameManager.Instance.State == GameState.InCity;
        handTransform.gameObject.SetActive(!isCity);

        // Если мы в городе, дальше ничего рисовать не нужно
        if (isCity) return;

        // 2. Очищаем старые префабы
        foreach (Transform child in handTransform)
        {
            Destroy(child.gameObject);
        }

        // 3. Создаем новые объекты
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