# Скрипты и импортёры — детальный разбор

Этот документ фокусируется на **механике работы каждого скрипта**: входные/выходные данные, побочные эффекты, порядок выполнения и известные особенности/риски. Отдельный акцент — на двух Editor-импортёрах, которые превращают CSV/TSV в игровые данные.

---

## 1. Импортёры (Editor Tools)

В проекте два независимых импортёра, оба — `EditorWindow`/static класс в `Assets/Editor/`, оба не входят в билд игры (работают только в Unity Editor).

### 1.1 BattleCardImporter

**Файл:** `Assets/Editor/BattleCardImporter.cs`
**Тип:** `EditorWindow`
**Меню:** `Tools → Battle Cards → Import from CSV`

#### Назначение
Импортирует данные боевых карт (врагов) из CSV-файла в `ScriptableObject`-ассеты `BattleCardData`.

#### Поля окна

```csharp
private string csvFilePath = "Assets/Data/BattleCards.csv";  // путь к CSV (редактируется в окне)
private string soSavePath  = "Assets/Data/BattleCards/";     // куда сохранять .asset
```

#### Ожидаемый формат CSV

Разделитель — **запятая** (`,`). Первая строка — заголовок, **пропускается** (`for i = 1; i < lines.Length`).

```
id,name,requiredAttack,rewardMoney,penaltyMoney
101,Песчаные разбойники,3,60,-50
102,Горные бандиты,4,70,-60
```

| Колонка (индекс) | Поле в BattleCardData | Тип |
|---|---|---|
| 0 | `cardID` | int |
| 1 | `enemyName` | string |
| 2 | `requiredAttack` | int |
| 3 | `rewardMoney` | int |
| 4 | `penaltyMoney` | int |

#### Алгоритм `ImportData()`

```
1. Проверить File.Exists(csvFilePath), иначе Debug.LogError + выход
2. Прочитать все строки File.ReadAllLines()
3. Для каждой строки (начиная со 2-й):
   a. line.Split(',')
   b. если values.Length < 5 → Debug.LogWarning, пропустить строку
   c. id = int.Parse(values[0])
   d. enemyName = values[1]
   e. card = FindOrCreateAsset(id, enemyName)
   f. card.requiredAttack = int.Parse(values[2])
      card.rewardMoney    = int.Parse(values[3])
      card.penaltyMoney   = int.Parse(values[4])
   g. EditorUtility.SetDirty(card)
4. AssetDatabase.SaveAssets()
5. AssetDatabase.Refresh()
```

#### Логика `FindOrCreateAsset(id, name)`

```csharp
string assetName = $"Card_{id}_{name}.asset";   // например Card_101_Песчаные разбойники.asset
string fullPath  = Path.Combine(soSavePath, assetName);

existingCard = AssetDatabase.LoadAssetAtPath<BattleCardData>(fullPath);
if (existingCard != null) return existingCard;       // обновление существующего

// иначе создание нового
newCard = ScriptableObject.CreateInstance<BattleCardData>();
newCard.cardID = id;
newCard.enemyName = name;
if (!Directory.Exists(soSavePath)) Directory.CreateDirectory(soSavePath);
AssetDatabase.CreateAsset(newCard, fullPath);
return newCard;
```

**Ключевая особенность:** идентификация ассета на диске идёт **по имени файла**, собранному из `id` и `name` — то есть если в новом запуске импорта изменится `name` при том же `id`, импортёр **не найдёт** старый ассет и создаст новый файл-дубликат (со старым именем оставшийся «осиротевшим»).

#### Известные риски / ограничения

| Риск | Описание |
|---|---|
| Нет обработки запятых в названии | `Split(',')` сломается, если `enemyName` содержит запятую |
| Нет try/catch при `int.Parse` | Невалидное число в CSV вызовет необработанное исключение и остановит импорт |
| Нет удаления устаревших карт | Если строку убрать из CSV, старый `.asset` останется на диске |
| Путь к CSV — относительный | `"Assets/Data/BattleCards.csv"` должен существовать в файловой системе проекта (не выбирается через диалог) |

---

### 1.2 TradeDataImporter (TradeDataIO.cs)

**Файл:** `Assets/Editor/TradeDataIO.cs`
**Namespace:** `Editor`
**Тип:** `static class TradeDataImporter`
**Меню:** `Trade → Import Trade Data`

#### Назначение
Импортирует **товары** (`Item`) и **города с ценами** (`CityData`) из единого TSV-файла, выбираемого через диалог.

#### Константы путей

```csharp
private const string BaseDataPath = "Assets/Data";
private const string ItemsPath    = BaseDataPath + "/Items";
private const string CitiesPath   = BaseDataPath + "/Cities";
```

#### Ожидаемый формат файла

Разделитель — **табуляция** (`\t`), не запятая. Первая строка — заголовок (пропускается).

```
cityName	itemName	weight	stock	buyPrice	sellPrice
Мадинат аль-Ахлам	Звёздный песок	1	100	7	4
Мадинат аль-Ахлам	Жемчуг желаний	2	100	26	16
Вади аль-Сараб	Звёздный песок	1	80	9	5
```

| Колонка (индекс) | Назначение | Используется для |
|---|---|---|
| 0 | `cityName` | поиск/создание `CityData` |
| 1 | `itemName` | поиск/создание `Item` |
| 2 | `weight` | только при создании нового `Item` |
| 3 | `stock` | `CityData.CityItem.stock` |
| 4 | `buyPrice` | `CityData.CityItem.buyPrice` |
| 5 | `sellPrice` | `CityData.CityItem.sellPrice` |

#### Алгоритм `ImportTradeData()`

```
1. Открыть диалог выбора файла (EditorUtility.OpenFilePanel, фильтр "csv")
   — если отменено → выход
2. Создать папки Assets/Data, Assets/Data/Items, Assets/Data/Cities (если их нет)
3. Загрузить ВСЕ существующие CityData из Cities/ и Item из Items/
   (через AssetDatabase.FindAssets + LoadAssetAtPath)
4. Прочитать строки файла, пропустить пустые
5. Для каждой строки (начиная со 2-й):
   a. parts = line.Split('\t')
   b. если parts.Length < 6 → Debug.LogWarning, пропустить
   c. cityName = parts[0].Trim(), itemName = parts[1].Trim()
   d. найти City по cityName в allCities (FirstOrDefault)
      — если не найден: создать новый CityData.asset, добавить в allCities
   e. найти Item по itemName в allItems (FirstOrDefault)
      — если не найден: создать новый Item.asset (weight = parts[2]), 
        добавить в allItems, createdItems++
   f. найти CityItem внутри city.items по ссылке на item
      — если не найден: создать новый CityData.CityItem { item = item }, добавить в city.items
   g. перезаписать cityItem.stock / buyPrice / sellPrice из строки (TryParseInt)
   h. EditorUtility.SetDirty(city), updatedCities++
6. AssetDatabase.SaveAssets()
7. AssetDatabase.Refresh()
8. Лог итогов: создано items, обновлено cities
```

#### Вспомогательные методы

```csharp
LoadAllAssets<T>(folder)   // AssetDatabase.FindAssets($"t:{typeof(T).Name}", ...) + загрузка
EnsureDirectoryExists(path) // AssetDatabase.IsValidFolder + CreateFolder при отсутствии
TryParseInt(value, default) // int.TryParse с безопасным fallback
TryParseFloat(value, default) // объявлен, но не используется в текущем коде
```

#### Важные отличия от BattleCardImporter

| | BattleCardImporter | TradeDataImporter |
|---|---|---|
| Разделитель | `,` (запятая) | `\t` (табуляция) |
| Идентификация существующего ассета | по имени файла на диске | по полю объекта (`cityName`/`itemName`) среди уже загруженных в память |
| Выбор файла | вручную вписывается путь в окне | системный диалог выбора файла |
| Поддержка дозаписи цен в существующий город | — (нет городов вообще) | да: `cityItem` ищется и обновляется, а не пересоздаётся |
| Удаление старых записей | нет | нет |

#### Известные риски / ограничения

| Риск | Описание |
|---|---|
| `weight` товара обновляется только при создании | Если `Item` уже существует, новое значение `weight` из файла **игнорируется** |
| Поиск по `Trim()`-имени | Лишние пробелы в середине названия города/товара создадут дубликат |
| `TryParseFloat` не используется | Мёртвый код, оставлен для возможного будущего использования (например, модификаторов цены) |
| Нет валидации количества колонок > 6 | Дополнительные колонки в файле просто игнорируются (не ошибка) |
| Диалог `OpenFilePanel` фильтрует `"csv"` | Хотя реальный формат — TSV; расширение файла не проверяется построчно, только в диалоге выбора |

---

## 2. Игровые скрипты — построчный разбор ключевой логики

### 2.1 GameManager.cs

```csharp
public static GameManager Instance { get; private set; }
```
Классический Singleton без `DontDestroyOnLoad` — подразумевается, что `GameManager` живёт на той же сцене всю игру (нет смены сцен в текущей реализации).

**Awake():** если `Instance` уже занят другим экземпляром — `Destroy(gameObject)` текущего. Иначе регистрирует себя, подписывается на `DiceSystem.OnDiceEvent`, вызывает `ValidateReferences()`.

**Start():** вызывает `StartNewTurn()`.

**HandleEndTurnRequest()** — ключевая точка входа из UI:
```csharp
if (_isEventPhaseActive) { /* блокирует повторный End Turn во время события */ return; }

bool hasActivePath = playerToken.PathController.HasActivePath();
if (hasActivePath)
{
    playerToken.AdvanceToken();
    if (playerToken.PathController.HasActivePath())  // путь ещё не завершён
        diceSystem.RollDice();
    // если путь завершился — AdvanceToken() сам вызвал ArriveAtDestination(),
    // а та — CompleteMovementPhase(); кубик не бросается
}
```
Это означает: **кубик бросается только если после шага остался путь**. Последний шаг маршрута не сопровождается броском кубика — вместо этого сразу открывается город.

**HandleDiceEvent(type)** — реагирует на `OnDiceEvent`, выставляет `_isEventPhaseActive = true`, диспетчеризует на `BattleManager` или `CardManager` через `?.` (null-safe — если ссылка не назначена, событие просто проглатывается без ошибки).

### 2.2 PlayerToken.cs

**AdvanceToken()** — порядок операций важен:
```csharp
1. pathController.Advance()              // currentCellIndex++
2. OnPlayerMoved?.Invoke()
3. if IsPathCompleted() → ArriveAtDestination()
   else:
     a. pathController.MoveCurrent()      // визуально переместить токен
     b. teamSystem.PaySalaries()          // зарплата списывается КАЖДЫЙ шаг, не каждый ход
     // c. HandleCurrentCellEffect — ЗАКОММЕНТИРОВАНО, эффект клетки сейчас не применяется напрямую
```
**Важно:** строка с `pathController.HandleCurrentCellEffect(...)` закомментирована — обработка эффекта клетки (Battle/Event) теперь идёт через `DiceSystem`, а не через тип клетки (`CellType`). Это значит, что enum `CellType` в `Archive/Cell.cs` сейчас не определяет события — они полностью случайны через кубик.

**ArriveAtDestination()**:
```csharp
finishCity = pathController.CurrentPath?.FinishCity;
if (finishCity == null) { LogWarning; return; }  // защита от незаполненного City на маршруте
pathController.ResetToken();
OnPlayerArrivedAtCity?.Invoke(finishCity);
GameManager.Instance?.CompleteMovementPhase();
```

### 2.3 TradeSystem.cs — самоподписка

```csharp
private void Awake()
{
    OnTradeRequest += OpenTrade;   // подписывается на СВОЁ СОБСТВЕННОЕ статическое событие
    ValidateReferences();
}
```
Это нетипичный, но рабочий паттерн: `RequestTrade(city)` — статический метод, вызываемый откуда угодно (например, из `CityPanel`) без прямой ссылки на экземпляр `TradeSystem`. Экземпляр подписывается на собственное событие в `Awake()`, поэтому фактическая обработка всё равно происходит в контексте конкретного `MonoBehaviour` с доступом к `playerInventory`/`tradeUIManager`.

⚠️ Если в сцене окажется **два** объекта `TradeSystem`, оба подпишутся на `OnTradeRequest`, и `OpenTrade` сработает дважды.

### 2.4 TradeTransactionHandler.cs — чистая статическая логика

Не наследует `MonoBehaviour` — это набор чистых функций, принимающих все зависимости параметрами. Удобно для unit-тестирования (хотя тестов в проекте сейчас нет, judging by `find` результатов).

```csharp
CalculateFinalPrice(basePrice, isBuying, playerStats)
// bargainEffect = Bargain * 0.01
// buy:  basePrice * (1 - bargainEffect)
// sell: basePrice * (1 + bargainEffect)
// итог: Mathf.Max(1, ...) — цена никогда не опускается ниже 1
```

`ExecuteBuyTransaction` / `ExecuteSellTransaction` напрямую мутируют переданные объекты (`playerInventory.Money -=`, `city.cityGold +=`, `cityItem.stock -=`) — побочные эффекты внутри статических методов, нет возврата нового состояния.

### 2.5 PathCellInitializer.cs / Cell.cs (Archive)

```csharp
foreach (Transform child in transform)
    if (child.CompareTag("Cell"))
    {
        cell = child.GetComponent<Cell>() ?? child.gameObject.AddComponent<Cell>();
        cell.cellNumber = cellIndex++;
        cell.name = $"Cell_{cellIndex}";
    }
```
Требует, чтобы дочерние объекты маршрута имели **Unity Tag = "Cell"**, иначе они не учитываются в пути и `PathController.InitializePathCells()` получит укороченный/неверный массив.

### 2.6 CardManager.cs / BattleManager.cs — единый контракт завершения

Оба сервиса обязаны вызвать `gameManager.CompleteEventPhase()` по завершении своей работы — это контракт, на котором держится весь цикл фаз. `CardManager.DrawCard()` делает это через `Invoke(nameof(...), 0.5f)` (искусственная задержка для ощущения «анимации»), `BattleManager` — синхронно сразу после `ProcessBattleResult()`.

⚠️ **Риск:** если оба `gameManager` не назначены в инспекторе (`null`), `gameManager.CompleteEventPhase()` упадёт с `NullReferenceException`, и `_isEventPhaseActive` останется `true` навсегда — игра «зависнет», блокируя `HandleEndTurnRequest()`.

### 2.7 PlayerInventory.cs — Persistence через PlayerPrefs

```csharp
SaveInventory():
  PlayerPrefs.SetInt("PlayerMoney", Money)
  PlayerPrefs.SetInt("InventoryCount", items.Count)
  for i in items: SetString($"InventoryItem_{i}", item.name), SetInt($"InventoryQuantity_{i}", qty)

LoadInventory():
  Money = GetInt("PlayerMoney", startMoney)
  items.Clear()
  for i in 0..count: 
    name = GetString($"InventoryItem_{i}")
    item = Resources.Load<Item>($"Items/{name}")   // ⚠️ требует Items/ внутри папки Resources!
```
**Критическое ограничение:** `Resources.Load<Item>("Items/...")` работает только если ассеты `Item` физически лежат в `Assets/Resources/Items/`. В текущей структуре проекта товары лежат в `Assets/Data/Items/` — **не** в `Resources`. Это значит, что `LoadInventory()` в текущем виде **не найдёт** предметы и метод фактически не функционален без переноса/дублирования ассетов в `Resources`.

---

## 3. Сводная таблица событийных контрактов

| Издатель | Событие | Сигнатура | Подписчики |
|---|---|---|---|
| `DiceSystem` | `OnDiceEvent` | `Action<DiceEventType>` | `GameManager` |
| `PlayerToken` | `OnPlayerArrivedAtCity` | `Action<City>` | `CityManager` |
| `PlayerToken` | `OnPlayerMoved` | `Action` | *(нет подписчиков в текущем коде)* |
| `CityPanel` | `OnPathSelected` | `Action<PathCellInitializer>` | `PlayerToken` |
| `TradeSystem` | `OnTradeRequest` | `Action<City>` | `TradeSystem` (сам на себя) |
| `TradeUIManager` | `OnTradeClosedRequest` | `Action` | `CityManager` |
| `PlayerInventory` | `OnInventoryChanged` | `Action` | *(нет подписчиков в текущем коде)* |
| `PlayerInventory` | `OnMoneyChanged` | `Action` | *(нет подписчиков в текущем коде)* |

> ⚠️ Несколько событий (`OnPlayerMoved`, `OnInventoryChanged`, `OnMoneyChanged`) объявлены, но в текущей версии кода никто на них не подписан — вероятно, заготовки для будущего UI (например, анимации монет или индикатора движения).

---

## 4. Рекомендации (на основе разбора)

1. **BattleCardImporter:** добавить try/catch вокруг `int.Parse`, чтобы битая строка не останавливала весь импорт.
2. **TradeDataImporter:** обновлять `item.weight` не только при создании, но и при повторном импорте — иначе изменения веса в файле не подтянутся.
3. **PlayerInventory.LoadInventory():** либо перенести `Item`-ассеты в `Assets/Resources/Items/`, либо переписать загрузку через `AssetDatabase`/собственный реестр, иначе сохранение не имеет практического эффекта при загрузке.
4. **GameManager:** добавить защитные проверки на `null` перед вызовом `CompleteEventPhase()`, чтобы избежать зависания `_isEventPhaseActive`.
5. **TradeSystem:** задокументировать или защититься от случая нескольких экземпляров в сцене (например, проверкой Singleton в `Awake()`).
