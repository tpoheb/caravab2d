using System.Collections.Generic;
using UnityEngine;

namespace StorySystem
{
    /// <summary>
    /// Фасад состояния игры для StorySystem.
    /// Этот класс — адаптер: он обращается к существующим системам
    /// (PlayerInventory, TurnQueue, PathController и т.д.) и возвращает
    /// данные в формате, понятном StoryManager.
    ///
    /// ИНСТРУКЦИЯ: замени заглушки реальными вызовами своих классов.
    /// </summary>
    public class GameState : MonoBehaviour
    {
        public static GameState Instance { get; private set; }

        // ── Ссылки на существующие системы (назначь в Inspector) ──────────
        [Header("Ссылки на существующие системы")]
        [SerializeField] private PlayerInventory playerInventory;   // уже существует
        // [SerializeField] private PathController pathController;   // раскомментируй если нужно
        // [SerializeField] private TurnQueue turnQueue;             // раскомментируй если нужно

        // ── Внутреннее состояние для отслеживания ─────────────────────────
        private string _lastVisitedCityId = "";
        private readonly HashSet<string> _collectedEventCardIds = new HashSet<string>();
        private readonly HashSet<string> _hiredUnitTypes = new HashSet<string>();

        // ──────────────────────────────────────────────
        // Lifecycle
        // ──────────────────────────────────────────────

        private void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }

        private void Start()
        {
            // Подписываемся на те же события, чтобы обновлять локальное состояние
            GameEvents.OnPlayerArrivedAtCity += city => _lastVisitedCityId = city;
            GameEvents.OnEventCardCollected   += card => _collectedEventCardIds.Add(card);
            GameEvents.OnUnitHired            += unit => _hiredUnitTypes.Add(unit);
        }

        // ──────────────────────────────────────────────
        // API для StoryManager
        // ──────────────────────────────────────────────

        /// <summary>ID последнего города, в который прибыл игрок.</summary>
        public string LastVisitedCityId => _lastVisitedCityId;

        /// <summary>Количество собранных карт событий.</summary>
        public int CollectedEventCardCount => _collectedEventCardIds.Count;

        /// <summary>Есть ли конкретная карта события в коллекции?</summary>
        public bool HasEventCard(string cardId) => _collectedEventCardIds.Contains(cardId);

        /// <summary>Количество товара в инвентаре игрока.</summary>
        public int GetItemAmount(string itemId)
        {
            if (playerInventory == null)
            {
                Debug.LogWarning("[GameState] playerInventory не назначен в Inspector.");
                return 0;
            }
            // TODO: замени на реальный вызов PlayerInventory
            // Пример: return playerInventory.GetAmount(itemId);
            return playerInventory.GetAmount(itemId);
        }

        /// <summary>Есть ли в команде юнит указанного типа?</summary>
        public bool HasUnitOfType(string unitType) => _hiredUnitTypes.Contains(unitType);
    }
}
