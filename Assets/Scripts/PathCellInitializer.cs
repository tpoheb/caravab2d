using UnityEngine;

public class PathCellInitializer : MonoBehaviour
{
    [Header("Настройки пути")]
    [SerializeField] private City finishCity;
    [SerializeField] private bool initializeOnStart = true;
    [Tooltip("Клетки в порядке движения. Если список пуст, используются дочерние объекты пути.")]
    [SerializeField] private Cell[] _routeCells;

    public City FinishCity => finishCity;
    private bool HasAssignedCells => _routeCells != null && _routeCells.Length > 0;
    public int CellCount => HasAssignedCells ? _routeCells.Length : transform.childCount;
    public Cell GetCell(int index) => HasAssignedCells ? _routeCells[index] : transform.GetChild(index).GetComponent<Cell>();

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
        if (_isInitialized) return; // убрали предупреждение — это нормальная ситуация

        // Два направления используют те же клетки доски, не меняя их имена и Transform.
        if (HasAssignedCells)
        {
            _isInitialized = true;
            Debug.Log($"PathCellInitializer: Подключено {CellCount} клеток доски к пути '{name}'");
            return;
        }

        int cellIndex = 0;

        foreach (Transform child in transform)
        {
            if (!child.CompareTag("Cell")) continue;

            Cell cell = child.GetComponent<Cell>();
            if (cell == null)
                cell = child.gameObject.AddComponent<Cell>();

            cell.cellNumber = cellIndex;
            child.name      = $"Cell_{cellIndex}";
            cellIndex++;
        }

        _isInitialized = true;
        Debug.Log($"PathCellInitializer: Инициализировано {cellIndex} клеток пути '{name}'");
    }
}
