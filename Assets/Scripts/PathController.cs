using UnityEngine;

public class PathController : MonoBehaviour
{
    [Header("Token")]
    [SerializeField] private GameObject tokenObject;

    private PathCellInitializer currentPath;
    private int currentCellIndex = -1;
    private Cell[] pathCells;

    public PathCellInitializer CurrentPath => currentPath;

    public void SetPath(PathCellInitializer path)
    {
        if (path == null) return;
        currentPath = path;
        currentCellIndex = 0;
        InitializePathCells();
        MoveCurrent();
        tokenObject.SetActive(true);
    }

    public void Advance()
    {
        currentCellIndex++;
    }

    public bool HasActivePath()
    {
        return currentPath != null && currentCellIndex >= 0;
    }

    public bool IsPathCompleted()
    {
        return pathCells != null && currentCellIndex >= pathCells.Length;
    }

    public void MoveCurrent()
    {
        MoveToCell(currentCellIndex);
    }

    public void ResetToken()
    {
        currentPath = null;
        currentCellIndex = -1;
        tokenObject.SetActive(false);
    }

    private void InitializePathCells()
    {
        int count = currentPath.transform.childCount;
        pathCells = new Cell[count];
        for (int i = 0; i < count; i++)
        {
            pathCells[i] = currentPath.transform.GetChild(i).GetComponent<Cell>();
            if (pathCells[i] == null)
                Debug.LogError($"Клетка {i} на пути не имеет компонента Cell");
        }
    }

    private void MoveToCell(int index)
    {
        if (index < 0 || index >= pathCells.Length || pathCells[index] == null)
        {
            Debug.LogError($"Невозможно переместиться на клетку {index}");
            return;
        }

        tokenObject.transform.position = pathCells[index].Position;
        Debug.Log($"Фишка перемещена на клетку {index}");
    }

    public void HandleCurrentCellEffect(UIHandler uiHandler, PlayerInventory inventory)
    {
        var cell = pathCells[currentCellIndex];
        switch (cell.Type)
        {
            case CellType.Battle:
                uiHandler.ShowBattleWindow(OnBattleComplete);
                break;
            case CellType.Event:
                uiHandler.ShowEventPopup(inventory, OnEventComplete);
                break;
        }
    }

    private void OnBattleComplete()
    {
        Debug.Log("Битва завершена");
    }

    private void OnEventComplete()
    {
        Debug.Log("Событие завершено");
    }
}
