using UnityEngine;
using System.Collections.Generic;

public class City : MonoBehaviour
{
    public string CityName => cityData.cityName;
    public CityData cityData;
    public CityPanel CityPanel; // Индивидуальная панель (опционально)
    [SerializeField] private string cityName; // Название города
    [SerializeField] private List<PathCellInitializer> inCityPaths = new List<PathCellInitializer>(); // Список путей в городе

   
    public List<PathCellInitializer> Paths => inCityPaths;

    void Start()
    {
        InitializeCity();
    }

    // Инициализация города
    private void InitializeCity()
    {
        if (string.IsNullOrEmpty(cityName))
        {
            cityName = "Unnamed City";
        }

        // Инициализируем все пути в городе
        foreach (var path in inCityPaths)
        {
            if (path != null)
            {
                path.InitializeCells();
            }
            else
            {
                Debug.LogWarning($"Обнаружен пустой путь в городе {cityName}");
            }
        }

        Debug.Log($"Город {cityName} инициализирован. Всего путей: {inCityPaths.Count}");
    }


}