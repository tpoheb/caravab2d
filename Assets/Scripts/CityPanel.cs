using UnityEngine;
using UnityEngine.UI;

public class CityPanel : MonoBehaviour
{
    [Header("UI Elements")]
    public GameObject panel;
    public Text cityNameText;
    public Button tradeButton;
    //public Button closeButton;

    [Header("Trade System")]
    public TradeItemSystem tradeSystem;

    private CityData currentCity;

    private void Start()
    {
        tradeButton.onClick.AddListener(OpenTrade);
        //closeButton.onClick.AddListener(ClosePanel);
        panel.SetActive(false);
    }

    public void OpenPanel(CityData city)
    {
        currentCity = city;

        // Обновляем UI города
        cityNameText.text = city.cityName;
        panel.SetActive(true);

        Debug.Log($"Открыта панель города: {city.cityName}");
    }

    private void OpenTrade()
    {
        if (currentCity != null)
        {
            tradeSystem.OpenCityTrade(currentCity);
        }
        else
        {
            Debug.LogError("Не выбран текущий город!");
        }
    }

    public void ClosePanel()
    {
        panel.SetActive(false);
        tradeSystem.CloseTrade(); // Закрываем и торговую панель
    }
}