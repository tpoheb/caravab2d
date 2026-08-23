# Архитектура систем — 1000 Дорог

## Общие принципы

Проект использует **событийную (Observer) архитектуру** через C# `static event`. Это позволяет системам быть независимыми: `CityPanel` не знает о `PlayerToken`, `TradeSystem` не знает о `CityPanel` — они общаются только через события.

Второй ключевой принцип — **ScriptableObject как данные**. Вся игровая конфигурация (характеристики юнитов, предметы, данные городов, боевые карты) хранится в `.asset` файлах и не зависит от сцены.

---

## Граф событий

```
┌──────────────────────────────────────────────────────────┐
│                      GAME MANAGER                        │
│  - Singleton                                             │
│  - Оркестрирует фазы хода                               │
└────────────────────────┬─────────────────────────────────┘
                         │ подписывается на
                         ▼
┌──────────────────────────────────────────────────────────┐
│                    DICE SYSTEM                           │
│  static event OnDiceEvent(DiceEventType)                 │
│  Battle | ShadowInfluence | PeacefulPass                 │
└──────────┬───────────────────────────────────────────────┘
           │ OnDiceEvent
     ┌─────┴──────────────────┐
     ▼                        ▼
┌──────────────┐    ┌──────────────────┐
│ BattleManager│    │  CardManager     │
│ StartRandom  │    │  DrawCard()      │
│ Battle()     │    │                  │
└──────┬───────┘    └────────┬─────────┘
       │ CompleteEventPhase()│
       └──────────┬──────────┘
                  ▼
         GameManager.CompleteEventPhase()

┌──────────────────────────────────────────────────────────┐
│                   PLAYER TOKEN                           │
│  static event OnPlayerArrivedAtCity(City)               │
│  static event OnPlayerMoved()                           │
└────────┬───────────────────────────────────────────────  ┘
         │ OnPlayerArrivedAtCity
         ▼
┌──────────────────────────────────────────────────────────┐
│                   CITY MANAGER                           │
│  → открывает CityPanel.OpenPanel(city)                   │
└────────────────────────┬─────────────────────────────────┘
                         │
                         ▼
┌──────────────────────────────────────────────────────────┐
│                    CITY PANEL                            │
│  static event OnPathSelected(PathCellInitializer)       │
│  → TradeSystem.RequestTrade(city) — при нажатии "Купить"│
└───────────┬──────────────────────────────────────────────┘
            │ OnPathSelected
            ▼
┌──────────────────────────────────────────────────────────┐
│                  PLAYER TOKEN                            │
│  SetPath(path) → pathController.SetPath(path)           │
└──────────────────────────────────────────────────────────┘

┌──────────────────────────────────────────────────────────┐
│                   TRADE SYSTEM                           │
│  static event OnTradeRequest(City)                       │
└────────────────────────┬─────────────────────────────────┘
                         │ OnTradeRequest (self-subscription)
                         ▼
                  OpenTrade(city)
                         │
                         ▼
┌──────────────────────────────────────────────────────────┐
│                TRADE UI MANAGER                          │
│  OpenTradePanel(cityData, inventory)                     │
│  static event OnTradeClosedRequest                       │
└────────────────────────┬─────────────────────────────────┘
                         │ OnTradeClosedRequest
                         ▼
               CityManager.ReOpenCityPanel()
```

---

## Фазы хода

```
StartNewTurn()
     │
     ▼
[Игрок выбирает маршрут в CityPanel]
     │ CityPanel.OnPathSelected
     ▼
PlayerToken.SetPath()
     │
     ▼
[Игрок нажимает End Turn]
     │ endTurnButton.onClick → GameManager.HandleEndTurnRequest()
     ▼
PlayerToken.AdvanceToken()
     ├─ pathController.Advance()
     ├─ teamSystem.PaySalaries()
     └─ [путь завершён?]
           ├─ НЕТ → pathController.MoveCurrent() → DiceSystem.RollDice()
           └─ ДА  → ArriveAtDestination()
                       ├─ pathController.ResetToken()
                       ├─ OnPlayerArrivedAtCity → CityPanel открывается
                       └─ GameManager.CompleteMovementPhase()

[DiceSystem бросает кубик]
     │ OnDiceEvent(type)
     ▼
GameManager.HandleDiceEvent()
     ├─ Battle          → BattleManager.StartRandomBattle()
     ├─ ShadowInfluence → CardManager.DrawCard()
     └─ PeacefulPass   → GameManager.CompleteEventPhase()
```

---

## Слои архитектуры

### Уровень данных (Data Layer)

ScriptableObjects — чистые данные без логики (кроме валидации):

```
PlayerStats.asset      — Attack, Bargain, Capacity
UnitData.asset         — имя, иконка, бонусы, найм, зарплата
Item.asset             — название, иконка, вес
CityData.asset         — название, cityGold, List<CityItem>
BattleCardData.asset   — enemyName, requiredAttack, reward, penalty
```

### Уровень логики (Logic Layer)

MonoBehaviour и static классы с бизнес-правилами:

```
GameManager            — оркестратор
TradeSystem            — открытие/закрытие торговли
TradeTransactionHandler— валидация и выполнение транзакций (static)
BattleManager          — разрешение боёв
TeamSystem             — управление командой
PlayerInventory        — инвентарь, деньги, вместимость
PathController         — продвижение по маршруту
```

### Уровень UI (Presentation Layer)

Только отображение и проброс событий наверх:

```
CityPanel              — панель города с маршрутами
TradeUIManager         — панель торговли
HirePanelUI            — панель найма
ItemUI                 — строка товара
CardHandUI / CardUI    — карты в руке
TopBarUI               — деньги, атака, ёмкость, торг
```

---

## ScriptableObject: PlayerStats

`PlayerStats` — ScriptableObject, но используется как **runtime-изменяемые данные** через `ModifyAttack()` и т.д. Это создаёт риск сохранения изменённых значений между Play Mode сессиями в редакторе.

**Текущий подход:** `PlayerInventory.Awake()` и связанные системы обращаются к `PlayerStats` напрямую через `SerializeField`.

**Рекомендация для продакшна:** при инициализации вызывать `Instantiate(playerStats)` для создания runtime-копии, не затрагивающей `.asset` файл.

---

## Dependency Injection через Inspector

Все зависимости инжектируются через `[SerializeField]` в Unity Inspector:

```csharp
// TradeSystem
[SerializeField] private PlayerInventory playerInventory;
[SerializeField] private PlayerStats playerStats;
[SerializeField] private TradeUIManager tradeUIManager;
```

Каждый компонент вызывает `ValidateReferences()` в `Awake()` / `Start()` и логирует ошибки при незаполненных ссылках.
