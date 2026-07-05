# Рефакторинг системы карт — итоги

## Новые файлы

| Файл | Назначение |
|------|-----------|
| `ICard.cs` | Интерфейс для всех карт колоды + enum `CardDeckType` |
| `CardDeck.cs` | Типизированная тасуемая колода (заменяет `List<object>`) |
| `CardDataExtensions.cs` | Extension-методы `ToEventCardData()` на `ShadowCardData`, `BattleCardData`, `ICard` |

## Удалить из проекта

| Файл | Причина |
|------|---------|
| `BattleCardEffect.cs` | Полный дубликат части `HandCardData.CardEffectType` |

---

## Изменения по файлам

### `ICard.cs` (новый)
Общий контракт `CardName / Description / DeckType` — позволяет хранить все карты в одной типизированной коллекции без `object`.

### `CardDeck.cs` (новый)
- `List<ICard>` вместо `List<object>` — убирает боксинг и `is`-касты в DrawCard
- Fisher-Yates shuffle инкапсулирован внутри
- `Draw()` / `Peek()` / `IsEmpty` / `Count` — чистый API

### `CardDataExtensions.cs` (новый)
- Конвертация `ShadowCardData → EventCardData` и `BattleCardData → EventCardData` вынесена из `CardManager` в extension-методы
- Устанавливает `HideFlags.DontSave` на создаваемые экземпляры → GC собирает их без утечки в Asset DB

### `ShadowCardData.cs`
- Реализует `ICard` (явная реализация интерфейса, не загрязняет публичный API)
- Убран пустой `applyOnceInCity` — он не использовался нигде в коде

### `BattleCardData.cs`
- Реализует `ICard`
- Строковое описание (ранее дублировалось в `CardManager`) перенесено в `ICard.Description` через extension-метод

### `CardManager.cs`
- `List<object> _shuffledDeck` → `CardDeck _deck` (типизировано, нет боксинга)
- `BuildAndShuffleDeck` → `RebuildDeck` (название отражает действие)
- `DrawShadowCard` / `DrawBattleCard` → `HandleShadowCard` / `HandleBattleCard`
- Конвертеры `ShadowCardToEventCard` / `BattleCardToEventCard` удалены — заменены `card.ToEventCardData()`
- Проверка `CancelCard` перенесена сюда из разрозненных мест — единая точка входа

### `ShadowEffectManager.cs`
- `ApplyTransientCard(type, value, duration)` — публичный метод для `HandManager`, убирает необходимость создавать `ShadowCardData` через `CreateTempShadowCard`; вся логика создания временного объекта в одном месте
- `ActiveEffect` стал `sealed class` с свойствами вместо публичных полей
- `ApplyStatsDelta` переименован в `ModifyStats` — точнее отражает смысл
- `switch` по `effectType` получил ветку `default` с предупреждением

### `BattleManager.cs`
- Добавлен `ResetState(BattleCardData)` — единая точка сброса состояния боя вместо разбросанных присваиваний
- `Win()` и `Lose()` теперь вызывают `ResetState(null)` в конце — нет риска двойного вызова
- Добавлено событие `OnBattleEscaped` (ранее пропуск боя не сигнализировался отдельным событием)
- `_attackBonus` не сбрасывается вручную в разных местах — только в `ResetState`

### `HandManager.cs`
- Удалена зависимость `GameManager.Instance.GetComponent<ShadowEffectManager>()` — прямая `[SerializeField]` ссылка
- `CreateTempShadowCard` удалён — заменён вызовом `effectManager.ApplyTransientCard()`
- `RemoveAndRefresh` переименован в `ConsumeCard` — термин «consume» стандартен для карточных игр
- `UseCard` вынесен в `ExecuteCardEffect` с явным параметром `state` — читаемее

### `EventCardDeckUI.cs`
- Добавлен `ShowCancelledCard(ICard)` — визуальная обратная связь при отмене карты (CardManager вызывает при `CancelCard`)
- `_activeCard` убирается через `DismissActiveCard(bool)` — дублирующийся код отписки объединён
- Тест-блок `#if UNITY_EDITOR` не изменён

### `HandCardData.cs`
- Комментарий о дублировании с `BattleCardEffect.cs` — тот файл нужно удалить

---

## Граф зависимостей (после рефакторинга)

```
GameManager
  ├── CardManager
  │     ├── CardDeck          (нет Unity-зависимостей)
  │     ├── ShadowEffectManager
  │     ├── BattleManager
  │     └── EventCardDeckUI
  ├── HandManager
  │     ├── BattleManager
  │     └── ShadowEffectManager   ← прямая ссылка (было GetComponent)
  └── ShadowEffectManager
        ├── PlayerStats
        ├── PlayerInventory
        └── TeamSystem
```

Циклических зависимостей нет. `ICard` / `CardDeck` / `CardDataExtensions` — чистые C# без Unity-зависимостей, легко покрываются unit-тестами.
