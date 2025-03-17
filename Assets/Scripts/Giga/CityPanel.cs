using UnityEngine;
using UnityEngine.UI;

public class CityPanel : MonoBehaviour
{
    public Button BuyGoodsButton;
    public Button HireTeamButton;
    public Dropdown ChoosePathDropdown;

    private void Awake()
    {
        BuyGoodsButton.onClick.AddListener(OnBuyGoodsClicked);
        HireTeamButton.onClick.AddListener(OnHireTeamClicked);
        ChoosePathDropdown.onValueChanged.AddListener(OnChoosePathChanged);
    }

    private void OnBuyGoodsClicked()
    {
        Debug.Log("������� �������...");
    }

    private void OnHireTeamClicked()
    {
        Debug.Log("���� �������...");
    }

    private void OnChoosePathChanged(int index)
    {
        Debug.Log($"������ ����: {index}");
        gameObject.SetActive(false); // ������� ������ ����� ������ ����
    }
}