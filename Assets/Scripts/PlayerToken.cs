using UnityEngine;
using UnityEngine.UI;

public class PlayerToken : MonoBehaviour
{
    [SerializeField] private CityPanel cityPanel; // Панель города
    [SerializeField] private Button endTurnButton; // Кнопка "Конец хода"
    [SerializeField] private GameObject tokenObject; // Объект фишки игрока

    private PathCellInitializer currentPath; // Текущий путь
    private int currentCellIndex = -1; // Индекс текущей клетки (-1 = нет пути)
    private Cell[] pathCells; // Клетки текущего пути

    void Start()
    {
        endTurnButton.onClick.AddListener(OnEndTurn);
        tokenObject.SetActive(false); // Фишка скрыта до выбора пути
    }

    // Установка пути и начало движения
    public void SetPath(PathCellInitializer path)
    {
        currentPath = path;
        currentCellIndex = 0; // Начинаем с первой клетки

        // Получаем все клетки пути
        pathCells = new Cell[path.transform.childCount];
        for (int i = 0; i < path.transform.childCount; i++)
        {
            pathCells[i] = path.transform.GetChild(i).GetComponent<Cell>();
        }

        // Помещаем фишку на первую клетку
        MoveToCell(currentCellIndex);
        tokenObject.SetActive(true);
    }

    // Перемещение на следующую клетку при нажатии "Конец хода"
    private void OnEndTurn()
    {
        if (currentPath == null || currentCellIndex < 0) return;

        currentCellIndex++;

        // Проверяем, достиг ли игрок конца пути
        if (currentCellIndex >= pathCells.Length)
        {
            // Открываем панель города финиша
            if (currentPath.FinishCity != null)
            {
                cityPanel.OpenPanel(currentPath.FinishCity);
                Debug.Log($"Игрок достиг города {currentPath.FinishCity.CityName}");
            }
            else
            {
                Debug.LogWarning("Город финиша не задан для этого пути!");
            }

            // Сбрасываем путь и скрываем фишку
            currentPath = null;
            currentCellIndex = -1;
            tokenObject.SetActive(false);
        }
        else
        {
            // Перемещаем фишку на следующую клетку
            MoveToCell(currentCellIndex);
        }
    }

    // Перемещение фишки на указанную клетку
    private void MoveToCell(int cellIndex)
    {
        if (cellIndex >= 0 && cellIndex < pathCells.Length)
        {
            tokenObject.transform.position = pathCells[cellIndex].Position;
            Debug.Log($"Фишка перемещена на клетку {cellIndex}");
        }
    }
}