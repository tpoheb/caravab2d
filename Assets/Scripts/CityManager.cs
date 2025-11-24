using UnityEngine;
using System.Collections.Generic;
using System;

public class CityManager : MonoBehaviour
{
    // --- Зависимости ---
    [Header("Настройки")]
    [SerializeField] private List<City> allCities;

    [Header("Общие UI-панели")]
    [SerializeField] private CityPanel cityPanel; 

    private void Start()
    {
        ValidateReferences();

        // Подписка на событие прибытия (PlayerToken - Издатель, CityManager - Подписчик)
        PlayerToken.OnPlayerArrivedAtCity += OpenCityPanelFor;
    }

    private void OnDestroy()
    {
        // --- ИСПРАВЛЕНИЕ: Удаляем проверку на null ---
        // Оператор -= безопасен и всегда должен использоваться для отписки.
        PlayerToken.OnPlayerArrivedAtCity -= OpenCityPanelFor;
    }

    /// <summary>
    /// Метод-обработчик, который вызывается, когда игрок прибывает в город.
    /// </summary>
    private void OpenCityPanelFor(City city)
    {
        if (cityPanel != null)
        {
            cityPanel.OpenPanel(city);
        }
        else
        {
            Debug.LogError("CityManager: Общая CityPanel не назначена! UI не будет открыт.");
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