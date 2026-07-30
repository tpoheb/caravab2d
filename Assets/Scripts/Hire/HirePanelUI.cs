using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class HirePanelUI : MonoBehaviour
{
    [Header("UI — Панель")]
    [SerializeField] private TextMeshProUGUI cityNameText;
    [SerializeField] private TextMeshProUGUI moneyText;
    [SerializeField] private Button closeButton;

    [Header("UI — Контейнеры")]
    [SerializeField] private Transform availableUnitsContainer;
    [SerializeField] private Transform currentTeamContainer;

    [Header("Префаб карточки юнита")]
    [SerializeField] private GameObject unitCardPrefab; // должен иметь компонент UnitCardUI

    private TeamSystem teamSystem;

    // ─────────────────────────────────────────────────────────────────────

    private void Awake()
    {
        if (closeButton != null)
            closeButton.onClick.AddListener(ClosePanel);
    }

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

    /// <summary>
    /// Вызывается после найма / увольнения для обновления обоих списков.
    /// </summary>
    public void RefreshContent()
    {
        if (teamSystem == null) return;

        moneyText.text = $"{teamSystem.CurrentMoney}";

        ClearContainer(availableUnitsContainer);
        ClearContainer(currentTeamContainer);

        // Доступные юниты — клик = Нанять
        foreach (UnitData unit in teamSystem.AvailableUnits)
        {
            bool alreadyHired   = teamSystem.CurrentTeam.Exists(m => m.unitData == unit);
            if (alreadyHired) continue;

            bool specialtyTaken = teamSystem.IsSpecialtyTaken(unit.specialty);
            UnitData captured   = unit;

            SpawnCard(
                data:        unit,
                parent:      availableUnitsContainer,
                onAction:    () => OnHireClicked(captured),
                interactable: !specialtyTaken,
                showCost:    true
            );
        }

        // Текущая команда — клик = Уволить
        foreach (TeamMember member in teamSystem.CurrentTeam)
        {
            TeamMember captured = member;

            SpawnCard(
                data:        member.unitData,
                parent:      currentTeamContainer,
                onAction:    () => OnFireClicked(captured),
                interactable: true,
                showCost:    false
            );
        }
    }

    // ─────────────────────────────────────────────────────────────────────

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

    private void SpawnCard(UnitData data, Transform parent,
                           System.Action onAction, bool interactable, bool showCost)
    {
        if (unitCardPrefab == null)
        {
            Debug.LogError("[HirePanelUI] unitCardPrefab не назначен!", this);
            return;
        }

        GameObject go   = Instantiate(unitCardPrefab, parent);
        UnitCardUI card = go.GetComponent<UnitCardUI>();

        if (card == null)
        {
            Debug.LogError("[HirePanelUI] Префаб не содержит компонент UnitCardUI!", go);
            return;
        }

        card.Setup(data, onAction, interactable, showCost);
    }

    private void UpdateCityName(City city)
    {
        if (cityNameText != null)
            cityNameText.text = city?.CityName ?? "Неизвестный город";
    }

    private void ClearContainer(Transform container)
    {
        foreach (Transform child in container)
            Destroy(child.gameObject);
    }

    public void ClosePanel() => gameObject.SetActive(false);
}