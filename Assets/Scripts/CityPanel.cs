using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;

public class CityPanel : MonoBehaviour
{
    [SerializeField] private City currentCity; // Ссылка на текущий город
    [SerializeField] private GameObject pathButtonPrefab; // Префаб кнопки пути
    [SerializeField] private Transform pathButtonsContainer; // Контейнер для кнопок путей
    [SerializeField] private Button hireTeamButton; // Кнопка найма команды
    [SerializeField] private Button buyGoodsButton; // Кнопка покупки товаров
    [SerializeField] private float buttonSpacing = 10f; // Расстояние между кнопками
    [SerializeField] private PlayerToken playerToken; // Ссылка на фишку игрока
    [SerializeField] private TradeSystem tradeSystem;

    private List<Button> pathButtons = new List<Button>(); // Список созданных кнопок путей

    void Start()
    {
        InitializePanel();
        gameObject.SetActive(true); // Панель изначально активна
    }

    public void InitializePanel()
    {
        if (currentCity == null)
        {
            Debug.LogError("Не задан текущий город для панели!");
            return;
        }

        ClearPathButtons();

        for (int i = 0; i < currentCity.Paths.Count; i++)
        {
            int pathIndex = i;
            GameObject buttonObj = Instantiate(pathButtonPrefab, pathButtonsContainer);
            Button pathButton = buttonObj.GetComponent<Button>();

            RectTransform buttonRect = buttonObj.GetComponent<RectTransform>();
            float buttonHeight = buttonRect.sizeDelta.y;
            buttonRect.anchoredPosition = new Vector2(0, -i * (buttonHeight + buttonSpacing));

            Text buttonText = pathButton.GetComponentInChildren<Text>();
            if (buttonText != null)
            {
                buttonText.text = $"Путь {pathIndex + 1}";
            }

            pathButton.onClick.AddListener(() => OnPathButtonClicked(pathIndex));

            pathButtons.Add(pathButton);
        }

        hireTeamButton.onClick.AddListener(OnHireTeamClicked);
        buyGoodsButton.onClick.AddListener(OnBuyGoodsClicked);
    }

    public void OpenPanel(City city)
    {
        currentCity = city;
        InitializePanel();
        gameObject.SetActive(true);
    }

    public void ClosePanel()
    {
        gameObject.SetActive(false);
    }

    private void OnPathButtonClicked(int pathIndex)
    {
        Debug.Log($"Выбран путь {pathIndex + 1} в городе {currentCity.CityName}");
        playerToken.SetPath(currentCity.Paths[pathIndex]); // Устанавливаем путь для фишки
        ClosePanel(); // Закрываем панель после выбора пути
    }

    private void OnHireTeamClicked()
    {
        Debug.Log($"Нажата кнопка найма команды в городе {currentCity.CityName}");
    }

    private void OnBuyGoodsClicked()
    {
        if (tradeSystem != null)
        {
            tradeSystem.OpenTradePanel(); // Открываем панель торговли
        }
        else
        {
            Debug.LogError("TradeSystem не найден!");
        }
        Debug.Log($"Нажата кнопка покупки товаров в городе {currentCity.CityName}");
    }

    private void ClearPathButtons()
    {
        foreach (var button in pathButtons)
        {
            if (button != null)
            {
                button.onClick.RemoveAllListeners();
                Destroy(button.gameObject);
            }
        }
        pathButtons.Clear();
    }
}