using UnityEngine;
using UnityEngine.UI;

public class PlayerToken : MonoBehaviour
{
    [SerializeField] private CityPanel cityPanel; // ������ ������
    [SerializeField] private Button endTurnButton; // ������ "����� ����"
    [SerializeField] private GameObject tokenObject; // ������ ����� ������

    private PathCellInitializer currentPath; // ������� ����
    private int currentCellIndex = -1; // ������ ������� ������ (-1 = ��� ����)
    private Cell[] pathCells; // ������ �������� ����

    void Start()
    {
        endTurnButton.onClick.AddListener(OnEndTurn);
        tokenObject.SetActive(false); // ����� ������ �� ������ ����
    }

    // ��������� ���� � ������ ��������
    public void SetPath(PathCellInitializer path)
    {
        currentPath = path;
        currentCellIndex = 0; // �������� � ������ ������

        // �������� ��� ������ ����
        pathCells = new Cell[path.transform.childCount];
        for (int i = 0; i < path.transform.childCount; i++)
        {
            pathCells[i] = path.transform.GetChild(i).GetComponent<Cell>();
        }

        // �������� ����� �� ������ ������
        MoveToCell(currentCellIndex);
        tokenObject.SetActive(true);
    }

    // ����������� �� ��������� ������ ��� ������� "����� ����"
    private void OnEndTurn()
    {
        if (currentPath == null || currentCellIndex < 0) return;

        currentCellIndex++;

        // ���������, ������ �� ����� ����� ����
        if (currentCellIndex >= pathCells.Length)
        {
            // ��������� ������ ������ ������
            if (currentPath.FinishCity != null)
            {
                cityPanel.OpenPanel(currentPath.FinishCity);
                Debug.Log($"����� ������ ������ {currentPath.FinishCity.CityName}");
            }
            else
            {
                Debug.LogWarning("����� ������ �� ����� ��� ����� ����!");
            }

            // ���������� ���� � �������� �����
            currentPath = null;
            currentCellIndex = -1;
            tokenObject.SetActive(false);
        }
        else
        {
            // ���������� ����� �� ��������� ������
            MoveToCell(currentCellIndex);
        }
    }

    // ����������� ����� �� ��������� ������
    private void MoveToCell(int cellIndex)
    {
        if (cellIndex >= 0 && cellIndex < pathCells.Length)
        {
            tokenObject.transform.position = pathCells[cellIndex].Position;
            Debug.Log($"����� ���������� �� ������ {cellIndex}");
        }
    }
}