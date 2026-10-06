# Скрипты и импортёры — текущая реализация

Сверено с `.cs` файлами 2026-10-06. API систем: [SYSTEMS.md](SYSTEMS.md); последовательность фаз: [ARCHITECTURE.md](ARCHITECTURE.md).

## Импортёры

Editor-инструменты находятся в `Assets/Editor/` и создают исходные ассеты. Импорт не добавляет новые карты в списки `CardManager` автоматически: ссылки надо назначить в Inspector.

| Инструмент | Меню Unity | Источник / результат |
|---|---|---|
| `BattleCardImporter` | Tools → Battle Cards → Import from CSV | Выбранный CSV/TSV → `Assets/Data/BattleCards/` по умолчанию |
| `ShadowCardCSVImporter` | Tools → Cards → Import Shadow Cards from CSV | `Assets/Data/ShadowCards.csv` → `Assets/Data/ShadowEvent/` |
| `ShadowCardImporter` | Tools → Cards → Create Shadow Cards | Встроенные определения событий → ассеты |
| `HandCardImporter` | Tools → 1000 Roads → Import Hand Cards | Встроенные определения руки → `Assets/Data/HandCard/` |
| `Editor.TradeDataImporter` | Tools → Trade → Import Trade Data | Выбранный CSV → `Assets/Data/Items/` и `Assets/Data/Cities/` |

### BattleCardImporter

Файл: `Assets/Editor/BattleCardImporter.cs`. Требует восемь позиционных колонок; заголовок пропускается.

```csv
ID,EnemyName,Description,RequiredAttack,RewardMoney,PenaltyMoney,CrewLoss,RewardHandCardID
101,Песчаные разбойники,"Разбойники перекрыли дорогу.",5,100,50,0,0
```

Разделитель определяется по заголовку: табуляция при наличии таба, иначе запятая. Для CSV поддерживаются кавычки и экранированные двойные кавычки; TSV разбивается по табам. Числовые поля проверяются через `TryParse`, ошибки выводятся в Console.

Ассет ищется по ожидаемому имени `Card_<ID>_<Name>.asset`, а не через реестр ID. Переименование при том же ID может создать второй ассет. Старые карты не удаляются автоматически. Ошибка числового поля может оставить частично обновлённый ассет.

Поле `weight` этим форматом не импортируется: его надо настроить в Inspector. Поля `minDifficulty` и `maxDifficulty` также не импортируются, но больше не ограничивают вытягивание. `crewLoss` и `rewardHandCardID` импортируются в данные, но текущая боевая логика их не применяет.

### ShadowCardCSVImporter

Файл: `Assets/Editor/ShadowCardCSVImporter.cs`. Читает заголовки без учёта регистра, по ним сопоставляет колонки; обязательны `ID`, `Name`, `Description`, `EffectType`.

```csv
ID,Name,Description,EffectType,Value,IsTemporary,Duration,MinDifficulty,MaxDifficulty,Weight,PenaltyValue
1,Найденный кошель,"На дороге лежит забытый кошель.",Money,10,0,1,0,10,15,0
2,Песчаная слепота,"Пыль мешает бойцам видеть.",Attack,-2,1,2,0,10,10,0
```

Парсер принимает запятую или табуляцию вне кавычек. Необязательные числовые поля получают значения по умолчанию. `IsTemporary` распознаёт `1`, `true`, `yes`.

Ассеты именуются `Shadow_<ID:000>_<Name>.asset`; переименование также может создать дубликат. Неизвестный `EffectType` сейчас преобразуется в `Money`, поэтому названия эффектов следует проверять. Колонки `System`, `Intensity`, `Tone` зарезервированы и не влияют на розыгрыш.

### TradeDataImporter

Файл: `Assets/Editor/TradeDataImporter.cs`, namespace `Editor`. Текущий разделитель — **запятая**, TSV этим импортёром не читается.

```csv
cityName,itemName,weight,stock,buyPrice,sellPrice
Кочевье,Финики,1,100,7,4
```

Расширенный формат имеет десять колонок и необязательную одиннадцатую:

```csv
cityName,itemName,weight,stock,buyPrice,sellPrice,minBuyPrice,maxBuyPrice,volatility,regenRate,demand
Кочевье,Финики,1,100,7,4,3.5,14,0.02,0.10,normal
```

Первая строка пропускается. Парсер использует простой `Split(',')`, поэтому поля с запятыми в названии не поддерживаются, а десятичные числа в CSV надо записывать с точкой.

Импорт идёт двумя проходами: товары, затем города. Существующий `Item.weight` обновляется. При первом упоминании существующего города его `items` очищается и заново заполняется строками текущего файла; записи, отсутствующие в файле для этого города, не сохраняются.

Идентификация идёт по пути ассета с очищенным именем. Диапазоны цен продажи выводятся из диапазона покупки и соотношения базовых цен. Значения `demand` из файла не следует считать постоянной настройкой: спрос пересчитывается игровыми методами.

### HandCardImporter и ShadowCardImporter

Создают карты из определений в исходниках, а не из CSV. HandCardImporter пропускает существующие ассеты, сохраняя иконки и настройки; новые ассеты получают начальные `playTiming`. Категория не задаёт фазу: метки меняются в Inspector.

## Порядок выполнения игровых скриптов

### GameManager

`Awake()` регистрирует Singleton и проверяет зависимости. `OnEnable()` подписывается на экземпляры кубика и фишки, добавляет обработчик кнопки вытягивания. `Start()` инициализирует характеристики и первый личный ход в городе.

При выборе пути `OnPathSelected()` делает шаг. Кнопка в `DrawingCard` блокирует повторный клик и вызывает `CardManager.DrawCard()`; раскрытие открывает `PreparingEncounter`.

`RequestEndTurn()` вызывает `AdvancePhase()`: подготовка → битва/событие → постфаза. Только подтверждение постфазы устанавливает `EndingTurn`, списывает штраф, тикает эффекты, сбрасывает карту и запускает `WaitingForAI`.

`WaitForAIAndBeginPlayerTurn()` ожидает всю очередь ИИ. После неё увеличивает личный номер хода и продолжает движение либо вводит игрока в город. `CompleteEventPhase()` подтверждает событие и открывает постфазу.

### PlayerToken и PathController

`StartPath()` устанавливает путь, но сам не делает шаг. `AdvanceToken()` вызывает `Step()`, а затем либо перемещает фишку на клетку с зарплатой и `OnStepCompleted`, либо сбрасывает путь и публикует `OnArrivedAtCity`.

`PathCellInitializer` инициализирует только детей с тегом `Cell`; `PathController` кэширует всех прямых детей и ожидает у каждого компонент `Cell`. Тип `CellType` сейчас не определяет карту или её вес.

### CardManager и UI

Два сериализованных каталога объединяются в один `CardDeck` при запуске. `Draw()` расходует карту; менеджер хранит её как текущую до конца личного хода. Сброс принадлежит менеджеру, UI получает только счётчики.

Карты раскрываются без применения эффекта и боевого броска. Подтверждение подготовки открывает битву с кубиком либо событие с окном карт до применения. Отмена раскрытой карты и побег ведут в постфазу.

`ShowCard()` показывает лицевую сторону синхронно и вызывает callback. Если префаб отображения не назначен, логируется ошибка, но callback всё равно исполняется. Искусственного ожидания анимации и отдельной очереди UI нет.

### AI и WorldEconomy

`AITrader.PrepareTurn()` входит в город с последней клетки до создания снимка. `WorldEconomy.TakeSnapshot()` копирует runtime-цены покупки/продажи, запасы и пути. `PlanTurn()` через `Task.Run` возвращает намерение, не фиксируя маршрут и не меняя мир.

`TurnQueue` ждёт все планы и исполняет в порядке списка: проверка занятости пути, торговля, корутина движения до конца. Инициатива не используется. На занятый путь не выходит второй ИИ в том же цикле; ИИ, уже находящийся в пути, сохраняет выбранный маршрут.

`WorldEconomy.GetCityData()` каждый раз получает `City.RuntimeData`, поэтому порядок `Awake()` не закрепляет ссылку на исходный ассет. AI и игрок торгуют с одним запасом и городским кошельком. При исполнении устаревшее намерение может не пройти проверку текущего рынка.

### TradeSystem и экономика

`TradeSystem` подписывается на своё статическое `OnTradeRequest` в `Awake()` и отписывается в `OnDestroy()`. Несколько экземпляров обработают один запрос несколько раз.

Игровой шаг `PlayerToken.OnStepCompleted` запускает `RegenerateGlobalEconomy()` для runtime-городов из списка `TradeSystem`. Вход в город, сделки и ходы ИИ сами по себе этот обработчик не вызывают.

`TradeTransactionHandler` получает зависимости параметрами, возвращает `bool` и сумму через `out`. Методы мутируют переданные runtime-данные, инвентарь и деньги. Цена партии игрока считается по единицам с динамическим изменением цены; AI пока считает сумму по исходной цене партии и затем сдвигает цены.

## Основные события

| Издатель | Событие | Тип | Связь |
|---|---|---|---|
| `DiceSystem` | `OnDiceRolled` | экземпляр, `Action<int>` | GameManager → боевой бросок |
| `PlayerToken` | `OnArrivedAtCity` | экземпляр, `Action<City>` | GameManager и CityManager |
| `PlayerToken` | `OnStepCompleted` | экземпляр, `Action` | TradeSystem → обновление экономики |
| `CityPanel` | `OnPathSelectedEvent` | статическое, `Action<PathCellInitializer>` | Уведомление после прямого вызова GameManager |
| `TradeSystem` | `OnTradeRequest` | статическое, `Action<City>` | Самоподписка TradeSystem |
| `TradeUIManager` | `OnTradeClosedRequest` | статическое, `Action` | CityManager → возврат в город |
| `CardDeckUI` | `OnCardDrawn`, `OnCardRevealed`, `OnCardDiscarded` | экземпляр, `Action<ICard>` | Уведомления отображения |
| `PlayerInventory` | `OnInventoryChanged`, `OnMoneyChanged` | экземпляр, `Action` | В том числе TopBarUI |
| `PlayerStats` | `OnStatsChanged` | экземпляр, `Action` | В том числе TopBarUI |

## Известные ограничения

- Подготовка, встреча и постфаза интерактивны; допустимые моменты задаёт `HandCardData.playTiming`.
- `ProcessTurn()` уменьшает длительность и только что полученного эффекта в конце текущего личного хода. Новое правило длительности ещё не утверждено.
- `ShadowEffectManager.OnEnterCity()` и `ConsumeBonusTrade()` не подключены к текущему CityManager. Наличие полей и описаний в ассете не гарантирует применение этих эффектов в городе.
- Рука видна в городе; метка `InCity` разрешает розыгрыш. Сброс общей колоды не является сбросом руки.
- `BattleManager` пока не использует `crewLoss` и `rewardHandCardID`; за победу выдаёт случайную карту руки.
- `PlayerInventory.LoadInventory()` использует `Resources.Load("Items/...")`, а товары находятся в `Assets/Data/Items/`; текущая загрузка не восстанавливает эти ассеты.

## Проверки

Меню **Tools → Проверки → Фазы хода, колода и общий рынок** запускает `Assets/Editor/Tests/CardSystemChecks.cs`. Проверки используют реальные классы, временные объекты и не изменяют сцену. Покрытие и границы проверки приведены в [REFACTORING.md](REFACTORING.md).
