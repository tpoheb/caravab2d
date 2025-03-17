using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class GameManager : MonoBehaviour
{
    [Header("Game Data")]
    public List<City> cities; // Все города в игре
    public GameObject playerToken; // Фишка игрока

    [Header("UI Components")]
    public Button endTurnButton; // Кнопка "Конец хода"
    public CityPanelManager cityPanelManager; // Менеджер панелей городов

    private City currentCity; // Текущий город игрока
    private Direction currentDirection; // Текущее направление
    private int currentTileIndex = 0; // Индекс текущей клетки

    void Start()
    {
        // Начинаем игру в первом городе
        if (cities.Count > 0)
        {
            currentCity = cities[0];
            OpenCityPanel(currentCity);
        }

        // Подписываемся на событие нажатия кнопки "Конец хода"
        if (endTurnButton != null)
        {
            endTurnButton.onClick.AddListener(OnEndTurn);
        }
    }

    // Открывает панель действий города
    private void OpenCityPanel(City city)
    {
        if (city.cityPanel != null && cityPanelManager != null)
        {
            city.cityPanel.SetActive(true);
            // Обновляем кнопки направлений через CityPanelManager
            cityPanelManager.UpdateDirectionButtons(city, SelectDirection);
        }
    }

    // Закрывает панель действий города
    private void CloseCityPanel(City city)
    {
        if (city.cityPanel != null)
        {
            city.cityPanel.SetActive(false);
        }
    }

    // Выбор направления
    public void SelectDirection(Direction direction)
    {
        Debug.Log("Выбрано направление: " + direction.destinationCity.cityName);
        currentDirection = direction;
        CloseCityPanel(currentCity);
        StartMoving();
    }

    // Начало движения по выбранному направлению
    private void StartMoving()
    {
        currentTileIndex = 0;
        MoveToNextTile();
    }

    // Перемещение на следующую клетку
    private void MoveToNextTile()
    {
        if (currentDirection != null && currentTileIndex < currentDirection.tiles.Count)
        {
            // Логика перемещения фишки (например, изменение спрайта или анимации)
            Debug.Log("Перемещение на клетку: " + currentDirection.tiles[currentTileIndex].tileName);
            currentTileIndex++;
        }
        else
        {
            // Достигли города назначения
            ArriveAtDestination();
        }
    }

    // Прибытие в город назначения
    private void ArriveAtDestination()
    {
        if (currentDirection != null)
        {
            currentCity = currentDirection.destinationCity;
            OpenCityPanel(currentCity);
            currentDirection = null;
        }
    }

    // Обработка нажатия кнопки "Конец хода"
    private void OnEndTurn()
    {
        if (currentDirection != null)
        {
            MoveToNextTile();
        }
    }
}