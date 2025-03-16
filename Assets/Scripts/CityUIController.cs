using UnityEngine;
using UnityEngine.UI;

public class CityUIController : MonoBehaviour
{
    public GameManager gameManager; // ������ �� GameManager
    public GameObject buttonPrefab; // ������ ������
    public Transform buttonContainer; // ��������� ��� ������

    // ������� ������ ��� ������� ����
    public void CreatePathButtons()
    {
        if (gameManager == null || gameManager.CurrentCity == null)
        {
            Debug.LogError("GameManager ��� ������� ����� �� ���������!");
            return;
        }

        // ������� ���������
        foreach (Transform child in buttonContainer)
        {
            Destroy(child.gameObject);
        }

        // ������� ������ ��� ������� ����
        for (int i = 0; i < gameManager.CurrentCity.availablePaths.Count; i++)
        {
            int pathIndex = i; // ��������� ���������� ��� ���������

            // ������ "������"
            CreateButton($"���� {i + 1} (������)", () => gameManager.ChoosePath(pathIndex, true));

            // ������ "�����"
            CreateButton($"���� {i + 1} (�����)", () => gameManager.ChoosePath(pathIndex, false));
        }
    }

    // ������� ������ � �������� ������� � ���������
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

    // ���������� ��� ������� �� ������ ������� �������
    public void OnBuyButtonClicked()
    {
        Debug.Log("������ �������!");
    }

    // ���������� ��� ������� �� ������ ����� �������
    public void OnHireButtonClicked()
    {
        Debug.Log("������� ������!");
    }
}