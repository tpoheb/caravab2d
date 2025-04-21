using UnityEngine;
using TMPro;
using System.Collections.Generic;
using UnityEngine.UI;

public class HirePanelUI : MonoBehaviour
{
    [Header("UI Elements")]
    [SerializeField] private Transform availableUnitsContainer;
    [SerializeField] private Transform currentTeamContainer;
    [SerializeField] private TextMeshProUGUI moneyText;
    [SerializeField] private GameObject unitButtonPrefab;

    private TeamSystem teamSystem;

    public void Initialize(TeamSystem system)
    {
        teamSystem = system;

        // Получаем ссылки автоматически, если не назначены
        if (moneyText == null)
            moneyText = GetComponentInChildren<TextMeshProUGUI>();
    }

    public void UpdateUI(List<UnitData> availableUnits, List<TeamMember> currentTeam, int currentMoney)
    {
        moneyText.text = $"Золото: {currentMoney}";

        ClearContainer(availableUnitsContainer);
        ClearContainer(currentTeamContainer);

        foreach (var unit in availableUnits)
        {
            CreateUnitButton(unit, availableUnitsContainer, () => teamSystem.TryHireUnit(unit));
        }

        foreach (var member in currentTeam)
        {
            CreateUnitButton(member.unitData, currentTeamContainer, () => teamSystem.FireUnit(member));
        }
    }

    private void CreateUnitButton(UnitData data, Transform parent, UnityEngine.Events.UnityAction action)
    {
        var button = Instantiate(unitButtonPrefab, parent);
        var text = button.GetComponentInChildren<TextMeshProUGUI>();
        text.text = $"{data.unitName}\nЦена: {data.hireCost}";

        button.GetComponent<Button>().onClick.AddListener(action);
    }

    private void ClearContainer(Transform container)
    {
        foreach (Transform child in container)
        {
            Destroy(child.gameObject);
        }
    }

    public void ShowPanel()
    {
        gameObject.SetActive(true);
    }

    public void ClosePanel()
    {
        gameObject.SetActive(false);
    }
}