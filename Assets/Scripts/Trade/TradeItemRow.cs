using UnityEngine;
using UnityEngine.UI;

public class TradeItemRow : MonoBehaviour
{
    // ���� ��� UI ���������
    private Text itemNameText;
    private Text cityQuantityText;
    private Text playerQuantityText;
    private InputField quantityInput;
    private Button buyButton;
    private Button sellButton;

    private Item item; // ������ ������

    // ������������� �����������
    void Awake()
    {
        // ������� ���������� � �������� ��������
        itemNameText = transform.Find("ItemNameText").GetComponent<Text>();
        cityQuantityText = transform.Find("CityQuantityText").GetComponent<Text>();
        playerQuantityText = transform.Find("PlayerQuantityText").GetComponent<Text>();
        quantityInput = transform.Find("QuantityInput").GetComponent<InputField>();
        buyButton = transform.Find("BuyButton").GetComponent<Button>();
        sellButton = transform.Find("SellButton").GetComponent<Button>();

        // ����������� ������ � �������
        buyButton.onClick.AddListener(BuyItem);
        sellButton.onClick.AddListener(SellItem);
    }

    // ������������� ������ ������
    public void Initialize(Item item)
    {
        this.item = item;
        UpdateUI();
    }

    // ���������� UI
    public void UpdateUI()
    {
        itemNameText.text = item.itemName;
        cityQuantityText.text = $"� ������: {item.quantityInCity}";
        playerQuantityText.text = $"� ������: {item.quantityInPlayerInventory}";
    }

    // ������� ������
    public void BuyItem()
    {
        int quantity = int.Parse(quantityInput.text);
        if (quantity > 0 && item.quantityInCity >= quantity)
        {
            item.quantityInCity -= quantity;
            item.quantityInPlayerInventory += quantity;
            Debug.Log($"������� {quantity} ������ ������ {item.itemName}");
        }
        else
        {
            Debug.Log("������������ ������ � ������ ��� ������� �������� ����������.");
        }
        UpdateUI();
    }

    // ������� ������
    public void SellItem()
    {
        int quantity = int.Parse(quantityInput.text);
        if (quantity > 0 && item.quantityInPlayerInventory >= quantity)
        {
            item.quantityInCity += quantity;
            item.quantityInPlayerInventory -= quantity;
            Debug.Log($"������� {quantity} ������ ������ {item.itemName}");
        }
        else
        {
            Debug.Log("������������ ������ � ������ ��� ������� �������� ����������.");
        }
        UpdateUI();
    }
}