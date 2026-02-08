using UnityEngine;
using System;
using UnityEngine.UI;

public class PlayerToken : MonoBehaviour
{
    // --- EVENTS ---
    public event Action<City> OnArrivedAtCity;
    public event Action OnStepCompleted; // Срабатывает после каждого успешного шага

    // --- SYSTEMS ---
    [Header("Systems")]
    [SerializeField] private PathController pathController;
    [SerializeField] private TeamSystem teamSystem;

    // --- UI ---
    [Header("UI")]
    [SerializeField] private Button endTurnButton;

    // --- START DATA ---
    [Header("Start Settings")]
    [SerializeField] private City startCity;

    private void Awake()
    {
        ValidateReferences();
    }

    private void Start()
    {
        if (endTurnButton != null)
            endTurnButton.onClick.AddListener(NotifyEndTurnRequest);
            
        InitializeStartCity();
    }

    private void OnDestroy()
    {
        if (endTurnButton != null)
            endTurnButton.onClick.RemoveListener(NotifyEndTurnRequest);
    }

    private void InitializeStartCity()
    {
        if (startCity != null)
        {
            Debug.Log($"PlayerToken: Start in city {startCity.CityName}");
            OnArrivedAtCity?.Invoke(startCity);
        }
    }

    // --------------------
    // PATH CONTROL
    // --------------------

    public void StartPath(PathCellInitializer path)
    {
        if (path == null) return;

        pathController.SetPath(path);
        // Мы не вызываем AdvanceToken здесь, так как это сделает GameManager 
        // через переход в состояние Moving.
    }

    /// <summary>
    /// Основной метод для совершения шага. Вызывается из GameManager.
    /// </summary>
    /// <returns>True, если достигнут город</returns>
    public bool AdvanceToken()
    {
        // 1. Пытаемся сделать шаг в контроллере
        bool pathCompleted = pathController.Step();

        if (pathCompleted)
        {
            ArriveAtDestination();
            return true;
        }

        // 2. Если путь не закончен, обновляем позицию фишки на клетке
        pathController.MoveCurrent();
        
        // 3. Выплачиваем зарплату или применяем эффекты за шаг
        teamSystem?.PaySalaries();

        // 4. Оповещаем системы, что шаг сделан
        OnStepCompleted?.Invoke();
        
        return false;
    }

    private void ArriveAtDestination()
    {
        City finishCity = pathController.CurrentPath?.FinishCity;
        pathController.ResetPath();

        if (finishCity != null)
        {
            Debug.Log($"PlayerToken: Arrived at city {finishCity.CityName}");
            OnArrivedAtCity?.Invoke(finishCity);
        }
    }

    private void NotifyEndTurnRequest()
    {
        GameManager.Instance?.RequestEndTurn();
    }

    private void ValidateReferences()
    {
        if (pathController == null) Debug.LogError("PlayerToken: PathController not assigned.");
        if (teamSystem == null) Debug.LogError("PlayerToken: TeamSystem not assigned.");
        if (endTurnButton == null) Debug.LogError("PlayerToken: EndTurnButton not assigned.");
    }
}