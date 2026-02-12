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
        // 1. Подписка на инвентарь (деньги и вес)
        if (playerInventory != null)
        {
            playerInventory.OnMoneyChanged += UpdateUI;
            playerInventory.OnInventoryChanged += UpdateUI;
        }

        // 2. ВАЖНО: Подписка на изменение характеристик (атака, торг, лимит)
        if (playerStats != null)
        {
            playerStats.OnStatsChanged += UpdateUI;
        }
    }

    private void OnDisable()
    {
        // Отписываемся от всего
        if (playerInventory != null)
        {
            playerInventory.OnMoneyChanged -= UpdateUI;
            playerInventory.OnInventoryChanged -= UpdateUI;
        }

        if (playerStats != null)
        {
            playerStats.OnStatsChanged -= UpdateUI;
        }
    }

    public void OpenPanel()
    {
        if (panel != null) panel.SetActive(true);
        UpdateUI();
    }

    public void UpdateUI()
    {
        // Проверка на null, чтобы не было ошибок в консоли, если ссылки не назначены
        if (playerInventory == null || playerStats == null) return;

        // 1. Обновление денег
        if (moneyText != null)
            moneyText.text = playerInventory.Money.ToString();

        // 2. Обновление атаки (Берем актуальную сумму из команды, которая теперь учитывает playerStats.Attack)
        if (attackText != null)
        {
            int totalAttack = (teamSystem != null) ? teamSystem.GetTotalAttack() : playerStats.Attack;
            attackText.text = totalAttack.ToString();
        }
    
        // 3. Обновление грузоподъемности
        if (capacityText != null)
        {
            int currentWeight = playerInventory.GetCurrentWeight();
            capacityText.text = $"{currentWeight}/{playerStats.Capacity}";
        }

        // 4. Обновление торга
        if (bargainText != null)
            bargainText.text = (playerStats.Bargain >= 0 ? "+" : "") + playerStats.Bargain.ToString();
        
        Debug.Log("[UI] Верхняя панель обновлена актуальными данными.");
    }
}