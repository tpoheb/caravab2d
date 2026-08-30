using UnityEngine;
using System.Collections.Generic;

public class CityManager : MonoBehaviour
{
    [Header("Зависимости")]
    [SerializeField] private CityPanel cityPanel;
    [SerializeField] private PlayerToken playerToken;

    [Header("Данные")]
    [SerializeField] private List<City> allCities;

    private City lastArrivedCity;
    private Dictionary<City, Vector3> _cityPositions = new();

    /// <summary>Текущий город, в котором находится игрок.</summary>
    public City PlayerCurrentCity => lastArrivedCity;

    /// <summary>Список всех городов на сцене (только для чтения).</summary>
    public IReadOnlyList<City> AllCities => allCities;

    /// <summary>Возвращает сохранённую при старте мировую позицию города.</summary>
    public Vector3 GetCityPosition(City city)
        => _cityPositions.TryGetValue(city, out var pos) ? pos : city.transform.position;

    // --------------------
    // ЖИЗНЕННЫЙ ЦИКЛ
    // --------------------

    private void Awake()
    {
        InitializeCities();
        ValidateReferences();
    }

    private void OnEnable()
    {
        Subscribe();
    }

    private void OnDisable()
    {
        Unsubscribe();
    }

    // --------------------
    // ИНИЦИАЛИЗАЦИЯ
    // --------------------

    private void InitializeCities()
    {
        if (allCities == null || allCities.Count == 0) return;

        foreach (City city in allCities)
        {
            if (city != null)
            {
                city.InitRuntimeData();
            }
        }

        Debug.Log("CityManager: Все данные городов успешно скопированы для PlayMode (Runtime).");
    }
    // --------------------
    // ПОДПИСКИ
    // --------------------

    private void Subscribe()
    {
        if (playerToken != null)
            playerToken.OnArrivedAtCity += HandlePlayerArrivedAtCity;
        TradeUIManager.OnTradeClosedRequest += HandleTradeClosed;
    }

    private void Unsubscribe()
    {
        if (playerToken != null)
            playerToken.OnArrivedAtCity -= HandlePlayerArrivedAtCity;
        TradeUIManager.OnTradeClosedRequest -= HandleTradeClosed;
    }

    // --------------------
    // ЛОГИКА
    // --------------------

    private void HandlePlayerArrivedAtCity(City city)
    {
        if (city == null)
        {
            Debug.LogWarning("CityManager: Получен null-город");
            return;
        }
        lastArrivedCity = city;
        Debug.Log($"CityManager: Игрок прибыл в город {city.CityName}");
        OpenCityPanel(city);
    }

    private void HandleTradeClosed()
    {
        if (lastArrivedCity == null)
        {
            Debug.LogWarning("CityManager: Нечего переоткрывать — город не сохранён");
            return;
        }
        Debug.Log($"CityManager: Возврат к панели города {lastArrivedCity.CityName}");
        OpenCityPanel(lastArrivedCity);
    }

    private void OpenCityPanel(City city)
    {
        if (cityPanel == null)
        {
            Debug.LogError("CityManager: CityPanel не назначен");
            return;
        }
        cityPanel.OpenPanel(city);
    }

    // --------------------
    // ВАЛИДАЦИЯ
    // --------------------

    private void ValidateReferences()
    {
        if (cityPanel == null)
            Debug.LogError("CityManager: CityPanel не назначен");
        if (playerToken == null)
            Debug.LogError("CityManager: PlayerToken не назначен");
        if (allCities == null || allCities.Count == 0)
            Debug.LogWarning("CityManager: Список городов пуст или не задан");
    }
}