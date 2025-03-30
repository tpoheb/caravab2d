using System.Collections.Generic;
using UnityEngine;

public class CityManager : MonoBehaviour
{
    [SerializeField] private List<City> allCities;
    [SerializeField] private CityPanel defaultCityPanel;

    private Dictionary<City, CityPanel> cityPanels = new Dictionary<City, CityPanel>();

    private void Awake()
    {
        // ������������� ������� (���� � ������� ������ ���� ������)
        foreach (var city in allCities)
        {
            if (city.CityPanel != null)
                cityPanels.Add(city, city.CityPanel);
        }
    }

    public CityPanel GetCityPanel(City city)
    {
        if (cityPanels.TryGetValue(city, out CityPanel panel))
            return panel;

        return defaultCityPanel;
    }
}