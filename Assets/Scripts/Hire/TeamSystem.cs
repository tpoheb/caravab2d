using System.Collections.Generic;
using UnityEngine;

public class TeamSystem : MonoBehaviour
{
    [Header("Player Systems")]
    [SerializeField] private PlayerInventory playerInventory;
    [SerializeField] private PlayerStats playerStats;

    public List<TeamMember> CurrentTeam { get; private set; } = new List<TeamMember>();
    public int CurrentMoney => playerInventory.Money;

    private void Awake()
    {
        if (playerInventory == null)
            playerInventory = GetComponent<PlayerInventory>();
        if (playerStats == null)
            playerStats = GetComponent<PlayerStats>();
    }

    public bool TryHireUnit(UnitData unitData)
    {
        if (IsSpecialtyTaken(unitData.specialty))
        {
            Debug.Log($"[TeamSystem] В команде уже есть {unitData.specialty}!");
            return false;
        }
        if (!playerInventory.TrySpendMoney(unitData.hireCost))
        {
            Debug.Log("[TeamSystem] Недостаточно денег!");
            return false;
        }

        var newMember = new TeamMember(unitData);
        CurrentTeam.Add(newMember);
        newMember.ApplyBonuses(playerStats);
        return true;
    }

    public bool IsSpecialtyTaken(UnitSpecialty specialty)
    {
        foreach (var member in CurrentTeam)
            if (member.unitData.specialty == specialty)
                return true;
        return false;
    }

    public void FireUnit(TeamMember member)
    {
        member.RemoveBonuses(playerStats);
        CurrentTeam.Remove(member);
    }

    /// <summary>
    /// Увольняет случайного члена команды без выплаты.
    /// Вызывается ShadowEffectManager для эффекта "Зов Мглы".
    /// Возвращает false если команда пуста.
    /// </summary>
    public bool FireRandomCrewMember()
    {
        if (CurrentTeam.Count == 0)
            return false;

        int index = Random.Range(0, CurrentTeam.Count);
        TeamMember victim = CurrentTeam[index];

        Debug.Log($"[TeamSystem] Зов Мглы: уходит {victim.unitData.unitName}.");
        FireUnit(victim);
        return true;
    }

    public void PaySalaries()
    {
        int total = CalculateTotalSalary();
        if (!playerInventory.TrySpendMoney(total))
        {
            Debug.LogWarning($"[TeamSystem] Не хватает денег на зарплаты: {total}");
            HandleSalaryShortage(total);
        }
    }

    private int CalculateTotalSalary()
    {
        int total = 0;
        foreach (var member in CurrentTeam)
            total += member.unitData.salaryPerTurn;
        return total;
    }

    private void HandleSalaryShortage(int requiredAmount)
    {
        // Логика штрафов или увольнений
    }

    public int GetTotalAttack()
    {
        int total = playerStats != null ? playerStats.Attack : 0;
        foreach (var member in CurrentTeam)
            total += member.unitData.attackBonus;
        return total;
    }

    public void AddMoney(int amount) => playerInventory.AddMoney(amount);
    public void RemoveMoney(int amount) => playerInventory.TrySpendMoney(amount);
}