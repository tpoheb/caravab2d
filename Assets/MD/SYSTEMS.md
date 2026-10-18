# Справочник систем — 1000 Дорог

Актуально на 2026-10-06. Справочник описывает существующие методы, события и ограничения, а не целевую модель из GDD. Общий цикл: [ARCHITECTURE.md](ARCHITECTURE.md).

Дополнено 2026-10-18: [актуальный набор карт](DOCUMENT_CARDS.md), [отложенные городские эффекты](CITY_EFFECTS.md) и [меню/сохранения](MENU_AND_SAVES.md).
## GameManager и GameState

Файлы: `Assets/Scripts/GameManager.cs`, `Assets/Scripts/GameState.cs`. Singleton: `GameManager.Instance`.

| API | Назначение |
|---|---|
| `State` | `Idle`, `InCity`, `Moving`, `DrawingCard`, `PreparingEncounter`, `ResolvingEvent`, `InBattle`, `PostEncounter`, `EndingTurn`, `WaitingForAI` |
| `TurnNumber` | Номер личного хода игрока; первый задаётся при запуске, следующий — после ИИ |
| `OnPathSelected(path)` | Только из `InCity`: устанавливает путь и сразу делает первый шаг |
| `ContinueMovement()` | Только из `Moving`: шаг на клетку либо вход в город |
| `RequestEndTurn()` | Делегирует `AdvancePhase()`: подготовка → встреча → постфаза → завершение; эффекты, сброс и ИИ только после постфазы |
| `CompleteEventPhase()` | Подтверждает событие из `ResolvingEvent`, открывает `PostEncounter` |
| `OnShadowCardRevealed()` | Открывает `PreparingEncounter` без применения события |
| `OnBattleCardRevealed()` | Помечает активный бой и открывает `PreparingEncounter` без броска |
| `OnBattleForceEnded()` / `OnCardCancelled()` | Переводят в `PostEncounter` |
| `OnDiceRolled(int value)` | Только в `InBattle`: обновляет бросок через `ExecuteBattle()`, оставляет окно боевых карт открытым |
| `PromptDiceChoice()` / `OnDiceChoiceSelected(int)` | Выбор числа 1–6 в подготовке или во время боя; блокирует переход до ответа |
| `PromptDoubleGoods()` / `OnDoubleGoodsSelected(Item)` | Удвоение товара; полноценный UI выбора пока заменён заглушками |

`StartTurn()` и `WaitForAIAndBeginPlayerTurn()` — приватные методы. Результат остаётся на экране до подтверждения постфазы; автоматической задержки завершения нет. Вход в город и выход на первую клетку следующего маршрута не увеличивают счётчик по отдельности.

## DiceSystem

Файл: `Assets/Scripts/DiceSystem.cs`.

```csharp
public event Action<int> OnDiceRolled;
public int LastRollResult { get; private set; }
public void RollDice();
```

Диапазон по умолчанию — 1–6. Кубик используется для атаки, тип карты выбирает общая колода. Файл `DiceEventType.cs` остался в проекте, но текущий игровой поток этот enum не использует.

## PlayerToken и путь

Файл: `Assets/Scripts/PlayerToken.cs`.

```csharp
public event Action<City> OnArrivedAtCity;
public event Action OnStepCompleted;
public void StartPath(PathCellInitializer path);
public bool AdvanceToken();
```

`AdvanceToken()` вызывает `PathController.Step()`. На клетке перемещает фишку, выплачивает зарплату и публикует `OnStepCompleted`. Если путь завершён — сбрасывает его и публикует прибытие, без зарплаты и события шага. Возвращает `true` при завершении пути.

| API `PathController` | Назначение |
|---|---|
| `HasActivePath`, `CurrentPath` | Состояние текущего пути |
| `IsReadyToArrive` | Есть путь и достигнута последняя клетка; используется для бесплатного входа ИИ в город |
| `SetPath(path)` | Кэширует клетки, сбрасывает индекс в −1, активирует фишку |
| `Step()` | Увеличивает индекс; возвращает `true` после выхода за клетки |
| `MoveCurrent()` | Перемещает фишку на текущую клетку |
| `ResetPath()` | Сбрасывает путь и скрывает фишку |
| `SetTokenObject(token)` | Назначает отображаемую фишку |

`PathCellInitializer.FinishCity` задаёт город назначения. `InitializeCells()` нумерует только детей с тегом `Cell`, добавляя компонент при необходимости. Сам `PathController` кэширует всех прямых детей и ожидает на каждом `Cell`; посторонние дети могут привести к несовпадению или ошибке.

## CardDeck

Файл: `Assets/Scripts/Card/CardDeck.cs`. Одна типизированная коллекция `ICard`, использующая `UnityEngine.Random`; вызывать на основном потоке.

| API | Назначение |
|---|---|
| `Add(card)` / `AddRange(cards)` | Добавление без null и повторной ссылки на один ассет |
| `Draw(condition = null, weightSelector = null)` | Выбор по положительным весам и удаление выбранной карты |
| `Count`, `IsEmpty` | Число оставшихся карт и пустота |
| `Peek()` | Первый элемент списка без расходования; не предсказывает следующий взвешенный выбор |
| `Clear()` / `Shuffle()` | Очистка / перестановка внутреннего списка |

Без `weightSelector` берётся поле `weight` у `ShadowCardData`/`BattleCardData`. Селектор позволяет позднее учитывать клетку, но пока не подключён к игровому менеджеру.

## CardManager и CardDeckUI

Файлы: `Assets/Scripts/Card/CardManager.cs`, `Assets/Scripts/Card/CardDeckUI.cs`.

`CardManager` владеет колодой, сбросом и `CurrentCard`. Каталоги `allShadowCards` и `allBattleCards` объединяются при запуске.

| API `CardManager` | Назначение |
|---|---|
| `RemainingCards`, `DiscardCount`, `CurrentCard` | Единственное игровое состояние колоды |
| `DrawCard()` | Вытягивание по весам без фильтра сложности |
| `DrawCard(Func<bool> extraConditions)` | То же вытягивание с дополнительной булевой проверкой допуска |
| `HideEventCard()` | Сброс текущей карты ровно один раз, скрытие UI и обновление счётчиков |

Повторное вытягивание при незавершённой текущей карте отклоняется. Пустая колода восстанавливается из сброса. Если нет подходящих карт или дополнительное условие ложно, менеджер разрешает завершить ход через `OnCardCancelled()`; карта не расходуется. Отмена уже выбранной карты флагом руки расходует эту карту.

| API `CardDeckUI` | Назначение |
|---|---|
| `SetDeckCounts(remaining, discarded)` | Получает счётчики менеджера, обновляет текст числа оставшихся |
| `ShowCard(card, onRevealed = null)` | Мгновенно показывает карту и вызывает callback |
| `ShowCancelledCard(card)` | Обёртка отменённой карты и сообщение в `EventPanelUI` |
| `DiscardCurrentCard()` | Уведомление UI о сбросе и скрытие |
| `HideCurrentCard()` | Только скрытие отображения |
| `OnCardDrawn`, `OnCardRevealed`, `OnCardDiscarded` | События экземпляра `Action<ICard>` |

`CardDisplay` показывает название и характеристики без flip-анимации. Полное описание выводит `EventPanelUI`. `CancelledCardWrapper` объявлен в файле `CardDeckUI.cs`.

## BattleManager

Файл: `Assets/Scripts/Card/BattleManager.cs`.

| API | Назначение |
|---|---|
| `PrepareBattle(card)` → `bool` | Инициализирует бой до розыгрыша карт подготовки, без броска |
| `ExecuteBattle(diceValue)` | Сохраняет бросок и показывает значения с накопленным ослаблением врага |
| `RequestNewRoll()` | Генерирует новый D6 и пересчитывает отображение |
| `AddAttackBonus(value)` | Добавляет бонус текущего боя |
| `FinalizeBattle()` | Фиксирует исход и выдаёт победную награду; штраф ждёт `SettleBattle()` после посткомбата |
| `SettleBattle()` | Списывает штраф с учётом карт посткомбата и очищает состояние боя |
| `AddEnemyAttackDebuff(value)` | Ослабляет врага до конца текущей битвы, включая перебросы |
| `ForceEndBattle(escaped)` | Завершает бой принудительно и уведомляет GameManager |
| `OnBattleWon`, `OnBattleLost`, `OnBattleEscaped` | События экземпляра `Action` |

```text
атака игрока = TeamSystem.GetTotalAttack() + бонус боя + последний бросок
атака врага = max(0, requiredAttack − ослабление врага)
победа: атака игрока >= атака врага
```

Победа выдаёт деньги и случайную карту из `HandManager.rewardPool` при фиксации исхода; поражение списывает денежный штраф после посткомбата с учётом снижения. Поля `rewardHandCardID` и `crewLoss` текущий `BattleManager` не применяет.

## ShadowEffectManager и HandManager

Файлы: `Assets/Scripts/Card/ShadowEffectManager.cs`, `Assets/Scripts/Card/HandManager.cs`.

`ShadowEffectManager.ApplyCard()` применяет событие; `ApplyTransientCard()` создаёт временный объект данных для эффекта руки. `ProcessTurn()` уменьшает оставшуюся длительность в конце личного хода игрока. При прибытии `CityManager` вызывает `OnEnterCity()` для разовой доплаты жалованья и `ConsumeBonusTrade()` для передачи процентов в бонус следующей успешной продажи. Переоткрытие панели и `RestoreCity()` их не вызывают. См. [CITY_EFFECTS.md](CITY_EFFECTS.md).

`HandManager.AddCard()`, `UseCard(index)`, `DiscardCard(index)` и `GiveRandomReward()` управляют рукой. Использование удаляет карту из `currentHand`; отдельный список сброса руки отсутствует.

`CanUseCard(index)` проверяет `HandCardData.playTiming` и применимость эффекта; `UseCard()` повторяет эту проверку перед расходованием. UI руки виден и в городе. Отмена действует на раскрытую карту до разрешения; побег открывает посткомбат; снижение штрафа доступно после поражения.

## ИИ и общий рынок

Файлы: `Assets/Scripts/AI/`.

| Класс / API | Назначение |
|---|---|
| `AITurnManager.ProcessAITurnAndWait()` | Корутина цикла ИИ, которую ждёт GameManager |
| `AITurnManager.ProcessAITurn()` | Совместимый запуск корутины без ожидания у вызывающего |
| `TurnQueue.ProcessTurnCoroutine()` | Общий снимок, фоновое планирование, последовательное исполнение |
| `AITrader.PrepareTurn()` | Бесплатный вход с последней клетки в город перед снимком |
| `ITrader.PlanTurn(snapshot)` | План без изменения мира и Unity API |
| `ITrader.ExecuteTrade(intent, economy)` | Торговля при нахождении в городе |
| `ITrader.ExecuteMovement(intent)` | Корутина одного шага |
| `PathClaims.TryClaim(path)` | Первый ИИ в очереди занимает путь на цикл |
| `WorldEconomy.GetCityData(city)` | Текущий `City.RuntimeData`, без подмены исходным ассетом |
| `WorldEconomy.TakeSnapshot()` | Копии цен, запасов и доступных путей |
| `WorldEconomy.Buy()` / `Sell()` | AI-сделки на общем runtime-рынке |

Планирование ИИ параллельное; торговля и движение последовательны в порядке списка. `Initiative` сохранена в данных, но не определяет очередь. ИИ не получают дорожные карты.

`GameSnapshot` копирует коллекции. `CitySnapshot.GetPrice()`, `GetSellPrice()`, `GetStock()` читают скопированные значения; `Paths` — копия списка ссылок. `GetCity()` сравнивает ссылки без Unity API. Сами Unity-объекты не клонируются и не должны разыменовываться из фонового потока.

## Город, торговля и команда

| Система | Актуальный контракт |
|---|---|
| `City` | `InitRuntimeData()`, `RuntimeData`, `Data`, `CityName`, список `Paths` |
| `CityManager` | Создаёт runtime-рынки, слушает `PlayerToken.OnArrivedAtCity`, открывает город и возвращает его после торговли |
| `CityPanel` (`Scripts/UI/`) | `OpenPanel(city)`, `ClosePanel()`; путь передаёт прямо в GameManager, затем публикует `OnPathSelectedEvent` |
| `TradeSystem` (`Scripts/Trade/`) | Статические `RequestTrade(city)` и `OnTradeRequest`; `BuyItem()`, `SellItem()`, `CloseTrade()`; работает с runtime-рынком |
| `TradeTransactionHandler` | `ProcessBuyTransaction(..., out int totalCost)` / `ProcessSellTransaction(..., out int totalValue)` возвращают `bool` |
| `TradeUIManager` | `OpenTradePanel()`, `CloseTradePanel()`, `RefreshTradeUI()` и статическое `OnTradeClosedRequest` |
| `PlayerInventory` | Товары, деньги, вес, средняя цена; события экземпляра `OnInventoryChanged`, `OnMoneyChanged` |
| `PlayerStats` (`Scripts/Hire/`) | `Initialize()`, изменение характеристик и событие `OnStatsChanged` |
| `TeamSystem` | `TryHireUnit()`, `FireUnit()`, `PaySalaries()`, `GetTotalAttack()`; зарплата за шаг на клетку |
| `TopBarUI` (`Scripts/UI/`) | Обновление по событиям инвентаря и характеристик |

Сделки игрока считают динамическую цену каждой единицы с торгом и модификаторами карт; AI-сделки пока считают сумму по начальной цене партии. Обе стороны меняют один запас, деньги и цены города. Торговля и наём не завершают личный ход.

## Editor-проверки

`Assets/Editor/Tests/CardSystemChecks.cs`: меню **Tools → Проверки → Фазы хода, колода и общий рынок**, либо `CardSystemChecks.Run` через `-executeMethod`. Проверки используют реальные игровые классы и временные объекты, удаляемые после выполнения. Это собственные Editor-проверки, не NUnit-тесты.
