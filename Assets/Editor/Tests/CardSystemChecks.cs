using System;
using System.Collections.Generic;
using System.Collections;
using System.Reflection;
using UnityEditor;
using UnityEngine;

/// <summary>Проверки настоящих игровых классов в Unity Editor, без загрузки и изменения сцены.</summary>
public static class CardSystemChecks
{
    [MenuItem("Tools/Проверки/Фазы хода, колода и общий рынок")]
    public static void Run()
    {
        var objects = new List<UnityEngine.Object>();
        var randomState = UnityEngine.Random.state;
        try
        {
            UnityEngine.Random.InitState(3471);
            CheckWeightedDeck(objects);
            CheckDiscardCycle(objects);
            CheckSharedMarket(objects);
            CheckTurnSequence(objects);
            Debug.Log("CARD_SYSTEM_CHECKS_PASSED");
        }
        finally
        {
            UnityEngine.Random.state = randomState;
            for (int i = objects.Count - 1; i >= 0; i--)
                if (objects[i] != null) UnityEngine.Object.DestroyImmediate(objects[i]);
        }
    }

    private static void CheckWeightedDeck(List<UnityEngine.Object> objects)
    {
        var shadow = Create<ShadowCardData>(objects);
        var battle = Create<BattleCardData>(objects);
        shadow.weight = 1;
        battle.weight = 3;
        var deck = new CardDeck();
        deck.Add(shadow);
        deck.Add(battle);
        Require(deck.Draw(_ => false) == null && deck.Count == 2, "Фильтр не должен расходовать карты.");
        Require(deck.Draw(null, _ => 0) == null && deck.Count == 2, "Нулевые веса исключают розыгрыш.");
        var first = deck.Draw();
        var second = deck.Draw();
        Require(first != null && second != null && !ReferenceEquals(first, second), "Оба типа карт должны расходоваться без повторов.");
        Require(deck.IsEmpty && deck.Draw() == null, "Пустая колода не выдаёт карты.");

        int battleDraws = 0;
        for (int i = 0; i < 10000; i++)
        {
            deck.Clear();
            deck.Add(shadow);
            deck.Add(battle);
            if (ReferenceEquals(deck.Draw(), battle)) battleDraws++;
        }
        Require(battleDraws > 7100 && battleDraws < 7900, "Общий выбор должен учитывать отношение весов 1:3.");
    }

    private static void CheckDiscardCycle(List<UnityEngine.Object> objects)
    {
        var root = CreateRoot(objects);
        var manager = root.AddComponent<CardManager>();
        var game = root.AddComponent<GameManager>();
        var battleManager = root.AddComponent<BattleManager>();
        var ui = root.AddComponent<BattleUIManager>();
        Set(battleManager, "uiManager", ui);
        Set(game, "battleManager", battleManager);
        Set(game, "diceSystem", root.AddComponent<DiceSystem>());
        Set(manager, "gameManager", game);
        Set(manager, "battleManager", battleManager);
        Set(manager, "effectManager", root.AddComponent<ShadowEffectManager>());
        var shadow = Create<ShadowCardData>(objects);
        var battle = Create<BattleCardData>(objects);
        // Сложность в старых данных не должна исключать ни один тип карты.
        shadow.minDifficulty = shadow.maxDifficulty = 5;
        battle.minDifficulty = battle.maxDifficulty = 7;
        Set(manager, "allShadowCards", new List<ShadowCardData> { shadow });
        Set(manager, "allBattleCards", new List<BattleCardData> { battle });
        typeof(CardManager).GetMethod("RebuildDeck", BindingFlags.NonPublic | BindingFlags.Instance).Invoke(manager, null);
        manager.DrawCard();
        Require(manager.RemainingCards == 1 && manager.CurrentCard != null, "Вытягивание должно уменьшать колоду.");
        manager.HideEventCard();
        manager.HideEventCard();
        Require(manager.DiscardCount == 1, "Повторное завершение не должно дублировать сброс.");
        manager.DrawCard();
        manager.HideEventCard();
        Require(manager.RemainingCards == 0 && manager.DiscardCount == 2, "Вся колода должна перейти в сброс.");
        manager.DrawCard();
        Require(manager.RemainingCards == 1 && manager.DiscardCount == 0, "Исчерпанная колода должна восстановиться из сброса.");
        manager.HideEventCard();
    }

    private static void CheckSharedMarket(List<UnityEngine.Object> objects)
    {
        var item = Create<Item>(objects);
        item.itemName = "Проверочный товар";
        item.weight = 1;
        var source = Create<CityData>(objects);
        source.cityName = "Проверочный рынок";
        source.cityGold = 100;
        source.items.Add(new CityData.CityItem
        {
            item = item, stock = 5, baseBuyPrice = 10, currentBuyPrice = 10,
            baseSellPrice = 8, currentSellPrice = 8,
            minBuyPrice = 5, maxBuyPrice = 20, minSellPrice = 4, maxSellPrice = 16
        });
        var root = CreateRoot(objects);
        var city = root.AddComponent<City>();
        Set(city, "sourceData", source);
        city.InitRuntimeData();
        objects.Add(city.RuntimeData);
        var economy = root.AddComponent<WorldEconomy>();
        Set(economy, "cityBindings", new List<WorldEconomy.CityBinding>
        {
            new WorldEconomy.CityBinding { city = city, cityData = source }
        });
        Set(economy, "allCities", new List<CityData> { source });
        Require(ReferenceEquals(economy.GetCityData(city), city.RuntimeData), "AI должен видеть runtime-рынок игрока.");
        Require(economy.GetCityData(null) == null, "Пустой город не должен вызывать исключение.");
        var trader = root.AddComponent<AITrader>();
        var profile = Create<TraderProfile>(objects);
        profile.startGold = 100;
        trader.Initialize(profile, city, economy);
        economy.Buy(city, item.itemName, 1, trader);
        Require(city.RuntimeData.items[0].stock == 4, "Сделка AI должна менять общий запас.");
        var stats = Create<PlayerStats>(objects);
        stats.Initialize();
        var inventory = root.AddComponent<PlayerInventory>();
        Set(inventory, "playerStats", stats);
        inventory.Money = 100;
        Require(TradeTransactionHandler.ProcessBuyTransaction(city.RuntimeData.items[0], 1,
            city.RuntimeData, inventory, stats, out _), "Игрок должен торговать с тем же рынком.");
        Require(economy.GetCityData(city).items[0].stock == 3, "AI должен видеть сделку игрока.");
        Require(source.items[0].stock == 5 && source.cityGold == 100 && source.items[0].currentBuyPrice == 10,
            "Сделки не должны изменять исходный ассет.");
        var snapshot = economy.TakeSnapshot();
        Require(snapshot.Cities[0].GetStock(item.itemName) == 3,
            "Снимок должен отражать общий runtime-рынок.");
        float snapshotBuyPrice = city.RuntimeData.items[0].currentBuyPrice;
        float snapshotSellPrice = city.RuntimeData.items[0].currentSellPrice;
        city.RuntimeData.items[0].stock = 0;
        city.RuntimeData.items[0].currentBuyPrice = 1000;
        city.RuntimeData.items[0].currentSellPrice = 1000;
        city.Paths.Add(CreateRoot(objects).AddComponent<PathCellInitializer>());
        Require(snapshot.Cities[0].GetStock(item.itemName) == 3 &&
            snapshot.Cities[0].GetPrice(item.itemName) == snapshotBuyPrice &&
            snapshot.Cities[0].GetSellPrice(item.itemName) == snapshotSellPrice && snapshot.Cities[0].Paths.Count == 0,
            "Снимок не должен меняться вместе с runtime-рынком и списком путей.");
        var planned = System.Threading.Tasks.Task.Run(() => trader.PlanTurn(snapshot)).GetAwaiter().GetResult();
        Require(planned.SelectedPath == null && planned.BuyOrders.Count == 1,
            "Планирование в фоне должно использовать прежний снимок, а не изменённый рынок.");
    }

    private static void CheckTurnSequence(List<UnityEngine.Object> objects)
    {
        var root = CreateRoot(objects);
        var city = root.AddComponent<City>();
        var source = Create<CityData>(objects);
        Set(city, "sourceData", source);
        city.InitRuntimeData();
        objects.Add(city.RuntimeData);
        var economy = root.AddComponent<WorldEconomy>();
        Set(economy, "cityBindings", new List<WorldEconomy.CityBinding>
        {
            new WorldEconomy.CityBinding { city = city, cityData = source }
        });
        var trace = new List<string>();
        // Инициативы намеренно обратны очереди: они не должны влиять на порядок.
        var first = new CheckTrader("first", 1, city, trace);
        var second = new CheckTrader("second", 100, city, trace);
        var queue = new TurnQueue(new List<ITrader> { first, second }, economy);
        var aiManager = root.AddComponent<AITurnManager>();
        Set(aiManager, "_turnQueue", queue);
        var singleton = typeof(AITurnManager).GetField("<Instance>k__BackingField",
            BindingFlags.Static | BindingFlags.NonPublic);
        var previousInstance = singleton.GetValue(null);
        try
        {
            singleton.SetValue(null, aiManager);
            var game = root.AddComponent<GameManager>();
            var player = root.AddComponent<PlayerToken>();
            var controller = root.AddComponent<PathController>();
            controller.SetTokenObject(CreateRoot(objects));
            Set(player, "pathController", controller);
            Set(game, "playerToken", player);
            player.OnStepCompleted += () => trace.Add("player.step");
            player.OnArrivedAtCity += _ => trace.Add("player.city");
            var oldPath = CreatePath(objects, city, 2);
            var newPath = CreatePath(objects, city, 2);
            Invoke(game, "StartTurn");
            game.OnPathSelected(oldPath);
            Require(game.TurnNumber == 1 && game.State == GameState.DrawingCard,
                "Первый личный ход должен привести на первую клетку.");
            trace.Clear();
            Drain((IEnumerator)Invoke(game, "WaitForAIAndBeginPlayerTurn"));
            Require(string.Join(",", trace) ==
                "first.trade,first.move.start,first.move.end,second.trade,second.move.start,second.move.end,player.step",
                "Игрок должен двигаться после всех AI, каждый AI должен завершить движение до следующего.");
            Require(game.TurnNumber == 2 && game.State == GameState.DrawingCard,
                "Следующий личный ход должен привести на последнюю клетку старого пути.");
            trace.Clear();
            Drain((IEnumerator)Invoke(game, "WaitForAIAndBeginPlayerTurn"));
            Require(trace[trace.Count - 1] == "player.city" && trace.Count == 7 &&
                game.State == GameState.InCity && game.TurnNumber == 3,
                "Вход в город должен происходить после AI в начале третьего личного хода.");
            game.OnPathSelected(newPath);
            Require(game.State == GameState.DrawingCard && game.TurnNumber == 3 &&
                trace[trace.Count - 1] == "player.step" && controller.CurrentPath == newPath,
                "Выход на первую клетку нового пути должен продолжать тот же ход с городом.");

            var trader = root.AddComponent<AITrader>();
            trader.Initialize(Create<TraderProfile>(objects), city, economy);
            var aiController = (PathController)typeof(AITrader).GetField("_pathController",
                BindingFlags.NonPublic | BindingFlags.Instance).GetValue(trader);
            aiController.SetTokenObject(CreateRoot(objects));
            aiController.SetPath(oldPath);
            aiController.Step();
            aiController.Step();
            var stateField = typeof(AITrader).GetField("_state", BindingFlags.NonPublic | BindingFlags.Instance);
            stateField.SetValue(trader, Enum.Parse(stateField.FieldType, "OnPath"));
            trader.PrepareTurn();
            Require(trader.CurrentCity == city && !aiController.HasActivePath,
                "AI должен бесплатно войти с последней клетки в город перед планированием нового шага.");
        }
        finally
        {
            singleton.SetValue(null, previousInstance);
        }
    }

    private static PathCellInitializer CreatePath(List<UnityEngine.Object> objects, City city, int count)
    {
        var root = CreateRoot(objects);
        var path = root.AddComponent<PathCellInitializer>();
        Set(path, "finishCity", city);
        for (int i = 0; i < count; i++)
        {
            var cell = new GameObject("Cell");
            cell.transform.SetParent(root.transform);
            cell.AddComponent<Cell>();
        }
        return path;
    }

    private static object Invoke(object target, string method) => target.GetType()
        .GetMethod(method, BindingFlags.NonPublic | BindingFlags.Instance).Invoke(target, null);

    private static void Drain(IEnumerator routine)
    {
        var stack = new Stack<IEnumerator>();
        stack.Push(routine);
        int iterations = 0;
        while (stack.Count > 0)
        {
            Require(++iterations < 10000, "Корутина не завершилась.");
            var current = stack.Peek();
            if (!current.MoveNext())
            {
                (current as IDisposable)?.Dispose();
                stack.Pop();
            }
            else if (current.Current is IEnumerator nested) stack.Push(nested);
            else System.Threading.Thread.Sleep(1);
        }
    }

    private sealed class CheckTrader : ITrader
    {
        public string DisplayName { get; }
        public int Initiative { get; }
        public int Gold => 100;
        public City CurrentCity { get; }
        private readonly List<string> _trace;
        public event Action<ITrader, City> OnArrivedAtCity { add { } remove { } }
        public event Action<ITrader, PathCellInitializer> OnPathBlocked { add { } remove { } }

        public CheckTrader(string name, int initiative, City city, List<string> trace)
        {
            DisplayName = name;
            Initiative = initiative;
            CurrentCity = city;
            _trace = trace;
        }
        public TurnIntent PlanTurn(GameSnapshot snapshot) => new TurnIntent { Trader = this };
        public void ExecuteTrade(TurnIntent intent, WorldEconomy economy) => _trace.Add(DisplayName + ".trade");
        public IEnumerator ExecuteMovement(TurnIntent intent)
        {
            _trace.Add(DisplayName + ".move.start");
            yield return null;
            _trace.Add(DisplayName + ".move.end");
        }
    }
    private static T Create<T>(List<UnityEngine.Object> objects) where T : ScriptableObject
    {
        var obj = ScriptableObject.CreateInstance<T>();
        objects.Add(obj);
        return obj;
    }

    private static GameObject CreateRoot(List<UnityEngine.Object> objects)
    {
        var root = new GameObject("CardSystemChecks");
        root.SetActive(false);
        objects.Add(root);
        return root;
    }

    private static void Set(object target, string field, object value)
        => target.GetType().GetField(field, BindingFlags.NonPublic | BindingFlags.Instance).SetValue(target, value);

    private static void Require(bool condition, string message)
    {
        if (!condition) throw new InvalidOperationException(message);
    }
}
