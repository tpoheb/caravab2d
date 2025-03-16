using UnityEngine;
using UnityEngine.UI;

public class CityUIController : MonoBehaviour
{
    public GameManager gameManager; // Ссылка на GameManager
    public GameObject buttonPrefab; // Префаб кнопки
    public Transform buttonContainer; // Контейнер для кнопок

    // Создает кнопки для каждого пути
    public void CreatePathButtons()
    {
        if (gameManager == null || gameManager.CurrentCity == null)
        {
            Debug.LogError("GameManager или текущий город не назначены!");
            return;
        }

        // Очищаем контейнер
        foreach (Transform child in buttonContainer)
        {
            Destroy(child.gameObject);
        }

        // Создаем кнопки для каждого пути
        for (int i = 0; i < gameManager.CurrentCity.availablePaths.Count; i++)
        {
            int pathIndex = i; // Локальная переменная для замыкания

            // Кнопка "Вперед"
            CreateButton($"Путь {i + 1} (Вперед)", () => gameManager.ChoosePath(pathIndex, true));

            // Кнопка "Назад"
            CreateButton($"Путь {i + 1} (Назад)", () => gameManager.ChoosePath(pathIndex, false));
        }
    }

    // Создает кнопку с заданным текстом и действием
    private void CreateButton(string buttonText, UnityEngine.Events.UnityAction action)
    {
        GameObject buttonObj = Instantiate(buttonPrefab, buttonContainer);
        Button button = buttonObj.GetComponent<Button>();
        button.onClick.AddListener(action);

        Text textComponent = buttonObj.GetComponentInChildren<Text>();
        if (textComponent != null)
        {
            textComponent.text = buttonText;
        }
    }

    // Вызывается при нажатии на кнопку покупки товаров
    public void OnBuyButtonClicked()
    {
        Debug.Log("Товары куплены!");
    }

    // Вызывается при нажатии на кнопку найма команды
    public void OnHireButtonClicked()
    {
        Debug.Log("Команда нанята!");
    }
}