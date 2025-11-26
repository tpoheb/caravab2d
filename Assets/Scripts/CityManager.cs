using UnityEngine;
using System.Collections.Generic;


public class CityManager : MonoBehaviour
{
    // --- Зависимости ---
    [Header("Настройки")]
    [SerializeField] private List<City> allCities;

    [Header("Общие UI-панели")]
    [SerializeField] private CityPanel cityPanel; 
    
    private City _lastArrivedCity; // НОВОЕ ПОЛЕ: Храним последний посещенный город

    private void Awake()
    {
        // 1. Подписки - делаем их только здесь.
        PlayerToken.OnPlayerArrivedAtCity += OpenCityPanelFor;
        TradeUIManager.OnTradeClosedRequest += ReOpenCityPanel;
    
        // ВАЖНО: УДАЛИТЕ ValidateReferences() из Awake()
        // Это предотвратит ошибку, если CityPanel еще не полностью загружен.
        // ValidateReferences(); // <-- УДАЛЕНО!
    }
    private void Start()
    {
        // 2. Проверка и валидация внешних ссылок - делаем это только здесь.
        ValidateReferences();
    
        // ВАЖНО: УДАЛИТЕ ПОВТОРНУЮ ПОДПИСКУ!
        //TradeUIManager.OnTradeClosedRequest += ReOpenCityPanel; // <-- УДАЛЕНО!
    }

    private void OnDestroy()
    {
        // Обязательная отписка
        PlayerToken.OnPlayerArrivedAtCity -= OpenCityPanelFor;
        TradeUIManager.OnTradeClosedRequest -= ReOpenCityPanel;
    }

    /// <summary>
    /// Метод-обработчик, который вызывается, когда игрок прибывает в город.
    /// </summary>
    private void OpenCityPanelFor(City city)
    {
        _lastArrivedCity = city; // Сохраняем город
        if (cityPanel != null)
        {
            cityPanel.OpenPanel(city);
        }
        else
        {
            Debug.LogError("CityManager: Общая CityPanel не назначена! UI не будет открыт.");
        }
    }
    /// <summary>
    /// Метод-обработчик, вызываемый после закрытия торговой панели.
    /// </summary>
    private void ReOpenCityPanel()
    {
        if (_lastArrivedCity != null)
        {
            Debug.Log($"CityManager: Повторно открываем панель для города {_lastArrivedCity.CityName}");
            // Вызываем существующий метод открытия панели
            OpenCityPanelFor(_lastArrivedCity); 
        }
        else
        {
            Debug.LogError("CityManager: Невозможно повторно открыть CityPanel, _lastArrivedCity is null.");
        }
    }

    private void ValidateReferences()
    {
        if (cityPanel == null)
        {
            Debug.LogError($"{nameof(CityPanel)} не назначен в {nameof(CityManager)}.");
        }
        if (allCities == null || allCities.Count == 0)
        {
            Debug.LogWarning("Список городов пуст или не назначен.");
        }
    }
}