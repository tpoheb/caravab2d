using UnityEngine;
using UnityEngine.UI;

public class PlayerToken : MonoBehaviour
{
    [Header("UI References")]

    [SerializeField] private Button endTurnButton;
    [SerializeField] private GameObject tokenObject;
    [SerializeField] private TeamSystem teamSystem;

    [Header("Game References")]
    [SerializeField] private CityManager cityManager;

    private PathCellInitializer currentPath;
    private int currentCellIndex = -1;
    private Cell[] pathCells;

    void Start()
    {
        endTurnButton.onClick.AddListener(OnEndTurn);
        tokenObject.SetActive(false);

        // Проверяем ссылки
        if (cityManager == null)
            Debug.LogError("CityManager не назначен!");
    }

    public void SetPath(PathCellInitializer path)
    {
        if (path == null) return;

        currentPath = path;
        currentCellIndex = 0;
        pathCells = new Cell[path.transform.childCount];

        for (int i = 0; i < path.transform.childCount; i++)
        {
            pathCells[i] = path.transform.GetChild(i).GetComponent<Cell>();
            if (pathCells[i] == null)
                Debug.LogError($"Клетка {i} на пути не имеет компонента Cell");
        }

        MoveToCell(currentCellIndex);
        tokenObject.SetActive(true);
    }

    private void OnEndTurn()
    {
        if (currentPath == null || currentCellIndex < 0) return;

        currentCellIndex++;

        if (currentCellIndex >= pathCells.Length)
        {
            ArriveAtDestination();
        }
        else
        {
            MoveToCell(currentCellIndex);

            teamSystem.PaySalaries();
        }
    }

    private void ArriveAtDestination()
    {
        if (currentPath.FinishCity != null)
        {
            // Получаем панель города назначения
            City destinationCity = currentPath.FinishCity;

           

            // Вариант 2: Получаем индивидуальную панель города
            CityPanel destinationPanel = cityManager.GetCityPanel(destinationCity);
            destinationPanel.OpenPanel(destinationCity);

            Debug.Log($"Игрок достиг города {destinationCity.CityName}");
        }
        else
        {
            Debug.LogWarning("Город финиша не задан для этого пути!");
        }

        ResetToken();
    }

    private void ResetToken()
    {
        currentPath = null;
        currentCellIndex = -1;
        tokenObject.SetActive(false);
    }

    private void MoveToCell(int cellIndex)
    {
        if (cellIndex < 0 || cellIndex >= pathCells.Length || pathCells[cellIndex] == null)
        {
            Debug.LogError($"Невозможно переместиться на клетку {cellIndex}");
            return;
        }

        tokenObject.transform.position = pathCells[cellIndex].Position;
        Debug.Log($"Фишка перемещена на клетку {cellIndex}");
    }
}