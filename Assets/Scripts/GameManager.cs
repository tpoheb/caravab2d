using System.Collections.Generic;
using UnityEngine;

public class GameManager : MonoBehaviour
{
    public Transform player; // Фишка игрока
    public List<Path> allPaths; // Все пути на поле
    public List<CityCell> allCities; // Все города на поле
    public CityCell startCity; // Стартовый город

    private List<Transform> currentPathCells; // Текущий путь (список клеток)
    private int currentCellIndex = 0; // Текущая клетка в пути
    private CityCell _currentCity; // Приватное поле для текущего города
    public CityCell CurrentCity // Публичное свойство
    {
        get => _currentCity;
        private set => _currentCity = value;
    }
    private bool isMovingForward = true; // Направление движения (вперед или назад)

    private void Start()
    {
        // Инициализация стартового города
        if (startCity != null)
        {
            CurrentCity = startCity;
            startCity.OnPlayerEnter(); // Открываем панель стартового города
        }
        else
        {
            Debug.LogError("Стартовый город не назначен!");
        }
    }

    // Вызывается при нажатии на кнопку "Конец хода"
    public void EndTurn()
    {
        if (CurrentCity != null)
        {
            Debug.Log("Игрок находится в городе. Выберите путь.");
            return;
        }

        // Проверяем, что currentPathCells инициализирован и не пуст
        if (currentPathCells == null || currentPathCells.Count == 0)
        {
            Debug.LogError("Список клеток пути не инициализирован или пуст!");
            return;
        }

        // Проверяем, что currentCellIndex не выходит за пределы списка
        if (currentCellIndex < currentPathCells.Count - 1)
        {
            currentCellIndex++;
            MovePlayerToCell(currentPathCells[currentCellIndex]);
            CheckCellType();
        }
        else
        {
            Debug.Log("Игрок достиг конца пути.");
            ArriveAtCity();
        }
    }

    // Перемещает фишку на указанную клетку
    private void MovePlayerToCell(Transform cell)
    {
        player.position = cell.position;
    }

    // Проверяет тип клетки (обычная или город)
    private void CheckCellType()
    {
        CityCell cityCell = currentPathCells[currentCellIndex].GetComponent<CityCell>();
        if (cityCell != null)
        {
            ArriveAtCity(cityCell);
        }
    }

    // Обрабатывает прибытие в город
    private void ArriveAtCity(CityCell city = null)
    {
        if (city == null)
        {
            city = currentPathCells[currentCellIndex].GetComponent<CityCell>();
        }

        if (city != null)
        {
            CurrentCity = city;
            city.OnPlayerEnter();
        }
    }

    // Вызывается при выборе пути в UI города
    public void ChoosePath(int pathIndex, bool isForward)
    {
        if (CurrentCity != null && pathIndex >= 0 && pathIndex < CurrentCity.availablePaths.Count)
        {
            Path chosenPath = CurrentCity.availablePaths[pathIndex];
            StartNewPath(chosenPath, isForward);
            CurrentCity.CloseCityPanel(); // Закрываем панель города
            CurrentCity = null; // Игрок покинул город
        }
        else
        {
            Debug.Log("Неверный выбор пути.");
        }
    }

    // Начинает новый путь
    private void StartNewPath(Path path, bool isForward)
    {
        if (path == null || path.cells == null || path.cells.Count == 0)
        {
            Debug.LogError("Путь не инициализирован или не содержит клеток!");
            return;
        }

        currentPathCells = isForward ? path.cells : path.GetReversedCells();
        currentCellIndex = 0; // Начинаем с первой клетки нового пути
        MovePlayerToCell(currentPathCells[currentCellIndex]);
    }
}