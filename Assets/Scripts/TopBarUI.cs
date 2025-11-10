using UnityEngine;
using TMPro;

public class TopBarUI : MonoBehaviour
{
    [Header("UI Elements")]
    [SerializeField] private TextMeshProUGUI moneyText;
    [SerializeField] private TextMeshProUGUI attackText;
    [SerializeField] private TextMeshProUGUI capacityText;
    [SerializeField] private TextMeshProUGUI bargainText;
    
    [Header("References")]
    [SerializeField] private PlayerInventory playerInventory;
    [SerializeField] private PlayerStats playerStats;

    private int lastMoney;
    private int lastAttack;
    private int lastCapacity;
    private int lastBargain;

    private void Start()
    {
        UpdateUI();
        CacheValues();
    }

    private void Update()
    {
        // Проверяем изменения и обновляем только при необходимости
        if (HasValuesChanged())
        {
            UpdateChangedValues();
            CacheValues();
        }
    }

    private void CacheValues()
    {
        lastMoney = playerInventory.Money;
        lastAttack = playerStats.Attack;
        lastCapacity = playerStats.Capacity;
        lastBargain = playerStats.Bargain;
    }

    private bool HasValuesChanged()
    {
        return lastMoney != playerInventory.Money ||
               lastAttack != playerStats.Attack ||
               lastCapacity != playerStats.Capacity ||
               lastBargain != playerStats.Bargain;
    }

    private void UpdateChangedValues()
    {
        if (lastMoney != playerInventory.Money)
            moneyText.text = $"{playerInventory.Money}";

        if (lastAttack != playerStats.Attack)
            attackText.text = $"{playerStats.Attack}";

        if (lastCapacity != playerStats.Capacity)
            capacityText.text = $"{playerStats.Capacity}";

        if (lastBargain != playerStats.Bargain)
            bargainText.text = $"{playerStats.Bargain}";
    }

    public void UpdateUI()
    {
        moneyText.text = $"{playerInventory.Money}";
        attackText.text = $"{playerStats.Attack}";
        capacityText.text = $"{playerStats.Capacity}";
        bargainText.text = $"{playerStats.Bargain}";
        
        CacheValues();
    }
}