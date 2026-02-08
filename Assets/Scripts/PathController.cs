using UnityEngine;

public class PathController : MonoBehaviour
{
    [Header("Token")]
    [SerializeField] private GameObject tokenObject;

    private PathCellInitializer currentPath;
    private Cell[] pathCells;
    private int currentCellIndex = -1;

    // --- PROPERTIES ---

    public bool HasActivePath => currentPath != null;
    public PathCellInitializer CurrentPath => currentPath;

    // --------------------
    // PATH LIFECYCLE
    // --------------------

    public void SetPath(PathCellInitializer path)
    {
        if (path == null)
        {
            Debug.LogError("PathController: Cannot set null path.");
            return;
        }

        currentPath = path;
        currentCellIndex = -1; // Ставим -1, чтобы первый Step() сделал индекс 0
        CachePathCells();
        ActivateToken();

        Debug.Log("PathController: Path initialized.");
    }

    /// <summary>
    /// Makes one step along the path.
    /// </summary>
    /// <returns>True if path is completed</returns>
    public bool Step()
    {
        if (!HasActivePath) return true;

        currentCellIndex++;

        // Если вышли за пределы массива — путь окончен
        if (IsPathCompleted())
        {
            return true;
        }

        // Мы НЕ вызываем перемещение здесь, так как 
        // PlayerToken сделает это сам через MoveCurrent()
        return false;
    }

    /// <summary>
    /// Публичный метод для визуального обновления позиции токена.
    /// Переименован из MoveToCurrentCell для совместимости с PlayerToken.
    /// </summary>
    public void MoveCurrent()
    {
        if (!IsValidIndex(currentCellIndex))
        {
            Debug.LogError($"PathController: Invalid cell index {currentCellIndex}.");
            return;
        }

        tokenObject.transform.position = pathCells[currentCellIndex].Position;
        Debug.Log($"PathController: Token moved to cell {currentCellIndex}.");
    }

    public void ResetPath()
    {
        currentPath = null;
        currentCellIndex = -1;

        if (tokenObject != null)
            tokenObject.SetActive(false);

        Debug.Log("PathController: Путь сброшен, токен деактивирован");
    }

    // --------------------
    // INTERNALS
    // --------------------

    private void CachePathCells()
    {
        int count = currentPath.transform.childCount;
        pathCells = new Cell[count];

        for (int i = 0; i < count; i++)
        {
            var cell = currentPath.transform.GetChild(i).GetComponent<Cell>();

            if (cell == null)
            {
                Debug.LogError($"PathController: Missing Cell component at index {i}.");
            }

            pathCells[i] = cell;
        }
    }

    private void ActivateToken()
    {
        if (tokenObject != null)
        {
            tokenObject.SetActive(true);
        }
    }

    private void MoveToCurrentCell()
    {
        if (!IsValidIndex(currentCellIndex))
        {
            Debug.LogError($"PathController: Invalid cell index {currentCellIndex}.");
            return;
        }

        tokenObject.transform.position = pathCells[currentCellIndex].Position;
        Debug.Log($"PathController: Token moved to cell {currentCellIndex}.");
    }

    private bool IsPathCompleted()
    {
        return pathCells == null || currentCellIndex >= pathCells.Length;
    }

    private bool IsValidIndex(int index)
    {
        return pathCells != null &&
               index >= 0 &&
               index < pathCells.Length &&
               pathCells[index] != null;
    }
}
