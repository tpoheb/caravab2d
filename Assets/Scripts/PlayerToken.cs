using UnityEngine;
using System;
using UnityEngine.UI;
using System.Collections.Generic; 

public class PlayerToken : MonoBehaviour
{
    // --- ИЗДАТЕЛЬ ---
    /// <summary>Событие: Игрок прибыл в город.</summary>
    public static event Action<City> OnPlayerArrivedAtCity; 
    /// <summary>Событие: Игрок переместился на клетку.</summary>
    public static event Action OnPlayerMoved; 
    
    /// <summary>Публичное свойство для доступа GameManager к PathController.</summary>
    public PathController PathController => pathController;

    // --- Системы ---
    [Header("Системы")]
    [SerializeField] private PathController pathController;
    [SerializeField] private UIHandler uiHandler; 
    [SerializeField] private TeamSystem teamSystem;
    [SerializeField] private DiceSystem diceSystem; 
    [SerializeField] private PlayerInventory playerInventory; 

    // --- UI/Данные ---
    [Header("UI")]
    [SerializeField] private Button endTurnButton; 
    
    [Header("Стартовые настройки")]
    [SerializeField] private City startCity;

    private void Awake()
    {
        ValidateReferences();
    }

    private void Start()
    {
        // 1. Подписка на UI: Оповещение GameManager о запросе хода.
        endTurnButton.onClick.AddListener(OnEndTurnRequested); 
        
        // 2. Подписка на событие выбора пути от CityPanel
        CityPanel.OnPathSelected += SetPath; 

        InitializeStartCity();
    }
    
    private void OnDestroy()
    {
        // Обязательная отписка
        if (endTurnButton != null)
        {
            endTurnButton.onClick.RemoveListener(OnEndTurnRequested);
        }
        CityPanel.OnPathSelected -= SetPath;
    }

    private void ValidateReferences()
    {
        if (pathController == null) Debug.LogError($"{nameof(PathController)} не назначен!");
        if (diceSystem == null) Debug.LogError($"{nameof(DiceSystem)} не назначен!");
        if (uiHandler == null) Debug.LogError($"{nameof(UIHandler)} не назначен!");
        if (teamSystem == null) Debug.LogError($"{nameof(TeamSystem)} не назначен!");
        if (playerInventory == null) Debug.LogError($"{nameof(PlayerInventory)} не назначен!");
        if (endTurnButton == null) Debug.LogError($"{nameof(endTurnButton)} не назначен!");
    }
    
    private void InitializeStartCity()
    {
        if (startCity != null)
        {
            Debug.Log($"PlayerToken: Игрок начинает в {startCity.CityName}"); 
            OnPlayerArrivedAtCity?.Invoke(startCity); 
        }
        else
        {
            Debug.LogWarning("PlayerToken: Стартовый город не назначен!");
        }
    }

    /// <summary>
    /// Метод, вызываемый по событию OnPathSelected от CityPanel.
    /// </summary>
    public void SetPath(PathCellInitializer path)
    {
        pathController.SetPath(path);
        
        Debug.Log($"PlayerToken: Установлен путь к {path.FinishCity.CityName}");
        
        // Оповещаем GameManager, что движение началось.
        OnPlayerMoved?.Invoke(); 
    }
    
    /// <summary>
    /// ИЗДАТЕЛЬ: Сигнал о том, что игрок хочет завершить текущую фазу.
    /// </summary>
    private void OnEndTurnRequested()
    {
        GameManager.Instance?.HandleEndTurnRequest(); 
    }

    // --- ОСНОВНАЯ ЛОГИКА ДВИЖЕНИЯ (Вызывается из GameManager) ---

    /// <summary>
    /// Продвигает токен на одну клетку по пути.
    /// </summary>
    public void AdvanceToken()
    {
        if (!pathController.HasActivePath())
        {
            Debug.LogWarning("PlayerToken: Попытка движения без активного пути.");
            return;
        }

        pathController.Advance();
        OnPlayerMoved?.Invoke(); 

        if (pathController.IsPathCompleted())
        {
            ArriveAtDestination();
        }
        else
        {
            // 1. Физическое перемещение фишки
            pathController.MoveCurrent();
            
            // 2. Выполнение постоянных эффектов (оплата)
            teamSystem.PaySalaries();
            
            // 3. УДАЛЕНО: ЛОГИКА СОБЫТИЯ
            // pathController.HandleCurrentCellEffect(uiHandler, playerInventory); 
            // События теперь обрабатываются GameManager после броска кубика.
        }
    }

    private void ArriveAtDestination()
    {
        var finishCity = pathController.CurrentPath?.FinishCity;

        if (finishCity == null)
        {
            Debug.LogWarning("PlayerToken: Город назначения не задан или путь пуст!");
            return;
        }

        // 1. Сброс пути для возможности выбора нового
        pathController.ResetToken();
        
        // 2. Издаем событие прибытия (CityPanel подхватит его)
        OnPlayerArrivedAtCity?.Invoke(finishCity);

        Debug.Log($"Игрок достиг города {finishCity.CityName}");
        
        // 3. Оповещаем GameManager о завершении фазы
        GameManager.Instance?.CompleteMovementPhase();
    }
}