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

        // �������� �� ���� �������� ��������
        foreach (Transform child in transform)
        {
            // ���������, �������� �� ������ ������� (�� ����, ����� ��� ����������)
            if (child.CompareTag("Cell")) // ��������������, ��� � ������ ���� ��� "Cell"
            {
                Cell cell = child.GetComponent<Cell>();

                if (cell == null)
                {
                    cell = child.gameObject.AddComponent<Cell>();
                }

                cell.cellNumber = cellIndex;
                cell.name = $"Cell_{cellIndex}"; // ������������� ��������������� ������
                cellIndex++;
            }
        }

        Debug.Log($"���������������� {cellIndex} ������");
    }
}

public class Cell : MonoBehaviour
{
    public int cellNumber;

    // �������������� �������� ������, ���� �����
    public Vector3 Position => transform.position;
    public bool IsActive => gameObject.activeSelf;
}