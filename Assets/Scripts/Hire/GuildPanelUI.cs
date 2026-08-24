using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// UI панель выбора гильдии.
/// Открывается из CityPanel по кнопке «Гильдии» (только в столице).
/// Строится по аналогии с HirePanelUI.
/// </summary>
public class GuildPanelUI : MonoBehaviour
{
    [Header("UI — Панель")]
    [SerializeField] private TextMeshProUGUI titleText;
    [SerializeField] private TextMeshProUGUI moneyText;
    [SerializeField] private TextMeshProUGUI currentGuildText;
    [SerializeField] private Button closeButton;
    [SerializeField] private Button leaveGuildButton;

    [Header("UI — Контейнер")]
    [SerializeField] private Transform guildsContainer;

    [Header("Префаб карточки гильдии")]
    [SerializeField] private GameObject guildCardPrefab; // должен иметь компонент GuildCardUI

    [Header("Данные")]
    [SerializeField] private List<GuildData> allGuilds = new List<GuildData>();

    private GuildSystem _guildSystem;

    // ─────────────────────────────────────────────────────────────────────

    private void Awake()
    {
        if (closeButton != null)
            closeButton.onClick.AddListener(ClosePanel);

        if (leaveGuildButton != null)
            leaveGuildButton.onClick.AddListener(OnLeaveClicked);
    }

    /// <summary>
    /// Открывает панель гильдий.
    /// </summary>
    /// <param name="guildSystem">Ссылка на GuildSystem игрока.</param>
    public void OpenPanel(GuildSystem guildSystem)
    {
        _guildSystem = guildSystem;

        if (_guildSystem == null)
        {
            Debug.LogError("[GuildPanelUI] GuildSystem не передан!", this);
            return;
        }

        RefreshContent();
        gameObject.SetActive(true);
    }

    /// <summary>
    /// Перестраивает список гильдий и обновляет информацию о текущей гильдии.
    /// </summary>
    public void RefreshContent()
    {
        if (_guildSystem == null) return;

        // Деньги игрока — берём из PlayerInventory через GuildSystem
        // GuildSystem не имеет прямой ссылки на PlayerInventory.Money в паблике,
        // но мы можем взять деньги через FindAnyObjectByType или сделать поле публичным.
        // Пока используем упрощённый вариант — обновим позже при интеграции.
        UpdateMoneyDisplay();

        // Текущая гильдия
        if (currentGuildText != null)
        {
            if (_guildSystem.CurrentGuild != null)
            {
                currentGuildText.text = $"Ваша гильдия: {_guildSystem.CurrentGuild.guildName}";
                currentGuildText.gameObject.SetActive(true);
            }
            else
            {
                currentGuildText.text = "Вы не состоите в гильдии";
                currentGuildText.gameObject.SetActive(true);
            }
        }

        // Кнопка выхода
        if (leaveGuildButton != null)
            leaveGuildButton.gameObject.SetActive(_guildSystem.HasGuild);

        // Список гильдий
        ClearContainer(guildsContainer);

        foreach (GuildData guild in allGuilds)
        {
            if (guild == null) continue;

            bool isCurrent = _guildSystem.CurrentGuild == guild;
            GuildData captured = guild;

            SpawnCard(
                data: guild,
                parent: guildsContainer,
                onAction: () => OnJoinClicked(captured),
                interactable: !isCurrent,
                isCurrentGuild: isCurrent
            );
        }
    }

    // ─────────────────────────────────────────────────────────────────────

    private void OnJoinClicked(GuildData guild)
    {
        if (_guildSystem == null || guild == null) return;

        bool success = _guildSystem.TryJoinGuild(guild);
        if (success)
            RefreshContent();
    }

    private void OnLeaveClicked()
    {
        if (_guildSystem == null) return;

        _guildSystem.LeaveGuild();
        RefreshContent();
    }

    private void SpawnCard(GuildData data, Transform parent,
                           System.Action onAction, bool interactable, bool isCurrentGuild)
    {
        if (guildCardPrefab == null)
        {
            Debug.LogError("[GuildPanelUI] guildCardPrefab не назначен!", this);
            return;
        }

        GameObject go = Instantiate(guildCardPrefab, parent);
        GuildCardUI card = go.GetComponent<GuildCardUI>();

        if (card == null)
        {
            Debug.LogError("[GuildPanelUI] Префаб не содержит компонент GuildCardUI!", go);
            return;
        }

        card.Setup(data, onAction, interactable, isCurrentGuild);
    }

    private void UpdateMoneyDisplay()
    {
        // Берём деньги через FindAnyObjectByType — временное решение для независимости UI
        if (moneyText != null)
        {
            PlayerInventory inv = Object.FindAnyObjectByType<PlayerInventory>();
            if (inv != null)
                moneyText.text = $"{inv.Money}";
            else
                moneyText.text = "—";
        }
    }

    private void ClearContainer(Transform container)
    {
        foreach (Transform child in container)
            Destroy(child.gameObject);
    }

    public void ClosePanel() => gameObject.SetActive(false);
}