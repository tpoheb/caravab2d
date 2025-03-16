using UnityEngine;

public class CityCell : MonoBehaviour
{
    public string cityName; // Название города
    public GameObject cityUI; // Ссылка на UI окно города

    public void OnPlayerEnter()
    {
        Debug.Log($"Игрок вошел в город: {cityName}");
        OpenCityUI();
    }

    private void OpenCityUI()
    {
        if (cityUI != null)
        {
            cityUI.SetActive(true); // Открываем UI города
        }
    }
}