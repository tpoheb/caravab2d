using System.Collections.Generic;
using UnityEngine;

public class CityCell : MonoBehaviour
{
    public string cityName; // Название города
    public List<Path> availablePaths; // Доступные пути из этого города
    public GameObject cityPanel; // Панель города
    public CityUIController cityUIController; // Контроллер UI города

    // Вызывается, когда игрок входит в город
    public void OnPlayerEnter()
    {
        Debug.Log($"Игрок вошел в город: {cityName}");
        OpenCityPanel();
        cityUIController.CreatePathButtons(); // Создаем кнопки выбора пути
    }

    // Открывает панель города
    public void OpenCityPanel()
    {
        if (cityPanel != null)
        {
            cityPanel.SetActive(true);
        }
        else
        {
            Debug.LogError("Панель города не назначена!");
        }
    }

    // Закрывает панель города
    public void CloseCityPanel()
    {
        if (cityPanel != null)
        {
            cityPanel.SetActive(false);
        }
    }
}