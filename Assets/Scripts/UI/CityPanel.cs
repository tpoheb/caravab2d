using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;
using System;

public class CityPanel : MonoBehaviour
{
    public static event Action<PathCellInitializer> OnPathSelectedEvent;

    [Header("UI Настройки")]
    [SerializeField] private GameObject pathButtonPrefab;
    [SerializeField] private Transform pathButtonsContainer;
    [SerializeField] private Button hireTeamButton;
    [SerializeField] private Button buyGoodsButton;
    [SerializeField] private TMP_Text cityNameText;
    [SerializeField] private TMP_Text cityInfoText;
    [SerializeField] private TMP_Text cityRumorsText;

    [Header("Системы")]
    [SerializeField] private TeamSystem teamSystem;
    [SerializeField] private HirePanelUI hirePanelUI;

    private City _currentCity;
    private readonly List<Button> _pathButtons = new List<Button>();

    private void Awake() => ValidateReferences();

    public void OpenPanel(City city)
    {
        if (city == null) return;
        if (gameObject.activeSelf && _currentCity == city) return;

        _currentCity = city;
        BuildPathButtons();
        SetupActionButtons();
        UpdateCityNameUI(city.CityName);
        UpdateCityInfoUI(city);

        gameObject.SetActive(true);
        Debug.Log($"[CityPanel] Открыта для: {_currentCity.CityName}");
    }

    private void UpdateCityNameUI(string cityName)
    {
        if (cityNameText != null)
            cityNameText.text = cityName ?? "Неизвестный город";
    }

    private void UpdateCityInfoUI(City city)
    {
        if (cityInfoText != null)
        {
            string info = city.Info;
            cityInfoText.text = string.IsNullOrWhiteSpace(info)
                ? "Информация о городе отсутствует."
                : info;
        }

        if (cityRumorsText != null)
        {
            var rumors = city.Rumors;
            if (rumors == null || rumors.Count == 0)
            {
                cityRumorsText.text = "Слухи отсутствуют.";
            }
            else
            {
                var builder = new System.Text.StringBuilder();
                for (int i = 0; i < rumors.Count; i++)
                {
                    if (i > 0) builder.AppendLine();
                    builder.Append("• ").Append(rumors[i]);
                }
                cityRumorsText.text = builder.ToString();
            }
        }
    }

    private void BuildPathButtons()
    {
        ClearPathButtons();

        if (_currentCity.Paths == null || _currentCity.Paths.Count == 0) return;

        foreach (PathCellInitializer path in _currentCity.Paths)
        {
            if (path == null || path.FinishCity == null) continue;

            GameObject buttonObj = Instantiate(pathButtonPrefab, pathButtonsContainer);
            Button pathButton = buttonObj.GetComponent<Button>();

            TMP_Text buttonText = pathButton.GetComponentInChildren<TMP_Text>();
            if (buttonText != null)
                buttonText.text = $"Путь к {path.FinishCity.CityName}";

            PathCellInitializer capturedPath = path;
            pathButton.onClick.AddListener(() => OnPathButtonClicked(capturedPath));

            _pathButtons.Add(pathButton);
        }
    }

    private void OnPathButtonClicked(PathCellInitializer path)
    {
        if (path == null) return;

        if (GameManager.Instance != null)
            GameManager.Instance.OnPathSelected(path);
        else
            Debug.LogError("[CityPanel] GameManager.Instance не найден!");

        OnPathSelectedEvent?.Invoke(path);
        ClosePanel();
    }

    private void SetupActionButtons()
    {
        hireTeamButton.onClick.RemoveAllListeners();
        buyGoodsButton.onClick.RemoveAllListeners();

        hireTeamButton.onClick.AddListener(OnHireTeamClicked);
        buyGoodsButton.onClick.AddListener(OnBuyGoodsClicked);
    }

    private void OnHireTeamClicked()
    {
        if (teamSystem == null || hirePanelUI == null)
        {
            Debug.LogError("[CityPanel] TeamSystem или HirePanelUI не назначены в Inspector!", this);
            return;
        }

        hirePanelUI.OpenPanel(_currentCity, teamSystem);
        
    }

    private void OnBuyGoodsClicked()
    {
        if (_currentCity == null) return;
        TradeSystem.RequestTrade(_currentCity);
        ClosePanel();
    }

    private void ClearPathButtons()
    {
        foreach (var btn in _pathButtons)
        {
            if (btn != null)
            {
                btn.onClick.RemoveAllListeners();
                Destroy(btn.gameObject);
            }
        }
        _pathButtons.Clear();
    }

    public void ClosePanel() => gameObject.SetActive(false);

    private void ValidateReferences()
    {
        if (cityNameText == null) Debug.LogWarning("[CityPanel] CityNameText не назначен!");
        if (pathButtonPrefab == null) Debug.LogError("[CityPanel] pathButtonPrefab не назначен!");
        if (teamSystem == null) Debug.LogError("[CityPanel] TeamSystem не назначен!");
        if (hirePanelUI == null) Debug.LogError("[CityPanel] HirePanelUI не назначен!");
    }
}