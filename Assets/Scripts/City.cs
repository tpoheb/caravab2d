using UnityEngine;
using System.Collections.Generic;

public class City : MonoBehaviour
{
    // --- ИСПРАВЛЕНИЕ: Прямой доступ к внутреннему полю cityName ---
    // Теперь CityName возвращает поле, которое вы сериализовали (cityData больше не нужен для имени).
    public string CityName => cityName; 
    

    [Header("Данные города")]
    [SerializeField] private string cityName = "Unnamed City"; // Название города (заполняется в Инспекторе)
    
    [SerializeField] 
    private List<PathCellInitializer> inCityPaths = new List<PathCellInitializer>(); // Список путей в городе
    
    // [УБРАТЬ] private CityData cityData; // УДАЛИТЬ, если не используется для других данных

    public List<PathCellInitializer> Paths => inCityPaths;

    void Awake() // Используем Awake для инициализации, чтобы быть уверенными, что CityName доступен в Start у PlayerToken
    {
        InitializeCity(); 
    }

    // Инициализация города
    private void InitializeCity()
    {
        // Проверка и инициализация (лучше, чем просто string.IsNullOrEmpty)
        if (string.IsNullOrWhiteSpace(cityName))
        {
            cityName = gameObject.name; // Использование имени объекта как резервного
            Debug.LogWarning($"Имя города не задано. Используется имя объекта: {cityName}");
        }

        // Инициализируем все пути в городе
        foreach (var path in inCityPaths)
        {
            if (path != null)
            {
                // Предполагается, что InitializeCells() что-то делает с данными пути
                path.InitializeCells();
            }
            else
            {
                Debug.LogWarning($"Обнаружен пустой (null) путь в городе {cityName}");
            }
        }

        Debug.Log($"Город {cityName} инициализирован. Всего путей: {inCityPaths.Count}");
    }
}