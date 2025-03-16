using System.Collections.Generic;
using UnityEngine;

public class CityCell : MonoBehaviour
{
    public string cityName; // �������� ������
    public List<Path> availablePaths; // ��������� ���� �� ����� ������
    public GameObject cityPanel; // ������ ������
    public CityUIController cityUIController; // ���������� UI ������

    // ����������, ����� ����� ������ � �����
    public void OnPlayerEnter()
    {
        Debug.Log($"����� ����� � �����: {cityName}");
        OpenCityPanel();
        cityUIController.CreatePathButtons(); // ������� ������ ������ ����
    }

    // ��������� ������ ������
    public void OpenCityPanel()
    {
        if (cityPanel != null)
        {
            cityPanel.SetActive(true);
        }
        else
        {
            Debug.LogError("������ ������ �� ���������!");
        }
    }

    // ��������� ������ ������
    public void CloseCityPanel()
    {
        if (cityPanel != null)
        {
            cityPanel.SetActive(false);
        }
    }
}