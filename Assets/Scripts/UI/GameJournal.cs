using System.Collections.Generic;
using StorySystem;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Сериализуемые данные одной записи журнала.
/// </summary>
[System.Serializable]
public class JournalEntryData
{
    public string message;
    public int turnNumber;

    public JournalEntryData() { }

    public JournalEntryData(string msg, int turn)
    {
        message = msg;
        turnNumber = turn;
    }
}

/// <summary>
/// Журнал квестов. Подписывается на GameEvents.OnQuestJournalEntry
/// и отображает записи в прокручиваемом списке.
///
/// Структура сцены:
///   JournalRoot (этот компонент, всегда активен)
///   └── JournalPanel (поле journalPanel, скрывается/показывается)
///       └── QuestEntryContainer (VerticalLayoutGroup)
/// </summary>
public class GameJournal : MonoBehaviour
{
    public static GameJournal Instance { get; private set; }

    [Header("Управление окном")]
    [SerializeField] private GameObject journalPanel;
    [SerializeField] private Button toggleButton;
    [SerializeField] private Button closeButton;

    [Header("Контейнер записей")]
    [Tooltip("VerticalLayoutGroup — место спавна записей")]
    [SerializeField] private Transform questEntryContainer;

    [Header("Префаб записи")]
    [Tooltip("Prefab с компонентом JournalEntryUI")]
    [SerializeField] private GameObject entryPrefab;

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
        GameEvents.OnQuestJournalEntry += HandleQuestEntry;
    }

    private void OnDisable()
    {
        GameEvents.OnQuestJournalEntry -= HandleQuestEntry;
    }

    private void Start()
    {
        if (toggleButton != null) toggleButton.onClick.AddListener(Toggle);
        if (closeButton  != null) closeButton.onClick.AddListener(Close);
    }

    private void OnDestroy()
    {
        if (toggleButton != null) toggleButton.onClick.RemoveListener(Toggle);
        if (closeButton  != null) closeButton.onClick.RemoveListener(Close);
    }

    // ──────────────────────────────────────────────
    // Обработчик события
    // ──────────────────────────────────────────────

    private void HandleQuestEntry(string title, string body)
    {
        Debug.Log($"[GameJournal] HandleQuestEntry: title='{title}', body='{body}'");

        string message = string.IsNullOrEmpty(body) ? title : $"{title}: {body}";
        var entry = new JournalEntryData(message, CurrentTurn());
        _questEntries.Add(entry);
        SpawnEntry(entry);
    }

    // ──────────────────────────────────────────────
    // UI
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

    // ──────────────────────────────────────────────
    // Вспомогательное
    // ──────────────────────────────────────────────

    private int CurrentTurn()
        => (GameManager.Instance != null) ? GameManager.Instance.TurnNumber : 0;

    private void SpawnEntry(JournalEntryData data)
    {
        if (entryPrefab == null || questEntryContainer == null)
        {
            Debug.LogError($"[GameJournal] SpawnEntry: prefab={entryPrefab?.name ?? "NULL"}, container={questEntryContainer?.name ?? "NULL"}");
            return;
        }

        GameObject go = Instantiate(entryPrefab, questEntryContainer);
        var ui = go.GetComponent<JournalEntryUI>();
        if (ui != null)
            ui.Setup(data);
        else
            Debug.LogError($"[GameJournal] JournalEntryUI не найден на префабе {go.name}!");
    }

    // ──────────────────────────────────────────────
    // Тест из инспектора
    // ──────────────────────────────────────────────

    [ContextMenu("TEST: добавить квестовую запись")]
    private void TestAddQuestEntry()
        => HandleQuestEntry("Тестовый квест", "Тестовое тело записи.");

    // ──────────────────────────────────────────────
    // Публичный доступ к данным (для сохранения)
    // ──────────────────────────────────────────────

    public List<JournalEntryData> GetQuestEntries() => new List<JournalEntryData>(_questEntries);

    public void Clear()
    {
        _questEntries.Clear();
        if (questEntryContainer == null) return;
        foreach (Transform child in questEntryContainer)
            Destroy(child.gameObject);
    }

    // ──────────────────────────────────────────────
    // Валидация
    // ──────────────────────────────────────────────

    private void ValidateReferences()
    {
        if (journalPanel == null)        Debug.LogWarning("[GameJournal] journalPanel не назначен");
        if (toggleButton == null)        Debug.LogWarning("[GameJournal] toggleButton не назначен");
        if (questEntryContainer == null) Debug.LogError("[GameJournal] questEntryContainer не назначен");
        if (entryPrefab == null)         Debug.LogError("[GameJournal] entryPrefab не назначен");
    }
}