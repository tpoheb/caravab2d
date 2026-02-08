using UnityEngine;

public class PathCellInitializer : MonoBehaviour
{
    [Header("Настройки пути")]
    [SerializeField] private City finishCity;
    [SerializeField] private bool initializeOnStart = true;

    public City FinishCity => finishCity;

    private bool _isInitialized;

    private void Start()
    {
        if (initializeOnStart)
        {
            InitializeCells();
        }
    }

    public void InitializeCells()
    {
        if (_isInitialized)
        {
            Debug.LogWarning($"PathCellInitializer: Путь '{name}' уже инициализирован");
            return;
        }

        int cellIndex = 0;

        foreach (Transform child in transform)
        {
            if (!child.CompareTag("Cell"))
                continue;

            Cell cell = child.GetComponent<Cell>();
            if (cell == null)
            {
                cell = child.gameObject.AddComponent<Cell>();
            }

            cell.cellNumber = cellIndex;
            child.name = $"Cell_{cellIndex}";
            cellIndex++;
        }

        _isInitialized = true;
        Debug.Log($"PathCellInitializer: Инициализировано {cellIndex} клеток пути '{name}'");
    }
}