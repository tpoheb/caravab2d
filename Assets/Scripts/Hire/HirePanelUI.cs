using System.Collections.Generic;
using UnityEngine;
using TMPro;
using UnityEngine.UI;

public class HirePanelUI : MonoBehaviour
{
    [Header("UI Elements")]
    [SerializeField] private Transform availableUnitsContainer;
    [SerializeField] private Transform currentTeamContainer;
    [SerializeField] private TextMeshProUGUI moneyText;
    [SerializeField] private GameObject unitButtonPrefab;

    private TeamSystem teamSystem;

    public void Initialize(TeamSystem system, PlayerInventory playerInventory, PlayerStats playerStats)
    {
        teamSystem = system;
    }

    public void UpdateUI(List<UnitData> availableUnits, List<TeamMember> currentTeam, int currentMoney)
    {
        moneyText.text = $"Золото: {currentMoney}";
        UpdateAvailableUnitsDisplay(availableUnits);
        UpdateCurrentTeamDisplay(currentTeam);
    }

    private void UpdateAvailableUnitsDisplay(List<UnitData> units)
    {
        ClearContainer(availableUnitsContainer);

        foreach (var unit in units)
        {
            var button = Instantiate(unitButtonPrefab, availableUnitsContainer);
            SetupUnitButton(button, unit, () => teamSystem.TryHireUnit(unit));
        }
    }

    private void UpdateCurrentTeamDisplay(List<TeamMember> teamMembers)
    {
        ClearContainer(currentTeamContainer);

        foreach (var member in teamMembers)
        {
            var button = Instantiate(unitButtonPrefab, currentTeamContainer);
            SetupUnitButton(button, member.unitData, () => teamSystem.FireUnit(member));
        }
    }

    private void SetupUnitButton(GameObject button, UnitData data, UnityEngine.Events.UnityAction action)
    {
        var text = button.GetComponentInChildren<TextMeshProUGUI>();
        text.text = $"{data.unitName}\n" +
                   $"Атака: +{data.attackBonus}\n" +
                   $"Выгода: +{data.bargainBonus}\n" +
                   $"Груз: +{data.capacityBonus}\n" +
                   $"Жалование: {data.salaryPerTurn}/ход";

        var buttonComp = button.GetComponent<Button>();
        buttonComp.onClick.RemoveAllListeners();
        buttonComp.onClick.AddListener(action);
    }

    private void ClearContainer(Transform container)
    {
        foreach (Transform child in container)
        {
            Destroy(child.gameObject);
        }
    }
}