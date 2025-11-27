using UnityEngine;
using System.Collections.Generic;

public class BattleManager : MonoBehaviour
{
    [Header("Зависимости")]
    [SerializeField] private GameManager gameManager;
    [SerializeField] private PlayerStats playerStats; // Ссылка на характеристики игрока (для проверки атаки)
    [SerializeField] private PlayerInventory playerInventory; // Ссылка на инвентарь (для изменения денег)

    [Header("Колода Битв")]
    [Tooltip("Список всех Scriptable Objects карт битвы.")]
    [SerializeField] private List<BattleCardData> battleDeck;
    
    private BattleCardData _currentCard;

    // ... (Awake и ValidateReferences остаются прежними)

    // --- ЛОГИКА ЗАПУСКА И ВЫБОРА КАРТЫ ---

    /// <summary>
    /// Вызывается из GameManager при выпадении события "Битва".
    /// </summary>
    public void StartRandomBattle()
    {
        if (battleDeck == null || battleDeck.Count == 0)
        {
            Debug.LogWarning("BattleManager: Колода карт пуста! Мирный проход.");
            gameManager.CompleteEventPhase();
            return;
        }

        // 1. Выбираем случайную карту
        int randomIndex = Random.Range(0, battleDeck.Count);
        _currentCard = battleDeck[randomIndex];
        
        Debug.Log($"BattleManager: НАЧАЛО БИТВЫ. Враг: {_currentCard.enemyName}");
        
        // 2. Сразу обрабатываем результат (в реальной игре здесь будет UI и ожидание)
        ProcessBattleResult(); 
    }

    // --- ЛОГИКА ОБРАБОТКИ РЕЗУЛЬТАТА ---

    private void ProcessBattleResult()
    {
        if (_currentCard == null) return;

        // Получаем значение атаки игрока (предполагаем, что у PlayerStats есть поле Attack)
        int playerAttack = playerStats.Attack; 
        
        bool isVictory = playerAttack >= _currentCard.requiredAttack;

        if (isVictory)
        {
            ApplyVictoryEffect();
        }
        else
        {
            ApplyDefeatEffect();
        }
        
        // Завершаем фазу события, чтобы ход мог продолжиться
        EndBattle(); 
    }

    private void ApplyVictoryEffect()
    {
        // 1. Применяем награду
        playerInventory.Money += _currentCard.rewardMoney;
        Debug.Log($"ПОБЕДА! Получено {_currentCard.rewardMoney} фелсов. Новое золото: {playerInventory.Money}");
    }

    private void ApplyDefeatEffect()
    {
        // 1. Применяем штраф
        playerInventory.Money += _currentCard.penaltyMoney;
        Debug.Log($"ПОРАЖЕНИЕ! Потеряно {Mathf.Abs(_currentCard.penaltyMoney)} фелсов. Новое золото: {playerInventory.Money}");
    }

    /// <summary>
    /// Вызывается по завершении боевой сцены/обработки.
    /// </summary>
    public void EndBattle()
    {
        _currentCard = null;
        Debug.Log("BattleManager: Битва завершена. Возврат управления GameManager.");

        // Оповещаем GameManager, что фаза события завершена.
        gameManager.CompleteEventPhase();
    }
}