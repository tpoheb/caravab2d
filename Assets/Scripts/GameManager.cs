using UnityEngine;
using System.Collections.Generic;
using System; // Добавлено для Action/Debug

// Определяем события кубика
public enum DiceEventType
{
    None = 0,
    Battle,
    ShadowInfluence,
    PeacefulPass
}


public class GameManager : MonoBehaviour
{
    // --- Шаблон Одиночки (Singleton) ---
    public static GameManager Instance { get; private set; }

    [Header("Системы и Зависимости")]
    [SerializeField] private PlayerToken playerToken;
    [SerializeField] private DiceSystem diceSystem;
    [SerializeField] private BattleManager battleManager; 
    [SerializeField] private CardManager cardManager;    

    // --- Состояние Игры ---
    public bool IsPlayerTurnActive { get; private set; } = false;
    
    // --- Фазы Хода ---
    private bool _isEventPhaseActive = false;
    
    private void Awake()
    {
        // Инициализация Одиночки
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        
        // --- Подписка на ключевые события ---
        SubscribeToEvents();
        ValidateReferences(); // Добавим проверку при Awake
    }

    private void Start()
    {
        StartNewTurn();
    }

    private void OnDestroy()
    {
        UnsubscribeFromEvents();
    }
    
    // --- Управление подписками ---

    private void SubscribeToEvents()
    {
        DiceSystem.OnDiceEvent += HandleDiceEvent;
    }

    private void UnsubscribeFromEvents()
    {
        DiceSystem.OnDiceEvent -= HandleDiceEvent;
    }

    // --- Логика Хода ---

    public void StartNewTurn()
    {
        IsPlayerTurnActive = true;
        Debug.Log("--- Начат новый ход игрока ---");
    }

    /// <summary>
    /// ОБРАБОТКА ОСНОВНОГО ЗАПРОСА НА КОНЕЦ ФАЗЫ (Кнопка End Turn).
    /// </summary>
    public void HandleEndTurnRequest()
    {
        if (_isEventPhaseActive)
        {
            Debug.LogWarning("GameManager: Попытка завершить ход во время активного события. Игнорируем.");
            return;
        }
        
        bool hasActivePath = playerToken.PathController.HasActivePath();

        if (hasActivePath)
        {
            // 1. ИГРОК В ПУТИ: Выполняем шаг движения.
            playerToken.AdvanceToken(); 

            // 2. Проверка: Если движение НЕ завершило путь (т.е. мы не прибыли в город).
            // PathController.HasActivePath() должна возвращать true, если AdvanceToken() не завершил путь.
            if (playerToken.PathController.HasActivePath()) 
            {
                 Debug.Log("GameManager: Игрок в пути. Бросаем кубик для определения события.");
                 // Запускаем фазу события.
                 diceSystem.RollDice(); 
            }
            // Если путь завершен, AdvanceToken() вызвал ArriveAtDestination(), 
            // который в свою очередь вызовет CompleteMovementPhase().
        }
        else
        {
            // 3. ИГРОК В ГОРОДЕ: Игнорируем запрос, ждем выбора пути.
            Debug.Log("GameManager: Игрок находится в городе. Бросок кубика не требуется. Ждем выбора пути.");
        }
    }
    
    // --- Обработка Событий Кубика ---
    
    private void HandleDiceEvent(DiceEventType type)
    {
        if (!IsPlayerTurnActive) return;
        
        // Активируем фазу события, чтобы блокировать дальнейшие действия игрока до завершения.
        _isEventPhaseActive = true; 
        Debug.Log($"GameManager: Обработка события кубика: {type}");

        switch (type)
        {
            case DiceEventType.Battle:
                // Передаем управление BattleManager'у. BattleManager должен вызвать CompleteEventPhase() по завершении.
                battleManager?.StartRandomBattle();
                break;

            case DiceEventType.ShadowInfluence:
                // Передаем управление CardManager'у. CardManager должен вызвать CompleteEventPhase() по завершении.
                cardManager?.DrawCard();
                break;

            case DiceEventType.PeacefulPass:
                // Мирный проход - событие завершено, сразу закрываем фазу.
                CompleteEventPhase();
                break;
                
            case DiceEventType.None:
                Debug.LogWarning("Получен пустой тип события.");
                CompleteEventPhase();
                break;
        }
    }
    
    // --- Завершение Фаз ---
    
    /// <summary>
    /// Вызывается после завершения Битвы, розыгрыша Карты или Мирного прохода.
    /// </summary>
    public void CompleteEventPhase()
    {
        _isEventPhaseActive = false;
        Debug.Log("GameManager: Фаза события завершена. Игрок может сделать следующий ход.");
    }

    /// <summary>
    /// Вызывается PlayerToken по завершении движения (прибытие в город).
    /// </summary>
    public void CompleteMovementPhase()
    {
        // Убеждаемся, что любая активная фаза события завершена, если она была.
        _isEventPhaseActive = false; 
        
        Debug.Log("GameManager: Фаза движения завершена. Игрок прибыл в город.");
    }
    
    /// <summary>
    /// Вызывается после того, как игрок завершил все действия за ход.
    /// </summary>
    public void EndPlayerTurn()
    {
        IsPlayerTurnActive = false;
        Debug.Log("Ход игрока завершен.");
        // Здесь можно вызвать StartNewTurn() или передать ход AI/другим игрокам.
    }
    
    // --- Валидация ---

    private void ValidateReferences()
    {
        if (playerToken == null) Debug.LogError($"{nameof(PlayerToken)} не назначен в {nameof(GameManager)}.");
        if (diceSystem == null) Debug.LogError($"{nameof(DiceSystem)} не назначен в {nameof(GameManager)}.");
        // Добавьте другие проверки
    }
}