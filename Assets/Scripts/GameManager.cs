using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class GameManager : MonoBehaviour
{
    [Header("Game Data")]
    public List<City> cities; // ��� ������ � ����
    public GameObject playerToken; // ����� ������

    [Header("UI Components")]
    public Button endTurnButton; // ������ "����� ����"
    public CityPanelManager cityPanelManager; // �������� ������� �������

    private City currentCity; // ������� ����� ������
    private Direction currentDirection; // ������� �����������
    private int currentTileIndex = 0; // ������ ������� ������

    void Start()
    {
        // �������� ���� � ������ ������
        if (cities.Count > 0)
        {
            currentCity = cities[0];
            OpenCityPanel(currentCity);
        }

        // ������������� �� ������� ������� ������ "����� ����"
        if (endTurnButton != null)
        {
            endTurnButton.onClick.AddListener(OnEndTurn);
        }
    }

    // ��������� ������ �������� ������
    private void OpenCityPanel(City city)
    {
        if (city.cityPanel != null && cityPanelManager != null)
        {
            city.cityPanel.SetActive(true);
            // ��������� ������ ����������� ����� CityPanelManager
            cityPanelManager.UpdateDirectionButtons(city, SelectDirection);
        }
    }

    // ��������� ������ �������� ������
    private void CloseCityPanel(City city)
    {
        if (city.cityPanel != null)
        {
            city.cityPanel.SetActive(false);
        }
    }

    // ����� �����������
    public void SelectDirection(Direction direction)
    {
        Debug.Log("������� �����������: " + direction.destinationCity.cityName);
        currentDirection = direction;
        CloseCityPanel(currentCity);
        StartMoving();
    }

    // ������ �������� �� ���������� �����������
    private void StartMoving()
    {
        currentTileIndex = 0;
        MoveToNextTile();
    }

    // ����������� �� ��������� ������
    private void MoveToNextTile()
    {
        if (currentDirection != null && currentTileIndex < currentDirection.tiles.Count)
        {
            // ������ ����������� ����� (��������, ��������� ������� ��� ��������)
            Debug.Log("����������� �� ������: " + currentDirection.tiles[currentTileIndex].tileName);
            currentTileIndex++;
        }
        else
        {
            // �������� ������ ����������
            ArriveAtDestination();
        }
    }

    // �������� � ����� ����������
    private void ArriveAtDestination()
    {
        if (currentDirection != null)
        {
            currentCity = currentDirection.destinationCity;
            OpenCityPanel(currentCity);
            currentDirection = null;
        }
    }

    // ��������� ������� ������ "����� ����"
    private void OnEndTurn()
    {
        if (currentDirection != null)
        {
            MoveToNextTile();
        }
    }
}