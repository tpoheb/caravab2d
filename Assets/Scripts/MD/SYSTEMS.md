# Справочник систем — 1000 Дорог

## GameManager

**Файл:** `Assets/Scripts/GameManager.cs`  
**Паттерн:** Singleton (`GameManager.Instance`)

### Публичные методы

| Метод | Описание |
|---|---|
| `StartNewTurn()` | Начинает новый ход, устанавливает `IsPlayerTurnActive = true` |
| `HandleEndTurnRequest()` | Вызывается UI. Если есть активный путь — двигает токен и бросает кубик |
| `CompleteEventPhase()` | Вызывается `BattleManager` / `CardManager` по завершении события |
| `CompleteMovementPhase()` | Вызывается `PlayerToken` при прибытии в город |
| `EndPlayerTurn()` | Заканчивает ход игрока |

### Enum DiceEventType

```csharp
public enum DiceEventType
{
    None = 0,
    Battle,            // кубик 1–2 → BattleManager
    ShadowInfluence,   // кубик 3–4 → CardManager
    PeacefulPass       // кубик 5–6 → CompleteEventPhase()
}
```

---

## DiceSystem

**Файл:** `Assets/Scripts/DiceSystem.cs`

### События

```csharp
public static event Action<DiceEventType> OnDiceEvent;
```

### Методы

| Метод | Описание |
|---|---|
| `RollDice()` | Бросает 1d6, публикует `OnDiceEvent` |

### Таблица результатов

| Кубик | DiceEventType |
|---|---|
| 1–2 | Battle |
| 3–4 | ShadowInfluence |
| 5–6 | PeacefulPass |

---

## PlayerToken

**Файл:** `Assets/Scripts/PlayerToken.cs`

### Статические события

```csharp
public static event Action<City> OnPlayerArrivedAtCity;
public static event Action OnPlayerMoved;
```

### Публичные свойства / методы

| Член | Описание |
|---|---|
| `PathController PathController` | Доступ к PathController для GameManager |
| `SetPath(PathCellInitializer path)` | Устанавливает маршрут, вызывается из CityPanel.OnPathSelected |
| `AdvanceToken()` | Продвигает на один шаг, вызывается из GameManager |

### Зависимости (Inspector)

- `PathController` — управление позицией
- `UIHandler` — (legacy, для старых попапов)
- `TeamSystem` — для `PaySalaries()` при каждом шаге
- `DiceSystem` — (хранится, но вызов через GameManager)
- `PlayerInventory`
- `Button endTurnButton`
- `City startCity` — начальный город

---

## PathController

**Файл:** `Assets/Scripts/PathController.cs`

### Методы

| Метод | Описание |
|---|---|
| `SetPath(PathCellInitializer path)` | Инициализирует путь, показывает токен |
| `Advance()` | Увеличивает `currentCellIndex` |
| `MoveCurrent()` | Перемещает токен на текущую ячейку |
| `HasActivePath()` → bool | Есть ли активный маршрут |
| `IsPathCompleted()` → bool | Достиг ли конца пути |
| `ResetToken()` | Сбрасывает путь, скрывает токен |

---

## PathCellInitializer

**Файл:** `Assets/Scripts/PathCellInitializer.cs`

Инициализирует дочерние объекты с тегом `"Cell"`, нумерует их и добавляет компонент `Cell` если его нет.

### Свойства

```csharp
public City FinishCity { get; }   // город назначения маршрута
```

### Методы

```csharp
public void InitializeCells()   // вызывается из City.Awake() или Start()
```

---

## City

**Файл:** `Assets/Scripts/City.cs`

### Свойства

```csharp
public string CityName { get; }
public CityData CityData { get; }   // ScriptableObject с товарами
public List<PathCellInitializer> Paths { get; }
```

---

## CityManager

**Файл:** `Assets/Scripts/CityManager.cs`

Слушает `PlayerToken.OnPlayerArrivedAtCity` и `TradeUIManager.OnTradeClosedRequest`.

При прибытии открывает `CityPanel`. При закрытии торговли — повторно открывает панель последнего города.

---

## CityPanel

**Файл:** `Assets/Scripts/CityPanel.cs`

### Статические события

```csharp
public static event Action<PathCellInitializer> OnPathSelected;
```

### Методы

| Метод | Описание |
|---|---|
| `OpenPanel(City city)` | Строит кнопки маршрутов, показывает панель |
| `ClosePanel()` | `gameObject.SetActive(false)` |

Кнопка «Купить товары» вызывает `TradeSystem.RequestTrade(city)` и закрывает панель.

---

## TradeSystem

**Файл:** `Assets/Scripts/Trade/TradeSystem.cs`

### Статические события

```csharp
public static event Action<City> OnTradeRequest;
```

### Публичные методы

| Метод | Описание |
|---|---|
| `RequestTrade(City city)` (static) | Публикует `OnTradeRequest` |
| `BuyItem(CityData.CityItem item, int qty)` | Покупка через TransactionHandler + обновление UI |
| `SellItem(CityData.CityItem item, int qty)` | Продажа через TransactionHandler + обновление UI |
| `CloseTrade()` | Закрывает UI торговли |

---

## TradeTransactionHandler

**Файл:** `Assets/Scripts/Trade/TradeTransactionHandler.cs`  
**Тип:** `static class` (без MonoBehaviour)

### Методы

```csharp
static void ProcessBuyTransaction(CityData.CityItem item, int qty,
    CityData city, PlayerInventory inv, PlayerStats stats)

static void ProcessSellTransaction(CityData.CityItem item, int qty,
    CityData city, PlayerInventory inv, PlayerStats stats)
```

### Формула цены

```csharp
float bargainEffect = playerStats.Bargain * 0.01f;
// Покупка: цена снижается при положительном Bargain
int buyPrice = Mathf.RoundToInt(basePrice * (1f - bargainEffect));
// Продажа: цена растёт при положительном Bargain  
int sellPrice = Mathf.RoundToInt(basePrice * (1f + bargainEffect));
finalPrice = Mathf.Max(1, calculatedPrice); // минимум 1
```

### Условия покупки

- `playerInventory.CanCarryMore(weight * qty)` — есть место
- `playerInventory.Money >= totalCost` — есть деньги
- `cityItem.stock >= qty` — есть товар в городе

### Условия продажи

- `playerInventory.GetItemStock(item) >= qty` — есть товар у игрока
- `city.cityGold >= totalValue` — у города есть золото

---

## TradeUIManager

**Файл:** `Assets/Scripts/Trade/TradeUIManager.cs`

### Статические события

```csharp
public static event System.Action OnTradeClosedRequest;
```

### Методы

| Метод | Описание |
|---|---|
| `OpenTradePanel(CityData, PlayerInventory)` | Открывает панель, строит ItemUI |
| `CloseTradePanel()` | Скрывает панель, удаляет ItemUI |
| `RefreshTradeUI(CityData, PlayerInventory)` | Полное перестроение UI |
| `UpdateMoneyUI(PlayerInventory, CityData)` | Обновляет деньги игрока и города |
| `RefreshItemStocks(PlayerInventory)` | Обновляет только количества у игрока |
| `ClearItemUIs()` | Уничтожает все `ItemUI` |

---

## PlayerInventory

**Файл:** `Assets/Scripts/Trade/PlayerInventory.cs`

### События

```csharp
public event System.Action OnInventoryChanged;
public event System.Action OnMoneyChanged;
```

### Методы инвентаря

| Метод | Описание |
|---|---|
| `AddItem(Item, int)` → bool | Добавляет товар, проверяет вместимость |
| `RemoveItem(Item, int)` → bool | Удаляет товар |
| `GetItemStock(Item)` → int | Количество конкретного товара |
| `HasItem(Item, int)` → bool | Есть ли минимальное количество |

### Методы денег

| Метод | Описание |
|---|---|
| `TrySpendMoney(int)` → bool | Списывает деньги, если достаточно |
| `AddMoney(int)` | Добавляет деньги |

### Методы вместимости

| Метод | Описание |
|---|---|
| `CanCarryItem(Item, int)` → bool | Проверка по весу |
| `GetCurrentWeight()` → int | Текущий суммарный вес |
| `GetRemainingCapacity()` → int | Остаток вместимости |

---

## PlayerStats

**Файл:** `Assets/Scripts/Hire/PlayerStats.cs`  
**Тип:** ScriptableObject

### Свойства

| Свойство | Диапазон | Описание |
|---|---|---|
| `Attack` | 1–100 | Боевая сила (сравнивается с `BattleCardData.requiredAttack`) |
| `Bargain` | −20..+20 | Влияет на цены торговли (±1% за единицу) |
| `Capacity` | 10–5000 | Максимальный вес инвентаря |

### Методы

```csharp
void ModifyAttack(int value)
void ModifyBargain(int value)
void ModifyCapacity(int value)
```

---

## TeamSystem

**Файл:** `Assets/Scripts/Hire/TeamSystem.cs`

### Публичные свойства

```csharp
public List<TeamMember> CurrentTeam { get; }
```

### Методы

| Метод | Описание |
|---|---|
| `OpenHirePanel()` | Инициализирует UI при первом вызове, обновляет и показывает |
| `TryHireUnit(UnitData)` → bool | Найм: списывает деньги, добавляет в команду, применяет бонусы |
| `FireUnit(TeamMember)` | Увольнение: убирает бонусы, удаляет из команды |
| `PaySalaries()` | Списывает суммарную зарплату, вызывается каждый шаг |
| `CloseHirePanel()` | Закрывает UI найма |

---

## BattleManager

**Файл:** `Assets/Scripts/BattleManager.cs`

### Методы

| Метод | Описание |
|---|---|
| `StartRandomBattle()` | Вытягивает случайную `BattleCardData`, обрабатывает результат |
| `EndBattle()` | Вызывает `GameManager.CompleteEventPhase()` |

### Логика боя

```
playerStats.Attack >= battleCard.requiredAttack
    → Победа: playerInventory.Money += rewardMoney
    → Поражение: playerInventory.Money += penaltyMoney (отрицательное)
```

---

## CardManager

**Файл:** `Assets/Scripts/CardManager.cs`  
**Паттерн:** Singleton

Получает вызов `DrawCard()` от `GameManager` при событии `ShadowInfluence`. Симулирует вытягивание карты с задержкой 0.5 сек (`Invoke`), затем вызывает `GameManager.CompleteEventPhase()`.

---

## IsoCameraFollow

**Файл:** `Assets/Scripts/IsoCameraFollow.cs`

```csharp
public Transform target;     // цель следования (PlayerToken)
public float smoothSpeed;    // скорость интерполяции
public Vector3 offset;       // смещение от цели
```

Обновляется в `LateUpdate()` через `Vector3.Lerp`.

---

## TopBarUI

**Файл:** `Assets/Scripts/TopBarUI.cs`

Отображает в верхней панели: деньги, атаку, вместимость, торг.

Использует паттерн «грязный флаг» в `Update()`: сравнивает кэшированные значения с текущими и перерисовывает только изменившиеся поля.

---

## PrefabPlacer (Editor-only)

**Файл:** `Assets/Scripts/PrefabPlacer.cs`

Editor-инструмент для расстановки префабов вдоль линии между двумя точками. Вызывается через контекстное меню компонента в Inspector.

```
[ContextMenu("Place Prefabs")]
void PlacePrefabs()
```
