using System;

namespace StorySystem
{
    /// <summary>
    /// Статический шина событий. Другие системы вызывают события здесь,
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
    }
}
