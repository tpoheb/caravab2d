using System.Collections.Generic;
using UnityEngine;

public class TeamSystem : MonoBehaviour
{
    [Header("Player Systems")]
    [SerializeField] private PlayerInventory playerInventory;
    [SerializeField] private PlayerStats playerStats;

    [Header("UI Configuration")]
    [SerializeField] private HirePanelUI hirePanelPrefab;
    [SerializeField] private Transform uiParent;

    [Header("Unit Data")]
    [SerializeField] private List<UnitData> availableUnits = new List<UnitData>();

    public List<TeamMember> CurrentTeam = new List<TeamMember>();
    private HirePanelUI hirePanelUI;
    private bool isUILoaded = false;

    private void Awake()
    {
        ValidateReferences();
    }

    private void ValidateReferences()
    {
        if (playerInventory == null)
            playerInventory = GetComponent<PlayerInventory>();

        if (playerStats == null)
            playerStats = GetComponent<PlayerStats>();

        if (uiParent == null && FindFirstObjectByType<Canvas>() != null)
            uiParent = FindFirstObjectByType<Canvas>().transform;
    }

    public void OpenHirePanel()
    {
        if (!isUILoaded)
        {
            InitializeUI();
        }

        hirePanelUI.UpdateUI(availableUnits, CurrentTeam, playerInventory.Money);
        hirePanelUI.ShowPanel();
    }

    private void InitializeUI()
    {
        if (hirePanelPrefab == null)
        {
            Debug.LogError("HirePanel prefab is not assigned!", this);
            return;
        }

        hirePanelUI = Instantiate(hirePanelPrefab, uiParent);
        hirePanelUI.Initialize(this); // ������ �������� ������ TeamSystem
        isUILoaded = true;
    }

    public bool TryHireUnit(UnitData unitData)
    {
        if (!playerInventory.TrySpendMoney(unitData.hireCost))
        {
            Debug.Log("Not enough money!");
            return false;
        }

        var newMember = new TeamMember(unitData);
        CurrentTeam.Add(newMember);
        newMember.ApplyBonuses(playerStats);

        UpdateUI();
        return true;
    }

    public void FireUnit(TeamMember member)
    {
        member.RemoveBonuses(playerStats);
        CurrentTeam.Remove(member);
        UpdateUI();
    }

    private void UpdateUI()
    {
        if (isUILoaded)
        {
            hirePanelUI.UpdateUI(availableUnits, CurrentTeam, playerInventory.Money);
        }
    }

    public void PaySalaries()
    {
        int totalSalary = CalculateTotalSalary();
        if (playerInventory.TrySpendMoney(totalSalary))
        {
            Debug.Log($"Salaries paid: {totalSalary}");
        }
        else
        {
            Debug.LogWarning($"Not enough money to pay salaries: {totalSalary}");
            HandleSalaryShortage(totalSalary);
        }
    }

    private int CalculateTotalSalary()
    {
        int total = 0;
        foreach (var member in CurrentTeam)
        {
            total += member.unitData.salaryPerTurn;
        }
        return total;
    }

    private void HandleSalaryShortage(int requiredAmount)
    {
        // ������ ���������� ��� �������� �����
        // ...
    }

    public void CloseHirePanel()
    {
        if (isUILoaded)
        {
            hirePanelUI.ClosePanel();
        }
    }

    private void OnDestroy()
    {
        if (hirePanelUI != null)
        {
            Destroy(hirePanelUI.gameObject);
        }
    }
    public int GetTotalAttack()
    {
        int totalAttack = 0;
    
        // 1. Добавляем атаку самого игрока из PlayerStats
        if (playerStats != null) totalAttack += playerStats.Attack;

        // 2. Добавляем атаку каждого члена команды
        foreach (var member in CurrentTeam)
        {
            totalAttack += member.unitData.attackBonus; // Убедитесь, что в UnitData есть поле attackPower
        }

        return totalAttack;
    }

// Методы для наград и штрафов
    public void AddMoney(int amount) => playerInventory.AddMoney(amount);
    public void RemoveMoney(int amount) => playerInventory.TrySpendMoney(amount); // Используем ваш метод траты
}