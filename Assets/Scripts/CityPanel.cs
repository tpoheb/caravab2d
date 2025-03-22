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

    private List<Button> pathButtons = new List<Button>(); // Список созданных кнопок путей

    void Start()
    {
        InitializePanel();
        gameObject.SetActive(true); // Панель изначально активна
    }

    // Инициализация панели
    public void InitializePanel()
    {
        if (currentCity == null)
        {
            Debug.LogError("Не задан текущий город для панели!");
            return;
        }

        // Очищаем старые кнопки, если они есть
        ClearPathButtons();

        // Создаем кнопки для каждого пути
        for (int i = 0; i < currentCity.Paths.Count; i++)
        {
            int pathIndex = i;
            GameObject buttonObj = Instantiate(pathButtonPrefab, pathButtonsContainer);
            Button pathButton = buttonObj.GetComponent<Button>();

            // Устанавливаем позицию кнопки (вертикальное расположение)
            RectTransform buttonRect = buttonObj.GetComponent<RectTransform>();
            float buttonHeight = buttonRect.sizeDelta.y;
            buttonRect.anchoredPosition = new Vector2(0, -i * (buttonHeight + buttonSpacing));

            // Настраиваем текст кнопки
            Text buttonText = pathButton.GetComponentInChildren<Text>();
            if (buttonText != null)
            {
                buttonText.text = $"Путь {pathIndex + 1}";
            }

            // Добавляем обработчик нажатия
            pathButton.onClick.AddListener(() => OnPathButtonClicked(pathIndex));

            pathButtons.Add(pathButton);
        }

        // Настраиваем кнопки найма и покупки
        hireTeamButton.onClick.AddListener(OnHireTeamClicked);
        buyGoodsButton.onClick.AddListener(OnBuyGoodsClicked);
    }

    // Открытие панели для конкретного города
    public void OpenPanel(City city)
    {
        currentCity = city;
        InitializePanel();
        gameObject.SetActive(true);
    }

    // Закрытие панели
    public void ClosePanel()
    {
        gameObject.SetActive(false);
    }

    // Обработчик нажатия на кнопку пути
    private void OnPathButtonClicked(int pathIndex)
    {
        Debug.Log($"Выбран путь {pathIndex + 1} в городе {currentCity.CityName}");
        ClosePanel(); // Закрываем панель после выбора пути
    }

    // Обработчик найма команды
    private void OnHireTeamClicked()
    {
        Debug.Log($"Нажата кнопка найма команды в городе {currentCity.CityName}");
        // Здесь можно добавить логику найма
    }

    // Обработчик покупки товаров
    private void OnBuyGoodsClicked()
    {
        Debug.Log($"Нажата кнопка покупки товаров в городе {currentCity.CityName}");
        // Здесь можно добавить логику покупки
    }

    // Очистка кнопок путей
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