using System.Collections.Generic;
using UnityEngine;

public class GameManager : MonoBehaviour
{
    public Transform player; // ����� ������
    public List<Path> allPaths; // ��� ���� �� ����
    public List<CityCell> allCities; // ��� ������ �� ����
    public CityCell startCity; // ��������� �����

    private List<Transform> currentPathCells; // ������� ���� (������ ������)
    private int currentCellIndex = 0; // ������� ������ � ����
    private CityCell _currentCity; // ��������� ���� ��� �������� ������
    public CityCell CurrentCity // ��������� ��������
    {
        get => _currentCity;
        private set => _currentCity = value;
    }
    private bool isMovingForward = true; // ����������� �������� (������ ��� �����)

    private void Start()
    {
        // ������������� ���������� ������
        if (startCity != null)
        {
            CurrentCity = startCity;
            startCity.OnPlayerEnter(); // ��������� ������ ���������� ������
        }
        else
        {
            Debug.LogError("��������� ����� �� ��������!");
        }
    }

    // ���������� ��� ������� �� ������ "����� ����"
    public void EndTurn()
    {
        if (CurrentCity != null)
        {
            Debug.Log("����� ��������� � ������. �������� ����.");
            return;
        }

        // ���������, ��� currentPathCells ��������������� � �� ����
        if (currentPathCells == null || currentPathCells.Count == 0)
        {
            Debug.LogError("������ ������ ���� �� ��������������� ��� ����!");
            return;
        }

        // ���������, ��� currentCellIndex �� ������� �� ������� ������
        if (currentCellIndex < currentPathCells.Count - 1)
        {
            currentCellIndex++;
            MovePlayerToCell(currentPathCells[currentCellIndex]);
            CheckCellType();
        }
        else
        {
            Debug.Log("����� ������ ����� ����.");
            ArriveAtCity();
        }
    }

    // ���������� ����� �� ��������� ������
    private void MovePlayerToCell(Transform cell)
    {
        player.position = cell.position;
    }

    // ��������� ��� ������ (������� ��� �����)
    private void CheckCellType()
    {
        CityCell cityCell = currentPathCells[currentCellIndex].GetComponent<CityCell>();
        if (cityCell != null)
        {
            ArriveAtCity(cityCell);
        }
    }

    // ������������ �������� � �����
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

    // ���������� ��� ������ ���� � UI ������
    public void ChoosePath(int pathIndex, bool isForward)
    {
        if (CurrentCity != null && pathIndex >= 0 && pathIndex < CurrentCity.availablePaths.Count)
        {
            Path chosenPath = CurrentCity.availablePaths[pathIndex];
            StartNewPath(chosenPath, isForward);
            CurrentCity.CloseCityPanel(); // ��������� ������ ������
            CurrentCity = null; // ����� ������� �����
        }
        else
        {
            Debug.Log("�������� ����� ����.");
        }
    }

    // �������� ����� ����
    private void StartNewPath(Path path, bool isForward)
    {
        if (path == null || path.cells == null || path.cells.Count == 0)
        {
            Debug.LogError("���� �� ��������������� ��� �� �������� ������!");
            return;
        }

        currentPathCells = isForward ? path.cells : path.GetReversedCells();
        currentCellIndex = 0; // �������� � ������ ������ ������ ����
        MovePlayerToCell(currentPathCells[currentCellIndex]);
    }
}