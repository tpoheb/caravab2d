using System.Collections.Generic;
using UnityEngine;

namespace StorySystem
{
    /// <summary>
    /// Центральный оркестратор нарративной системы.
    /// Подключи этот компонент к GameObject "StoryManager" на сцене.
    /// Назначь все StoryBeat-ассеты в список allBeats через Inspector.
    /// </summary>
    public class StoryManager : MonoBehaviour
    {
        public static StoryManager Instance { get; private set; }

        [Header("Все нарративные вставки проекта")]
        [Tooltip("Перетащи сюда все StoryBeat ScriptableObject'ы из папки Assets")]
        [SerializeField] private List<StoryBeat> allBeats = new List<StoryBeat>();

        [Header("Зависимости")]
        [SerializeField] private StoryWindowUI storyWindowUI;
        [SerializeField] private QuestJournal questJournal;

        // Состояние: какие вставки уже были показаны (beatId)
        private readonly HashSet<string> _shownBeats = new HashSet<string>();

        // Очередь вставок, готовых к показу (показываем по одной)
        private readonly Queue<StoryBeat> _pendingBeats = new Queue<StoryBeat>();

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
            DontDestroyOnLoad(gameObject);
        }

        private void Start()
        {
            // Подписываемся на игровые события
            GameEvents.OnPlayerArrivedAtCity += HandleArriveAtCity;
            GameEvents.OnEventCardCollected   += HandleEventCardCollected;
            GameEvents.OnItemAddedToInventory += HandleItemAdded;
            GameEvents.OnUnitHired            += HandleUnitHired;
        }

        private void OnDestroy()
        {
            GameEvents.OnPlayerArrivedAtCity -= HandleArriveAtCity;
            GameEvents.OnEventCardCollected   -= HandleEventCardCollected;
            GameEvents.OnItemAddedToInventory -= HandleItemAdded;
            GameEvents.OnUnitHired            -= HandleUnitHired;
        }

        // ──────────────────────────────────────────────
        // Обработчики игровых событий
        // ──────────────────────────────────────────────

        private void HandleArriveAtCity(string cityId)     => CheckAllBeats();
        private void HandleEventCardCollected(string cardId) => CheckAllBeats();
        private void HandleItemAdded(string itemId)        => CheckAllBeats();
        private void HandleUnitHired(string unitType)      => CheckAllBeats();

        // ──────────────────────────────────────────────
        // Проверка условий
        // ──────────────────────────────────────────────

        private void CheckAllBeats()
        {
            bool anyQueued = false;

            foreach (StoryBeat beat in allBeats)
            {
                if (_shownBeats.Contains(beat.beatId)) continue;
                if (IsAlreadyPending(beat)) continue;
                if (!AllConditionsMet(beat)) continue;

                _pendingBeats.Enqueue(beat);
                anyQueued = true;
            }

            // Показываем первую из очереди, если окно сейчас свободно
            if (anyQueued || _pendingBeats.Count > 0)
                TryShowNextBeat();
        }

        private bool IsAlreadyPending(StoryBeat beat)
        {
            foreach (StoryBeat b in _pendingBeats)
                if (b.beatId == beat.beatId) return true;
            return false;
        }

        /// <summary>
        /// Проверяет ВСЕ условия вставки (логика AND).
        /// </summary>
        private bool AllConditionsMet(StoryBeat beat)
        {
            foreach (StoryCondition condition in beat.conditions)
            {
                if (!IsConditionMet(condition)) return false;
            }
            return true;
        }

        private bool IsConditionMet(StoryCondition condition)
        {
            switch (condition.conditionType)
            {
                case StoryConditionType.ArriveAtCity:
                    return GameState.Instance.LastVisitedCityId == condition.targetCityId;

                case StoryConditionType.CollectEventCards:
                    bool countOk = condition.requiredCardCount <= 0
                        || GameState.Instance.CollectedEventCardCount >= condition.requiredCardCount;
                    bool cardOk = string.IsNullOrEmpty(condition.specificCardId)
                        || GameState.Instance.HasEventCard(condition.specificCardId);
                    return countOk && cardOk;

                case StoryConditionType.HaveItemInInventory:
                    return GameState.Instance.GetItemAmount(condition.requiredItemId) >= condition.requiredItemAmount;

                case StoryConditionType.HireUnitByType:
                    return GameState.Instance.HasUnitOfType(condition.requiredUnitType);

                default:
                    Debug.LogWarning($"[StoryManager] Неизвестный тип условия: {condition.conditionType}");
                    return false;
            }
        }

        // ──────────────────────────────────────────────
        // Показ окна
        // ──────────────────────────────────────────────

        private void TryShowNextBeat()
        {
            if (storyWindowUI.IsVisible) return;          // окно уже открыто — ждём
            if (_pendingBeats.Count == 0) return;

            StoryBeat beat = _pendingBeats.Dequeue();
            _shownBeats.Add(beat.beatId);

            storyWindowUI.Show(beat, OnWindowClosed);
            questJournal.AddEntry(beat);
        }

        private void OnWindowClosed()
        {
            // После закрытия текущего окна — показываем следующее из очереди
            TryShowNextBeat();
        }

        // ──────────────────────────────────────────────
        // Сохранение / загрузка состояния
        // ──────────────────────────────────────────────

        /// <summary>
        /// Возвращает список ID уже показанных вставок для сохранения в SaveData.
        /// </summary>
        public List<string> GetShownBeatIds() => new List<string>(_shownBeats);

        /// <summary>
        /// Восстанавливает состояние из SaveData при загрузке игры.
        /// </summary>
        public void LoadShownBeatIds(List<string> ids)
        {
            _shownBeats.Clear();
            foreach (string id in ids)
                _shownBeats.Add(id);
        }
    }
}
