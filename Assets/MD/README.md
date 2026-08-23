# 🐪 1000 Дорог (1000 Roads) — Caravan Trading Game

> **Unity 6 · URP 17 · C# · Board/Card Game**

Пошаговая торговая игра с элементами настольной и карточной механики. Игрок управляет торговым
 Казан маршрутами между городами арабского фэнтезийного мира, торгует товарами, нанимает отряд спутников и сражается с врагами на случайных событиях.

---

## 🗺️ Содержание

- [О проекте](#о-проекте)
- [Быстрый старт](#быстрый-старт)
- [Архитектура](#архитектура)
- [Игровые системы](#игровые-системы)
- [Данные и контент](#данные-и-контент)
- [Структура проекта](#структура-проекта)

---

## О проекте

**1000 дорог** — инди-игра в жанре торгового симулятора с пошаговым перемещением по гексагональной карте. Игрок выбирает маршруты между городами, торгует экзотическими товарами (звёздный песок, жемчуг желаний, слёзы русалок…), нанимает наёмников и встречает случайные события — битвы с разбойниками или карточные события «Тени».

**Сеттинг:** арабское фэнтези, фиктивный мир с городами вроде *Мадинат аль-Ахлам*, *Айн аль-Аташ*, *Нур аль-Наджм*.

**Статус:** активная разработка, 2D→3D переход завершён, гексагональная карта работает.

---

## Быстрый старт

### Требования

| Зависимость | Версия |
|---|---|
| Unity Editor | **6000.2.13f1** (Unity 6) |
| Render Pipeline | URP 17.2 |
| Input System | 1.14.2 |
| TextMesh Pro | встроен в URP |

### Открытие проекта

```bash
git clone https://github.com/tpoheb/caravab2d.git
# Открыть папку в Unity Hub → Add project from disk
```

> ⚠️ При первом открытии Unity может пересоздать импортированные ресурсы. Дождитесь завершения импорта.

### Запуск

1. Открыть сцену `Assets/Scenes/`
2. Нажать ▶ Play
3. Игрок стартует в начальном городе — откроется `CityPanel`
4. Выбрать маршрут → нажать **End Turn** для продвижения

---

## Архитектура

Проект построен на **событийной архитектуре** через C# `static event` — компоненты не знают друг о друге напрямую, общаются через события.

```
GameManager (Singleton)
    │
    ├── DiceSystem ──────────────────→ OnDiceEvent (Battle / ShadowInfluence / PeacefulPass)
    │
    ├── PlayerToken ─────────────────→ OnPlayerArrivedAtCity, OnPlayerMoved
    │       └── PathController
    │
    ├── BattleManager ←──────────────── GameManager.StartRandomBattle()
    │
    ├── CardManager ←────────────────── GameManager.DrawCard()
    │
    └── CityManager ←────────────────── PlayerToken.OnPlayerArrivedAtCity
            └── CityPanel ───────────→ OnPathSelected, TradeSystem.RequestTrade()
                    └── TradeSystem ──→ OnTradeRequest → TradeUIManager
```

### Паттерны

| Паттерн | Где используется |
|---|---|
| Singleton | `GameManager`, `CardManager` |
| Observer (static event) | `DiceSystem.OnDiceEvent`, `PlayerToken.OnPlayerArrivedAtCity`, `TradeSystem.OnTradeRequest`, `TradeUIManager.OnTradeClosedRequest`, `CityPanel.OnPathSelected` |
| ScriptableObject как данные | `UnitData`, `PlayerStats`, `BattleCardData`, `Item`, `CityData` |
| Separation of concerns | `TradeSystem` (логика) / `TradeUIManager` (UI) / `TradeTransactionHandler` (транзакции) |

---

## Игровые системы

### 🎮 GameManager

Центральный оркестратор. Управляет фазами хода:
- `StartNewTurn()` — начало хода игрока
- `HandleEndTurnRequest()` — обрабатывает нажатие «End Turn»
- `CompleteEventPhase()` — завершение события (вызывается `BattleManager` / `CardManager`)
- `CompleteMovementPhase()` — вызывается `PlayerToken` по прибытии

**Фазы события** (`DiceEventType`):

| Результат кубика | Событие |
|---|---|
| 1–2 | `Battle` → BattleManager |
| 3–4 | `ShadowInfluence` → CardManager |
| 5–6 | `PeacefulPass` → фаза сразу завершается |

---

### 🎲 DiceSystem

Бросает 1d6 и публикует `OnDiceEvent`. Вызывается `GameManager` после хода токена.

---

### 🪙 Торговая система (Trade)

Трёхуровневая архитектура:

```
TradeSystem          — бизнес-логика, хранит ссылку на текущий город
TradeUIManager       — отображает панель, создаёт ItemUI
TradeTransactionHandler — static класс, валидирует и выполняет транзакции
```

**Формула цены с торгом:**
```
finalPrice = basePrice × (1 ± bargain × 0.01)
```
(Bargain уменьшает цену покупки, увеличивает цену продажи)

**Ограничения:**
- Игрок не может купить, если нет денег / нет места в инвентаре / нет стока в городе
- Город не может купить, если нет `cityGold`

---

### 🛡️ BattleManager

При событии `Battle` вытягивает случайную `BattleCardData` и сравнивает `PlayerStats.Attack` с `requiredAttack`:

- **Победа:** `+rewardMoney` (типично +60)
- **Поражение:** `penaltyMoney` (типично −50)

После результата вызывает `GameManager.CompleteEventPhase()`.

---

### 🃏 Card System

| Класс | Роль |
|---|---|
| `CardManager` | Получает событие `ShadowInfluence`, симулирует вытягивание карты |
| `CardDeckManager` | Перемешивание колоды, рука (hand), сброс |
| `BattleCardData` (SO) | Данные боевой карты: enemy, requiredAttack, reward, penalty |
| `CardHandUI` | Отображение руки |
| `CardUI` | Одна карта в UI |

---

### 👥 Система найма (Hire / Team)

```
TeamSystem
    ├── UnitData (ScriptableObject) — имя, иконка, бонусы, стоимость найма, зарплата
    ├── TeamMember — экземпляр нанятого юнита
    ├── HirePanelUI — отображение панели найма
    └── PlayerStats (ScriptableObject) — Attack, Bargain, Capacity
```

- `TryHireUnit()` — списывает деньги, добавляет в команду, применяет бонусы к `PlayerStats`
- `FireUnit()` — убирает бонусы, удаляет из команды
- `PaySalaries()` — вызывается при каждом шаге токена

---

### 🏙️ Города и маршруты

```
City (MonoBehaviour)
    ├── cityName
    ├── CityData (ScriptableObject) — товары, cityGold
    └── List<PathCellInitializer> inCityPaths — маршруты из города

PathCellInitializer
    ├── finishCity — город назначения
    └── Child objects с тегом "Cell" — шаги маршрута
```

`CityManager` слушает `PlayerToken.OnPlayerArrivedAtCity` и открывает `CityPanel`.

---

### 📦 Инвентарь игрока (PlayerInventory)

- Хранит список `InventoryItem { Item item; int quantity; }`
- Проверяет вместимость (`PlayerStats.Capacity` и `Item.weight`)
- Сохранение через `PlayerPrefs` (`SaveInventory` / `LoadInventory`)
- События: `OnInventoryChanged`, `OnMoneyChanged`

---

### 📷 Камера

`IsoCameraFollow` — изометрическая камера, плавно следит за `target` через `Vector3.Lerp`.

---

## Данные и контент

### Города (6 штук)

| ID | Название |
|---|---|
| 1 | Мадинат аль-Ахлам |
| 2 | Вади аль-Сараб |
| 3 | Нур аль-Наджм |
| 4 | Кахф аль-Замман |
| 5 | Айн аль-Аташ |
| 6 | Бустан аль-Сахра |

### Товары (18 предметов)

Вода памяти, Жемчуг желаний, Звёздный песок, Кораллы шепотов, Кристаллы жажды, Лунное серебро, Металл снов, Огненные рубины, Пыль миражей, Розы пустыни, Самоцветы мороза, Слёзы русалок, Соль бурь, Ткани из тумана, Чернила забвения, Эликсир грёз, Эхо-камни, Янтарь времени.

### Боевые карты (8 врагов)

| ID | Враг |
|---|---|
| 101 | Песчаные разбойники |
| 102 | Горные бандиты |
| 103 | Морские разбойники |
| 104 | Кочевые налётчики |
| 105 | Пустынные демоны |
| 106 | Каравани грабители |
| 107 | Ночные охотники |
| 108 | Главарь разбойников |

### Импорт данных (Editor Tools)

| Инструмент | Меню | Формат |
|---|---|---|
| `BattleCardImporter` | Tools → Battle Cards → Import from CSV | CSV: `id,name,attack,reward,penalty` |
| `TradeDataImporter` | Trade → Import Trade Data | TSV: `city\titem\tweight\tstock\tbuy\tsell` |

---

## Структура проекта

```
Assets/
├── Data/
│   ├── BattleCards/       # BattleCardData ScriptableObjects (Card_101_*.asset)
│   ├── Cities/            # CityData ScriptableObjects
│   └── Items/             # Item ScriptableObjects
├── Editor/
│   ├── BattleCardImporter.cs   # CSV → BattleCardData
│   └── TradeDataIO.cs          # TSV → Item + CityData
├── Prefabs/
│   ├── City.prefab
│   ├── CardPrefab.prefab
│   ├── HirePanel.prefab
│   ├── ItemUIPrefab.prefab
│   └── PathButtonPref.prefab
├── Scripts/
│   ├── GameManager.cs
│   ├── BattleManager.cs
│   ├── CardManager.cs
│   ├── DiceSystem.cs
│   ├── City.cs
│   ├── CityManager.cs
│   ├── CityPanel.cs
│   ├── PlayerToken.cs
│   ├── PathController.cs
│   ├── PathCellInitializer.cs
│   ├── TopBarUI.cs
│   ├── IsoCameraFollow.cs
│   ├── PrefabPlacer.cs
│   ├── Card/
│   │   ├── BattleCardData.cs
│   │   ├── CardDeckManager.cs
│   │   ├── CardHandUI.cs
│   │   └── CardUI.cs
│   ├── Hire/
│   │   ├── HirePanelUI.cs
│   │   ├── PlayerStats.cs
│   │   ├── TeamMember.cs
│   │   ├── TeamSystem.cs
│   │   └── UnitData.cs
│   └── Trade/
│       ├── CityData.cs
│       ├── Item.cs
│       ├── ItemUI.cs
│       ├── PlayerInventory.cs
│       ├── TradeData.cs
│       ├── TradeSystem.cs
│       ├── TradeTransactionHandler.cs
│       └── TradeUIManager.cs
├── Scenes/
├── Prefabs/
└── TextMesh Pro/
```

---

## Лицензия

Проект в разработке. Все права защищены.
