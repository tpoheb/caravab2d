using UnityEngine;
using TMPro;
using UnityEngine.UI;

public class PlayerPanelUI : MonoBehaviour
{
    // Теперь вместо одного statsText у нас три отдельных поля.
    [Header("UI Elements")]
    [SerializeField] private GameObject panel;
    [SerializeField] private TextMeshProUGUI moneyText;
    
    // Новые поля для раздельного отображения характеристик
    [SerializeField] private TextMeshProUGUI attackText;
    [SerializeField] private TextMeshProUGUI capacityText; // Грузоподъемность (Capacity)
    [SerializeField] private TextMeshProUGUI bargainText;  // Выгода/Торг (Bargain)
    
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
        // 2. Обновление Денег
        moneyText.text = $"${playerInventory.Money}";

        // 3. Раздельное обновление Характеристик
        // Атака
        attackText.text = $"Атака: {playerStats.Attack}";
        
        // Грузоподъемность
        capacityText.text = $"Грузоп.: {playerStats.Capacity}";
        
        // Выгода/Торг
        bargainText.text = $"Выгода: {playerStats.Bargain}";

        // 4. Обновление Инвентаря
        UpdateInventoryUI();
    }

    private void UpdateInventoryUI()
    {
        // Очистка старых элементов инвентаря
        foreach (Transform child in inventoryContainer)
        {
            Destroy(child.gameObject);
        }

        // Создание новых элементов
        foreach (var item in playerInventory.Items)
        {
            var itemUI = Instantiate(inventoryItemPrefab, inventoryContainer);
            itemUI.GetComponentInChildren<TextMeshProUGUI>().text =
                $"{item.item.name}: {item.quantity}";
        }
    }
}