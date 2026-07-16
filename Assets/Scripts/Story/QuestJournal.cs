using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace StorySystem
{
    /// <summary>
    /// Дневник заданий. Хранит записи и отображает их в UI.
    /// Подключи этот скрипт к GameObject "QuestJournal" на сцене.
    ///
    /// Иерархия в Canvas (пример):
    ///   Canvas
    ///     └── JournalPanel (Panel) ← toggleable
    ///           └── EntryContainer (VerticalLayoutGroup)
    ///                 └── EntryPrefab (Prefab с TextMeshProUGUI)
    /// </summary>
    public class QuestJournal : MonoBehaviour
    {
        [Header("UI")]
        [SerializeField] private GameObject journalPanel;
        [SerializeField] private Transform entryContainer;
        [SerializeField] private GameObject entryPrefab;   // Prefab: содержит Title + Body TMP
        [SerializeField] private Button toggleButton;

        // Все записи в памяти (для сохранения)
        private readonly List<JournalEntryData> _entries = new List<JournalEntryData>();

        // ──────────────────────────────────────────────
        // Lifecycle
        // ──────────────────────────────────────────────

        private void Awake()
        {
            if (toggleButton != null)
                toggleButton.onClick.AddListener(ToggleJournal);

            journalPanel.SetActive(false);
        }

        // ──────────────────────────────────────────────
        // Публичный API
        // ──────────────────────────────────────────────

        /// <summary>
        /// Добавляет запись в дневник на основе нарративной вставки.
        /// Вызывается автоматически из StoryManager.
        /// </summary>
        public void AddEntry(StoryBeat beat)
        {
            if (string.IsNullOrEmpty(beat.journalEntry)) return;

            var data = new JournalEntryData
            {
                title = beat.journalTitle,
                body  = beat.journalEntry
            };

            _entries.Add(data);
            SpawnEntryUI(data);
        }

        public void ToggleJournal() => journalPanel.SetActive(!journalPanel.activeSelf);

        /// <summary>
        /// Возвращает все записи для сохранения.
        /// </summary>
        public List<JournalEntryData> GetAllEntries() => new List<JournalEntryData>(_entries);

        /// <summary>
        /// Восстанавливает дневник из SaveData.
        /// </summary>
        public void LoadEntries(List<JournalEntryData> savedEntries)
        {
            _entries.Clear();

            // Очищаем UI-контейнер
            foreach (Transform child in entryContainer)
                Destroy(child.gameObject);

            foreach (JournalEntryData entry in savedEntries)
            {
                _entries.Add(entry);
                SpawnEntryUI(entry);
            }
        }

        // ──────────────────────────────────────────────
        // Приватные методы
        // ──────────────────────────────────────────────

        private void SpawnEntryUI(JournalEntryData data)
        {
            if (entryPrefab == null || entryContainer == null) return;

            GameObject go = Instantiate(entryPrefab, entryContainer);

            // Ищем TMP-компоненты внутри prefab по тегам/именам
            TextMeshProUGUI[] texts = go.GetComponentsInChildren<TextMeshProUGUI>();
            if (texts.Length >= 1) texts[0].text = data.title;
            if (texts.Length >= 2) texts[1].text = data.body;
        }
    }

    // ──────────────────────────────────────────────────
    // Данные одной записи дневника (сериализуемые)
    // ──────────────────────────────────────────────────

    [System.Serializable]
    public class JournalEntryData
    {
        public string title;
        public string body;
    }
}
