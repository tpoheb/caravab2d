using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;

public class CityPanel : MonoBehaviour
{
    [SerializeField] private City currentCity; // ������ �� ������� �����
    [SerializeField] private GameObject pathButtonPrefab; // ������ ������ ����
    [SerializeField] private Transform pathButtonsContainer; // ��������� ��� ������ �����
    [SerializeField] private Button hireTeamButton; // ������ ����� �������
    [SerializeField] private Button buyGoodsButton; // ������ ������� �������
    [SerializeField] private float buttonSpacing = 10f; // ���������� ����� ��������
    [SerializeField] private PlayerToken playerToken; // ������ �� ����� ������
    [SerializeField] private TradeSystem tradeSystem;

    private List<Button> pathButtons = new List<Button>(); // ������ ��������� ������ �����

    void Start()
    {
        InitializePanel();
        gameObject.SetActive(true); // ������ ���������� �������
    }

    public void InitializePanel()
    {
        if (currentCity == null)
        {
            Debug.LogError("�� ����� ������� ����� ��� ������!");
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
                buttonText.text = $"���� {pathIndex + 1}";
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
        Debug.Log($"������ ���� {pathIndex + 1} � ������ {currentCity.CityName}");
        playerToken.SetPath(currentCity.Paths[pathIndex]); // ������������� ���� ��� �����
        ClosePanel(); // ��������� ������ ����� ������ ����
    }

    private void OnHireTeamClicked()
    {
        Debug.Log($"������ ������ ����� ������� � ������ {currentCity.CityName}");
    }

    private void OnBuyGoodsClicked()
    {
        if (tradeSystem != null)
        {
            tradeSystem.OpenTradePanel(); // ��������� ������ ��������
        }
        else
        {
            Debug.LogError("TradeSystem �� ������!");
        }
        Debug.Log($"������ ������ ������� ������� � ������ {currentCity.CityName}");
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