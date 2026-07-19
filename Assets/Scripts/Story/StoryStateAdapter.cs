using System.Collections.Generic;
using UnityEngine;

namespace StorySystem
{
    /// <summary>
    /// Адаптер состояния игры для StorySystem.
    /// Подписывается на PlayerToken.OnArrivedAtCity и транслирует
    /// события в GameEvents — StoryManager реагирует автоматически.
    /// </summary>
    public class StoryStateAdapter : MonoBehaviour
    {
        public static StoryStateAdapter Instance { get; private set; }

        // ── Ссылки на существующие системы (назначь в Inspector) ──────────
        [Header("Ссылки на существующие системы")]
        [SerializeField] private PlayerInventory playerInventory;
        [SerializeField] private PlayerToken playerToken;  // источник события прибытия в город

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
            // Подписываемся на PlayerToken.OnArrivedAtCity — он уже есть в проекте.
            // City.cityId используется как идентификатор для условий StoryBeat.
            if (playerToken != null)
                playerToken.OnArrivedAtCity += OnPlayerArrivedAtCity;
            else
                Debug.LogWarning("[StoryStateAdapter] PlayerToken не назначен в Inspector — " +
                                 "триггер ArriveAtCity не будет работать.");

            // Обновляем локальное состояние при срабатывании GameEvents
            GameEvents.OnPlayerArrivedAtCity += city => _lastVisitedCityId = city;
            GameEvents.OnEventCardCollected   += card => _collectedEventCardIds.Add(card);
            GameEvents.OnUnitHired            += unit => _hiredUnitTypes.Add(unit);
        }

        private void OnDestroy()
        {
            if (playerToken != null)
                playerToken.OnArrivedAtCity -= OnPlayerArrivedAtCity;
        }

        // ── Обработчик прибытия в город ───────────────────────────────────

        /// <summary>
        /// Вызывается когда PlayerToken прибывает в город (старт и финиш пути).
        /// Транслирует событие в GameEvents — StoryManager подхватит автоматически.
        /// City.cityId должен совпадать со строкой в StoryCondition.targetCityId.
        /// </summary>
        private void OnPlayerArrivedAtCity(City city)
        {
            if (city == null) return;
            GameEvents.PlayerArrivedAtCity(city.CityName);
            Debug.Log($"[StoryStateAdapter] Прибытие в город: {city.CityName}");
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
        /// <param name="itemId">Имя Item-ассета (совпадает с именем файла в Resources/Items/)</param>
        public int GetItemAmount(string itemId)
        {
            if (playerInventory == null)
            {
                Debug.LogWarning("[StoryStateAdapter] playerInventory не назначен в Inspector.");
                return 0;
            }

            Item item = Resources.Load<Item>($"Items/{itemId}");
            if (item == null)
            {
                Debug.LogWarning($"[StoryStateAdapter] Item '{itemId}' не найден в Resources/Items/.");
                return 0;
            }

            return playerInventory.GetItemStock(item);
        }

        /// <summary>Есть ли в команде юнит указанного типа?</summary>
        public bool HasUnitOfType(string unitType) => _hiredUnitTypes.Contains(unitType);
    }
}