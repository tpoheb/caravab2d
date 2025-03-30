using System;
using UnityEngine;
using UnityEngine.UI;

public class CityPanelManager : MonoBehaviour
{
    [Header("UI Prefabs and Containers")]
    public GameObject directionButtonPrefab; // ������ ������ �����������
    public Transform directionButtonsContainer; // ��������� ��� ������ �����������

    // ���������� ������ ����������� ��� �������� ������ ������
    public void UpdateDirectionButtons(City city, Action<Direction> onDirectionSelected)
    {
        if (directionButtonsContainer == null || directionButtonPrefab == null)
        {
            Debug.LogError("�� ��������� ������ ������ ��� ��������� ��� ������.");
            return;
        }

        // ������� ������ ������
        foreach (Transform child in directionButtonsContainer)
        {
            Destroy(child.gameObject);
        }

        // ������� ����� ������
        foreach (var direction in city.directions)
        {
            GameObject buttonObject = Instantiate(directionButtonPrefab, directionButtonsContainer);
            Button button = buttonObject.GetComponent<Button>();
            Text buttonText = buttonObject.GetComponentInChildren<Text>();

            if (buttonText != null)
            {
                buttonText.text = "� " + direction.destinationCity.cityName;
            }

            // ������������� �� ������� ������� ������
            if (button != null && onDirectionSelected != null)
            {
                button.onClick.AddListener(() => onDirectionSelected(direction));
            }
        }
    }
}