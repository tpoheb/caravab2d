using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;
using System;

public class CityPanel : MonoBehaviour
{
    // --- ИЗДАТЕЛЬ ---
    // Событие: Выбран путь. PlayerToken (или Game Manager) подпишется.
    public static event Action<PathCellInitializer> OnPathSelected;
    

    // --- UI/Настройки ---
    [Header("UI Настройки")]
    [SerializeField] private GameObject pathButtonPrefab;
    [SerializeField] private Transform pathButtonsContainer;
    [SerializeField] private Button hireTeamButton;
    [SerializeField] private Button buyGoodsButton;
    [SerializeField] private TMP_Text cityNameText;

    // --- Внутренние данные ---
    private City _currentCity;
    private readonly List<Button> _pathButtons = new List<Button>();

    private void Awake()
    {
        ValidateReferences();
        
    }
    
    public void OpenPanel(City city)
    {
        // Если панель уже активна для этого города, просто выходим (для избежания перестроения UI)
        if (gameObject.activeSelf && _currentCity == city)
            return;
    
        if (city == null) return;

        _currentCity = city;

        BuildPathButtons();
        SetupActionButtons(); 
        
        UpdateCityNameUI(city.CityName);

        gameObject.SetActive(true);
        Debug.Log($"CityPanel открыта для города: {_currentCity.CityName}");
    }
    private void UpdateCityNameUI(string cityName)
    {
        if (cityNameText != null)
        {
            cityNameText.text = cityName ?? "Unknown City";
        }
    }
    private void BuildPathButtons()
    {
        ClearPathButtons();

        if (_currentCity.Paths == null || _currentCity.Paths.Count == 0)
        {
            Debug.Log($"CityPanel: В городе {_currentCity.CityName} нет доступных путей.");
            return;
        }

        for (int i = 0; i < _currentCity.Paths.Count; i++)
        {
            PathCellInitializer path = _currentCity.Paths[i]; 
            
            GameObject buttonObj = Instantiate(pathButtonPrefab, pathButtonsContainer);
            Button pathButton = buttonObj.GetComponent<Button>();

            Text buttonText = pathButton.GetComponentInChildren<Text>();
            if (buttonText != null)
                buttonText.text = $"Путь к {path.FinishCity?.CityName ?? "????"}";

            // Привязываем путь к кнопке.
            pathButton.onClick.AddListener(() => OnPathButtonClicked(path)); 

            _pathButtons.Add(pathButton);
        }
    }
    
    private void SetupActionButtons()
    {
        // Отписка перед подпиской для безопасности
        hireTeamButton.onClick.RemoveAllListeners();
        buyGoodsButton.onClick.RemoveAllListeners();

        // Подписываемся
        hireTeamButton.onClick.AddListener(OnHireTeamClicked);
        buyGoodsButton.onClick.AddListener(OnBuyGoodsClicked);
    }

    private void OnPathButtonClicked(PathCellInitializer path)
    {
        if (path == null) return;

        // --- ИЗДАТЕЛЬ ---
        // Вызываем событие выбора пути! PlayerToken подхватит его.
        OnPathSelected?.Invoke(path);

        ClosePanel();
    }

    private void OnHireTeamClicked()
    {
        // Здесь можно вызвать другое статическое событие, например: 
        // TeamSystem.OnHireTeamRequest?.Invoke(_currentCity);
        Debug.Log($"Наем команды в городе {_currentCity?.CityName}");
    }

    private void OnBuyGoodsClicked()
    {
        if (_currentCity == null) return; 

        // --- ИСПРАВЛЕНИЕ: Вызываем публичный статический метод-помощник ---
        TradeSystem.RequestTrade(_currentCity); 

        ClosePanel();
        // Debug.Log($"Запрос на открытие торговли в городе {_currentCity.CityName}");
    }
    private void ClearPathButtons()
    {
        // Удаляем кнопки в обратном порядке
        for (int i = _pathButtons.Count - 1; i >= 0; i--)
        {
            if (_pathButtons[i] != null)
            {
                _pathButtons[i].onClick.RemoveAllListeners();
                Destroy(_pathButtons[i].gameObject);
            }
        }
        _pathButtons.Clear();
    }
    
    public void ClosePanel()
    {
        gameObject.SetActive(false);
    }
    
    private void ValidateReferences()
    {
        if (cityNameText == null) Debug.LogWarning("CityPanel: Поле City Name Text не назначено!");
    }
    
}