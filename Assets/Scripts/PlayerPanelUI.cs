using UnityEngine;
using TMPro;
using UnityEngine.UI;

public class PlayerPanelUI : MonoBehaviour
{
    [Header("UI Elements")]
    [SerializeField] private GameObject panel;
    [SerializeField] private TextMeshProUGUI moneyText;
    [SerializeField] private TextMeshProUGUI statsText;
    [SerializeField] private Transform inventoryContainer;
    [SerializeField] private GameObject inventoryItemPrefab;
    [SerializeField] private Button closeButton;

    [Header("References")]
    [SerializeField] private PlayerInventory playerInventory;
    [SerializeField] private PlayerStats playerStats;
    [SerializeField] private TeamSystem teamSystem;

    private void Awake()
    {
        closeButton.onClick.AddListener(ClosePanel);
        panel.SetActive(false);
    }

    public void OpenPanel()
    {
        UpdateUI();
        panel.SetActive(true);
    }

    public void ClosePanel()
    {
        panel.SetActive(false);
    }

    public void UpdateUI()
    {
        // Обновляем деньги
        moneyText.text = $"Золото: {playerInventory.Money}";

        // Обновляем характеристики
        statsText.text = $"Атака: {playerStats.Attack}\n" +
                        $"Выгода: {playerStats.Bargain}\n" +
                        $"Грузоподъемность: {playerStats.Capacity}";

        // Обновляем инвентарь
        UpdateInventoryUI();
    }

    private void UpdateInventoryUI()
    {
        // Очищаем контейнер
        foreach (Transform child in inventoryContainer)
        {
            Destroy(child.gameObject);
        }

        // Заполняем инвентарь
        foreach (var item in playerInventory.Items)
        {
            var itemUI = Instantiate(inventoryItemPrefab, inventoryContainer);
            itemUI.GetComponentInChildren<TextMeshProUGUI>().text =
                $"{item.item.name}: {item.quantity}";
        }
    }
}