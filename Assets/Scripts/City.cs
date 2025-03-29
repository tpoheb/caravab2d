using UnityEngine;
using System.Collections.Generic;

public class City : MonoBehaviour
{
    public string CityName => cityData.cityName;
    public CityData cityData;
    public CityPanel CityPanel; // �������������� ������ (�����������)
    [SerializeField] private string cityName; // �������� ������
    [SerializeField] private List<PathCellInitializer> inCityPaths = new List<PathCellInitializer>(); // ������ ����� � ������

   
    public List<PathCellInitializer> Paths => inCityPaths;

    void Start()
    {
        InitializeCity();
    }

    // ������������� ������
    private void InitializeCity()
    {
        if (string.IsNullOrEmpty(cityName))
        {
            cityName = "Unnamed City";
        }

        // �������������� ��� ���� � ������
        foreach (var path in inCityPaths)
        {
            if (path != null)
            {
                path.InitializeCells();
            }
            else
            {
                Debug.LogWarning($"��������� ������ ���� � ������ {cityName}");
            }
        }

        Debug.Log($"����� {cityName} ���������������. ����� �����: {inCityPaths.Count}");
    }


}