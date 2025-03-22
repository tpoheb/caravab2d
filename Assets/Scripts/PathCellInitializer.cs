using UnityEngine;

public class PathCellInitializer : MonoBehaviour
{
    [SerializeField] private bool initializeOnStart = true;

    void Start()
    {
        if (initializeOnStart)
        {
            InitializeCells();
        }
    }

    public void InitializeCells()
    {
        int cellIndex = 0;

        // Проходим по всем дочерним объектам
        foreach (Transform child in transform)
        {
            // Проверяем, является ли объект клеткой (по тегу, имени или компоненту)
            if (child.CompareTag("Cell")) // Предполагается, что у клеток есть тег "Cell"
            {
                Cell cell = child.GetComponent<Cell>();

                if (cell == null)
                {
                    cell = child.gameObject.AddComponent<Cell>();
                }

                cell.cellNumber = cellIndex;
                cell.name = $"Cell_{cellIndex}"; // Дополнительно переименовываем объект
                cellIndex++;
            }
        }

        Debug.Log($"Инициализировано {cellIndex} клеток");
    }
}

public class Cell : MonoBehaviour
{
    public int cellNumber;

    // Дополнительные свойства клетки, если нужно
    public Vector3 Position => transform.position;
    public bool IsActive => gameObject.activeSelf;
}