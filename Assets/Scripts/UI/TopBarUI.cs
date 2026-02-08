using UnityEngine;
using TMPro;

public class TopBarUI : MonoBehaviour
{
    [Header("UI Elements")]
    [SerializeField] private GameObject panel;
    [SerializeField] private TextMeshProUGUI moneyText;
    
    [Header("Stats Fields")]
    [SerializeField] private TextMeshProUGUI attackText;
    [SerializeField] private TextMeshProUGUI capacityText; 
    [SerializeField] private TextMeshProUGUI bargainText;  
    
    [Header("References")]
    [SerializeField] private PlayerInventory playerInventory;
    [SerializeField] private PlayerStats playerStats;
    [SerializeField] private TeamSystem teamSystem;

    private void Start()
    {
        // Включаем панель и обновляем данные в первый раз
        OpenPanel();
    }

    private void OnEnable()
    {
        if (playerInventory != null)
        {
            // Подписываемся на события инвентаря
            playerInventory.OnMoneyChanged += UpdateUI;
            playerInventory.OnInventoryChanged += UpdateUI;
        }

        // Подписка на выбор пути (статическое событие из CityPanel)
        //CityPanel.OnPathSelected += HandlePathSelected;
    }

    private void OnDisable()
    {
        if (playerInventory != null)
        {
            // Отписываемся, чтобы избежать утечек памяти
            playerInventory.OnMoneyChanged -= UpdateUI;
            playerInventory.OnInventoryChanged -= UpdateUI;
        }

        //CityPanel.OnPathSelected -= HandlePathSelected;
    }

    private void HandlePathSelected(PathCellInitializer path)
    {
        UpdateUI();
    }

    public void OpenPanel()
    {
        UpdateUI();
        if (panel != null) panel.SetActive(true);
    }

    public void UpdateUI()
    {
        if (playerInventory == null || playerStats == null) return;

        // 1. Обновление денег (берем напрямую из Money)
        moneyText.text = playerInventory.Money.ToString();

        // 2. Обновление атаки (Сумма игрока и всей команды)
        int totalAttack = (teamSystem != null) ? teamSystem.GetTotalAttack() : playerStats.Attack;
        attackText.text = totalAttack.ToString();
    
        // 3. Обновление грузоподъемности (показываем текущий вес / макс. вместимость)
        // Если хочешь видеть просто общую емкость, оставь только playerStats.Capacity
        int currentWeight = playerInventory.GetCurrentWeight();
        capacityText.text = $"{currentWeight}/{playerStats.Capacity}";

        // 4. Обновление торга
        bargainText.text = playerStats.Bargain.ToString();
    }
}