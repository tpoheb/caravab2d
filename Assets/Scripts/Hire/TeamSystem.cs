using System.Collections.Generic;
using UnityEngine;

public class TeamSystem : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private PlayerInventory playerInventory;
    [SerializeField] private PlayerStats playerStats;
    [SerializeField] private HirePanelUI hirePanelUI;

    [Header("Available Units")]
    [SerializeField] private List<UnitData> availableUnits = new List<UnitData>();

    [Header("Current Team")]
    [SerializeField] private List<TeamMember> currentTeam = new List<TeamMember>();

    private void Start()
    {
        hirePanelUI.Initialize(this, playerInventory, playerStats);
    }

    public void OpenHirePanel()
    {
        hirePanelUI.UpdateUI(availableUnits, currentTeam, playerInventory.money);
        hirePanelUI.gameObject.SetActive(true);
    }

    public bool TryHireUnit(UnitData unitData)
    {
        if (!playerInventory.TrySpendMoney(unitData.hireCost))
        {
            Debug.Log("Недостаточно денег!");
            return false;
        }

        var newMember = new TeamMember(unitData);
        currentTeam.Add(newMember);
        newMember.ApplyBonuses(playerStats);

        hirePanelUI.UpdateUI(availableUnits, currentTeam, playerInventory.money);
        return true;
    }

    public void FireUnit(TeamMember member)
    {
        member.RemoveBonuses(playerStats);
        currentTeam.Remove(member);
        hirePanelUI.UpdateUI(availableUnits, currentTeam, playerInventory.money);
    }

    public void PaySalaries()
    {
        int totalSalary = 0;
        foreach (var member in currentTeam)
            totalSalary += member.unitData.salaryPerTurn;

        playerInventory.TrySpendMoney(totalSalary);
    }

    public void CloseHirePanel()
    {
        hirePanelUI.UpdateUI(availableUnits, currentTeam, playerInventory.money);
        hirePanelUI.gameObject.SetActive(false);
    }
}