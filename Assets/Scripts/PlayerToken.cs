using UnityEngine;
using UnityEngine.UI;
using System;

public class PlayerToken : MonoBehaviour
{
    // --- ИЗДАТЕЛЬ ---
    // Событие: Игрок прибыл в город (публичное статическое событие для подписки UI и менеджеров)
    public static event Action<City> OnPlayerArrivedAtCity; 

    // --- ПОДПИСЧИК ---
    // Подписка на событие выбора пути, которое будет издавать CityPanel.
    // Это заменяет playerToken.SetPath(path) в CityPanel.
    
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
        // Подписки на UI/системы
        endTurnButton.onClick.AddListener(OnEndTurn);
        diceSystem.OnDiceRolled += ApplyDiceEffects;
        
        // --- Устранение прямой зависимости ---
        // Подписываемся на событие выбора пути, которое придет от CityPanel
        CityPanel.OnPathSelected += SetPath; 

        InitializeStartCity();
    }

    private void InitializeStartCity()
    {
        if (startCity != null)
        {
            // Использование оператора ?? для безопасного доступа к CityName
            string cityName = startCity.CityName ?? "Безымянный город"; 
        
            // Теперь Debug.Log не упадет, даже если startCity.CityName == null
            Debug.Log($"PlayerToken: Игрок начинает в {cityName}"); 
        
            // Вызываем событие прибытия
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
        // Игрок выбрал путь, начинаем движение.
        pathController.SetPath(path);
        
        // Закрываем весь UI (CityPanel должен был уже закрыться сам, 
        // но на всякий случай закрываем всю общую панель UI).
        uiHandler.CloseAll(); 
        
        Debug.Log($"PlayerToken: Установлен путь к {path.FinishCity.CityName}");
    }
    
    private void OnEndTurn()
    {
        if (!pathController.HasActivePath())
        {
            diceSystem.RollDice();
            return;
        }

        pathController.Advance();

        if (pathController.IsPathCompleted())
        {
            ArriveAtDestination();
        }
        else
        {
            pathController.MoveCurrent();
            teamSystem.PaySalaries();
            pathController.HandleCurrentCellEffect(uiHandler, playerInventory);
        }
    }

    private void ApplyDiceEffects(int diceResult)
    {
        playerInventory.Money += diceSystem.LastMoneyModifier;
        Debug.Log($"Деньги после броска кубика: {(diceSystem.LastMoneyModifier >= 0 ? "+" : "")}{diceSystem.LastMoneyModifier}");
    }

    private void ArriveAtDestination()
    {
        var finishCity = pathController.CurrentPath?.FinishCity;

        if (finishCity == null)
        {
            Debug.LogWarning("PlayerToken: Город назначения не задан или путь пуст!");
            return;
        }

        pathController.ResetToken();
        uiHandler.CloseAll(); 
        
        // --- ИЗДАТЕЛЬ ---
        // Игрок прибыл, издаем событие. CityPanel подхватит его.
        OnPlayerArrivedAtCity?.Invoke(finishCity);

        Debug.Log($"Игрок достиг города {finishCity.CityName}");
    }

    private void ValidateReferences()
    {
        if (pathController == null) Debug.LogError($"{nameof(PathController)} не назначен!");
        if (diceSystem == null) Debug.LogError($"{nameof(DiceSystem)} не назначен!");
        // ... (остальные проверки)
    }

    private void OnDestroy()
    {
        // Обязательная отписка
        if (diceSystem != null)
        {
            diceSystem.OnDiceRolled -= ApplyDiceEffects;
        }
        // Отписка от события CityPanel
        CityPanel.OnPathSelected -= SetPath;
    }
}