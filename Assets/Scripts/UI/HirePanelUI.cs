using UnityEngine;
using TMPro;
using System.Collections.Generic;
using UnityEngine.UI;

public class HirePanelUI : MonoBehaviour
{
    [Header("UI Elements")]
    [SerializeField] private TextMeshProUGUI cityNameText;
    [SerializeField] private Transform availableUnitsContainer;
    [SerializeField] private Transform currentTeamContainer;
    [SerializeField] private TextMeshProUGUI moneyText;
    [SerializeField] private GameObject unitButtonPrefab;

    private TeamSystem teamSystem;

    public void OpenPanel(City city, TeamSystem system)
    {
        teamSystem = system;

        if (teamSystem == null)
        {
            Debug.LogError("[HirePanelUI] TeamSystem не передан!", this);
            return;
        }

        UpdateCityName(city);
        RefreshContent();
        gameObject.SetActive(true);
    }

    private void UpdateCityName(City city)
    {
        if (cityNameText != null)
            cityNameText.text = city?.CityName ?? "Неизвестный город";
    }

    // Вызывается после найма/увольнения для обновления списков
    public void RefreshContent()
    {
        if (teamSystem == null) return;

        moneyText.text = $"{teamSystem.CurrentMoney}";

        ClearContainer(availableUnitsContainer);
        ClearContainer(currentTeamContainer);

        foreach (var unit in teamSystem.AvailableUnits)
            CreateUnitButton(unit, availableUnitsContainer, () => OnHireClicked(unit));

        foreach (var member in teamSystem.CurrentTeam)
            CreateUnitButton(member.unitData, currentTeamContainer, () => OnFireClicked(member));
    }

    private void OnHireClicked(UnitData unit)
    {
        teamSystem.TryHireUnit(unit);
        RefreshContent();
    }

    private void OnFireClicked(TeamMember member)
    {
        teamSystem.FireUnit(member);
        RefreshContent();
    }

    private void CreateUnitButton(UnitData data, Transform parent, UnityEngine.Events.UnityAction action)
    {
        var button = Instantiate(unitButtonPrefab, parent);
        var text = button.GetComponentInChildren<TextMeshProUGUI>();
        text.text = $"{data.unitName}\nЦена: {data.hireCost}";

        bool specialtyTaken = teamSystem.IsSpecialtyTaken(data.specialty);
        button.GetComponent<Button>().interactable = !specialtyTaken;
    
        if (!specialtyTaken)
            button.GetComponent<Button>().onClick.AddListener(action);
    }

    private void ClearContainer(Transform container)
    {
        foreach (Transform child in container)
            Destroy(child.gameObject);
    }

    public void ClosePanel() => gameObject.SetActive(false);
}