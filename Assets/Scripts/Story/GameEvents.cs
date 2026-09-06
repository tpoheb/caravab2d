using System;

namespace StorySystem
{
    /// <summary>
    /// Статическая шина событий. Другие системы вызывают события здесь,
    /// StoryManager подписывается и реагирует — без прямых зависимостей.
    ///
    /// Вызов из игровых систем:
    ///   GameEvents.OnPlayerArrivedAtCity?.Invoke(city.cityId);
    ///   GameEvents.OnEventCardCollected?.Invoke(card.cardId);
    ///   GameEvents.OnItemAddedToInventory?.Invoke(item.itemId);
    ///   GameEvents.OnUnitHired?.Invoke(unit.unitType);
    /// </summary>
    public static class GameEvents
    {
        // Игрок прибыл в город (передаём cityId)
        public static event Action<string> OnPlayerArrivedAtCity;

        // Игрок получил карту события (передаём cardId)
        public static event Action<string> OnEventCardCollected;

        // В инвентарь добавлен товар (передаём itemId)
        public static event Action<string> OnItemAddedToInventory;

        // Нанят член команды (передаём unitType)
        public static event Action<string> OnUnitHired;

        // ── Журнал: торговля ──────────────────────────────

        /// <summary>Игрок купил товар. (itemName, quantity, totalCost, cityName)</summary>
        public static event Action<string, int, int, string> OnItemBought;

        /// <summary>Игрок продал товар. (itemName, quantity, totalValue, cityName)</summary>
        public static event Action<string, int, int, string> OnItemSold;

        // ── Журнал: квесты ────────────────────────────────

        /// <summary>Нарративная запись для журнала квестов. (title, body)</summary>
        public static event Action<string, string> OnQuestJournalEntry;

        // ──────────────────────────────────────────────
        // Вспомогательные методы для безопасного вызова
        // ──────────────────────────────────────────────

        public static void PlayerArrivedAtCity(string cityId)
            => OnPlayerArrivedAtCity?.Invoke(cityId);

        public static void EventCardCollected(string cardId)
            => OnEventCardCollected?.Invoke(cardId);

        public static void ItemAddedToInventory(string itemId)
            => OnItemAddedToInventory?.Invoke(itemId);

        public static void UnitHired(string unitType)
            => OnUnitHired?.Invoke(unitType);

        public static void ItemBought(string itemName, int quantity, int totalCost, string cityName)
            => OnItemBought?.Invoke(itemName, quantity, totalCost, cityName);

        public static void ItemSold(string itemName, int quantity, int totalValue, string cityName)
            => OnItemSold?.Invoke(itemName, quantity, totalValue, cityName);

        public static void QuestJournalEntry(string title, string body)
            => OnQuestJournalEntry?.Invoke(title, body);
    }
}
