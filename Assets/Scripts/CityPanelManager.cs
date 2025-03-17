using System;
using UnityEngine;
using UnityEngine.UI;

public class CityPanelManager : MonoBehaviour
{
    [Header("UI Prefabs and Containers")]
    public GameObject directionButtonPrefab; // Префаб кнопки направления
    public Transform directionButtonsContainer; // Контейнер для кнопок направлений

    // Обновление кнопок направлений при открытии панели города
    public void UpdateDirectionButtons(City city, Action<Direction> onDirectionSelected)
    {
        if (directionButtonsContainer == null || directionButtonPrefab == null)
        {
            Debug.LogError("Не настроены префаб кнопки или контейнер для кнопок.");
            return;
        }

        // Очищаем старые кнопки
        foreach (Transform child in directionButtonsContainer)
        {
            Destroy(child.gameObject);
        }

        // Создаем новые кнопки
        foreach (var direction in city.directions)
        {
            GameObject buttonObject = Instantiate(directionButtonPrefab, directionButtonsContainer);
            Button button = buttonObject.GetComponent<Button>();
            Text buttonText = buttonObject.GetComponentInChildren<Text>();

            if (buttonText != null)
            {
                buttonText.text = "К " + direction.destinationCity.cityName;
            }

            // Подписываемся на событие нажатия кнопки
            if (button != null && onDirectionSelected != null)
            {
                button.onClick.AddListener(() => onDirectionSelected(direction));
            }
        }
    }
}