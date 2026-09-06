using System.Collections.Generic;
using StorySystem;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Тип записи журнала. Определяет, в какую вкладку попадёт запись и какую иконку показать.
/// </summary>
public enum JournalEntryType
{
    TradeBuy,      // покупка товара
    TradeSell,     // продажа товара
    Quest          // нарративное событие / задание
}

/// <summary>
/// Сериализуемые данные одной записи журнала.
/// </summary>
[System.Serializable]
public class JournalEntryData
{
    public JournalEntryType type;
    public string message;      // "Куплено Железо x5 за 150 золота в Каменске"
    public int turnNumber;      // номер хода, на котором произошло событие

    public JournalEntryData() { }

    public JournalEntryData(JournalEntryType t, string msg, int turn)
    {
        type = t;
        message = msg;
        turnNumber = turn;
    }
}

/// <summary>
/// Журнал игрока. Единая система с двумя вкладками: «Торговля» и «Квесты».
/// Подписывается на GameEvents (OnItemBought, OnItemSold, OnQuestJournalEntry)
/// и накапливает записи, отображая их в прокручиваемых списках.
///
/// Подключи этот компонент к GameObject "GameJournal" в Canvas.
/// </summary>
public class GameJournal : MonoBehaviour
{
    public static GameJournal Instance { get; private set; }

    [Header("Панели вкладок")]
    [SerializeField] private GameObject tradePanel;   // ScrollRect "Торговля"
    [SerializeField] private GameObject questPanel;   // ScrollRect "Квесты"

    [Header("Контейнеры записей")]
    [Tooltip("VerticalLayoutGroup внутри tradePanel (место спавна записей)")]
    [SerializeField] private Transform tradeEntryContainer;
    [Tooltip("VerticalLayoutGroup внутри questPanel (место спавна записей)")]
    [SerializeField] private Transform questEntryContainer;

    [Header("Префаб записи")]
    [Tooltip("Prefab с компонентом JournalEntryUI")]
    [SerializeField] private GameObject entryPrefab;

    [Header("Управление окном")]
    [SerializeField] private GameObject journalPanel;
    [SerializeField] private Button toggleButton;     // кнопка в TopBar
    [SerializeField] private Button closeButton;      // кнопка закрытия внутри окна

    [Header("Кнопки вкладок")]
    [SerializeField] private Button tabTradeButton;   // вкладка «Торговля»
    [SerializeField] private Button tabQuestButton;   // вкладка «Квесты»

    // Раздельные списки записей для двух вкладок
    private readonly List<JournalEntryData> _tradeEntries = new List<JournalEntryData>();
    private readonly List<JournalEntryData> _questEntries = new List<JournalEntryData>();

    // ──────────────────────────────────────────────
    // Lifecycle
    // ──────────────────────────────────────────────

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;

        ValidateReferences();

        if (journalPanel != null)
            journalPanel.SetActive(false);
    }

    private void OnEnable()
    {
        GameEvents.OnItemBought        += HandleItemBought;
        GameEvents.OnItemSold          += HandleItemSold;
        GameEvents.OnQuestJournalEntry += HandleQuestEntry;
    }

    private void OnDisable()
    {
        GameEvents.OnItemBought        -= HandleItemBought;
        GameEvents.OnItemSold          -= HandleItemSold;
        GameEvents.OnQuestJournalEntry -= HandleQuestEntry;
    }

    private void Start()
    {
        if (toggleButton != null) toggleButton.onClick.AddListener(Toggle);
        if (closeButton != null) closeButton.onClick.AddListener(Close);
        if (tabTradeButton != null) tabTradeButton.onClick.AddListener(ShowTradeTab);
        if (tabQuestButton != null) tabQuestButton.onClick.AddListener(ShowQuestTab);

        ShowTradeTab();
    }

    private void OnDestroy()
    {
        if (toggleButton != null) toggleButton.onClick.RemoveListener(Toggle);
        if (closeButton != null) closeButton.onClick.RemoveListener(Close);
        if (tabTradeButton != null) tabTradeButton.onClick.RemoveListener(ShowTradeTab);
        if (tabQuestButton != null) tabQuestButton.onClick.RemoveListener(ShowQuestTab);
    }

    // ──────────────────────────────────────────────
    // Обработчики событий торговли
    // ──────────────────────────────────────────────

    private void HandleItemBought(string itemName, int quantity, int cost, string cityName)
    {
        string message = $"Куплено {itemName} x{quantity} за {cost} золота в {cityName}";
        var entry = new JournalEntryData(JournalEntryType.TradeBuy, message, CurrentTurn());
        _tradeEntries.Add(entry);
        SpawnEntry(entry, tradeEntryContainer);
    }

    private void HandleItemSold(string itemName, int quantity, int value, string cityName)
    {
        string message = $"Продано {itemName} x{quantity} за {value} золота в {cityName}";
        var entry = new JournalEntryData(JournalEntryType.TradeSell, message, CurrentTurn());
        _tradeEntries.Add(entry);
        SpawnEntry(entry, tradeEntryContainer);
    }

    private void HandleQuestEntry(string title, string body)
    {
        string message = string.IsNullOrEmpty(body) ? title : $"{title}: {body}";
        var entry = new JournalEntryData(JournalEntryType.Quest, message, CurrentTurn());
        _questEntries.Add(entry);
        SpawnEntry(entry, questEntryContainer);
    }

    // ──────────────────────────────────────────────
    // UI: открытие / закрытие / вкладки
    // ──────────────────────────────────────────────

    public void Toggle()
    {
        if (journalPanel == null) return;
        journalPanel.SetActive(!journalPanel.activeSelf);
    }

    public void Close()
    {
        if (journalPanel != null)
            journalPanel.SetActive(false);
    }

    public void ShowTradeTab()
    {
        if (tradePanel != null) tradePanel.SetActive(true);
        if (questPanel != null) questPanel.SetActive(false);
    }

    public void ShowQuestTab()
    {
        if (questPanel != null) questPanel.SetActive(true);
        if (tradePanel != null) tradePanel.SetActive(false);
    }

    // ──────────────────────────────────────────────
    // Вспомогательное
    // ──────────────────────────────────────────────

    private int CurrentTurn()
        => (GameManager.Instance != null) ? GameManager.Instance.TurnNumber : 0;

    private void SpawnEntry(JournalEntryData data, Transform container)
    {
        if (entryPrefab == null || container == null) return;

        GameObject go = Instantiate(entryPrefab, container);
        var ui = go.GetComponent<JournalEntryUI>();
        if (ui != null)
            ui.Setup(data);
    }

    // ──────────────────────────────────────────────
    // Публичный доступ к данным (для сохранения)
    // ──────────────────────────────────────────────

    public List<JournalEntryData> GetTradeEntries()  => new List<JournalEntryData>(_tradeEntries);
    public List<JournalEntryData> GetQuestEntries()  => new List<JournalEntryData>(_questEntries);

    public void Clear()
    {
        _tradeEntries.Clear();
        _questEntries.Clear();
        ClearContainer(tradeEntryContainer);
        ClearContainer(questEntryContainer);
    }

    private static void ClearContainer(Transform container)
    {
        if (container == null) return;
        foreach (Transform child in container)
            Destroy(child.gameObject);
    }

    private void ValidateReferences()
    {
        if (tradePanel == null)          Debug.LogError("[GameJournal] tradePanel не назначен");
        if (questPanel == null)          Debug.LogError("[GameJournal] questPanel не назначен");
        if (tradeEntryContainer == null) Debug.LogError("[GameJournal] tradeEntryContainer не назначен");
        if (questEntryContainer == null) Debug.LogError("[GameJournal] questEntryContainer не назначен");
        if (entryPrefab == null)         Debug.LogError("[GameJournal] entryPrefab не назначен");
        if (journalPanel == null)        Debug.LogWarning("[GameJournal] journalPanel не назначен");
        if (toggleButton == null)        Debug.LogWarning("[GameJournal] toggleButton не назначен");
        if (tabTradeButton == null)      Debug.LogWarning("[GameJournal] tabTradeButton не назначен");
        if (tabQuestButton == null)      Debug.LogWarning("[GameJournal] tabQuestButton не назначен");
    }
}