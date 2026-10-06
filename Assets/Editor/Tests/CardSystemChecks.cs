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
            CheckEncounterPhases(objects);
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

    private static void CheckEncounterPhases(List<UnityEngine.Object> objects)
    {
        var root = CreateRoot(objects);
        var game = root.AddComponent<GameManager>();
        var cards = root.AddComponent<CardManager>();
        var battle = root.AddComponent<BattleManager>();
        var hand = root.AddComponent<HandManager>();
        var effects = root.AddComponent<ShadowEffectManager>();
        var inventory = root.AddComponent<PlayerInventory>();
        var team = root.AddComponent<TeamSystem>();
        var stats = Create<PlayerStats>(objects);
        stats.Initialize();
        Set(inventory, "playerStats", stats);
        Set(team, "playerInventory", inventory);
        Set(team, "playerStats", stats);
        Set(effects, "playerInventory", inventory);
        Set(effects, "playerStats", stats);
        Set(game, "cardManager", cards);
        Set(game, "battleManager", battle);
        Set(game, "handManager", hand);
        Set(game, "shadowEffectManager", effects);
        Set(game, "diceSystem", root.AddComponent<DiceSystem>());
        Set(battle, "teamSystem", team);
        var battleUI = root.AddComponent<BattleUIManager>();
        Set(battle, "uiManager", battleUI);
        var continueObject = new GameObject("Продолжить", typeof(RectTransform), typeof(UnityEngine.UI.Button));
        continueObject.transform.SetParent(root.transform, false);
        var continueButton = continueObject.GetComponent<UnityEngine.UI.Button>();
        var labelObject = new GameObject("Подпись", typeof(RectTransform), typeof(TMPro.TextMeshProUGUI));
        labelObject.transform.SetParent(continueObject.transform, false);
        var continueLabel = labelObject.GetComponent<TMPro.TextMeshProUGUI>();
        Set(battleUI, "endTurnButton", continueButton);
        var drawObject = new GameObject("Вытянуть карту", typeof(RectTransform), typeof(UnityEngine.UI.Button));
        drawObject.transform.SetParent(root.transform, false);
        var sharedDrawButton = drawObject.GetComponent<UnityEngine.UI.Button>();
        Set(game, "drawCardButton", sharedDrawButton);
        Set(battleUI, "diceButton", sharedDrawButton);
        Set(hand, "battleManager", battle);
        Set(hand, "effectManager", effects);
        Set(hand, "playerInventory", inventory);
        Set(cards, "gameManager", game);
        Set(cards, "battleManager", battle);
        Set(cards, "effectManager", effects);

        var gameInstance = typeof(GameManager).GetField("<Instance>k__BackingField", BindingFlags.Static | BindingFlags.NonPublic);
        var handInstance = typeof(HandManager).GetField("<Instance>k__BackingField", BindingFlags.Static | BindingFlags.NonPublic);
        var cardInstance = typeof(CardManager).GetField("<Instance>k__BackingField", BindingFlags.Static | BindingFlags.NonPublic);
        var oldGame = gameInstance.GetValue(null);
        var oldHand = handInstance.GetValue(null);
        var oldCards = cardInstance.GetValue(null);
        try
        {
            gameInstance.SetValue(null, game);
            handInstance.SetValue(null, hand);
            cardInstance.SetValue(null, cards);
            Invoke(game, "Subscribe");
            var bonus = Create<HandCardData>(objects);
            bonus.effectType = HandCardData.CardEffectType.AddBonus;
            bonus.value = 3;
            bonus.playTiming = HandCardData.PlayTiming.BeforeBattle;
            var debuff = Create<HandCardData>(objects);
            debuff.effectType = HandCardData.CardEffectType.EnemyAttackDebuff;
            debuff.value = 4;
            debuff.playTiming = HandCardData.PlayTiming.DuringBattle;
            var reroll = Create<HandCardData>(objects);
            reroll.effectType = HandCardData.CardEffectType.Reroll;
            reroll.playTiming = HandCardData.PlayTiming.DuringBattle;
            var oath = Create<HandCardData>(objects);
            oath.effectType = HandCardData.CardEffectType.BattlePenaltyReduce;
            oath.value = 40;
            oath.playTiming = HandCardData.PlayTiming.AfterBattle;
            var reward = Create<HandCardData>(objects);
            reward.effectType = HandCardData.CardEffectType.GoldBoost;
            reward.playTiming = HandCardData.PlayTiming.AfterBattle;
            Set(hand, "rewardPool", new List<HandCardData> { reward });
            var enemy = Create<BattleCardData>(objects);
            enemy.requiredAttack = 100;
            enemy.penaltyMoney = -100;
            enemy.rewardMoney = 50;
            Set(cards, "allBattleCards", new List<BattleCardData> { enemy });
            Invoke(cards, "RebuildDeck");
            Invoke(game, "StartTurn");
            inventory.Money = 1000;
            hand.AddCard(bonus);
            hand.AddCard(debuff);
            hand.AddCard(reroll);
            hand.AddCard(oath);
            typeof(GameManager).GetMethod("SetState", BindingFlags.NonPublic | BindingFlags.Instance)
                .Invoke(game, new object[] { GameState.DrawingCard });
            Require(sharedDrawButton.gameObject.activeSelf,
                "Общая со старым кубиком кнопка вытягивания должна оставаться видимой в DrawingCard.");
            Invoke(game, "OnDrawCardButtonPressed");
            Require(!sharedDrawButton.gameObject.activeSelf, "Кнопка вытягивания скрывается во время встречи.");
            Require(continueButton.gameObject.activeSelf && continueLabel.text == "Начать битву",
                "Раскрытие должно показывать кнопку начала подготовки к битве.");
            var handPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/HandCard.prefab");
            Require(handPrefab != null, "Игровой префаб руки должен существовать.");
            var handView = UnityEngine.Object.Instantiate(handPrefab, root.transform);
            var slot = handView.GetComponent<CardSlotUI>();
            slot.Setup(bonus, 0);
            var useButton = (UnityEngine.UI.Button)typeof(CardSlotUI).GetField("useButton",
                BindingFlags.NonPublic | BindingFlags.Instance).GetValue(slot);
            Require(useButton != null && useButton.interactable, "Подходящая карта должна быть доступна в префабе руки.");
            Require(game.State == GameState.PreparingEncounter && !battle.HasDiceRoll,
                "Раскрытие боя должно остановиться на подготовке без броска.");
            Require(hand.CanUseCard(0) && !hand.CanUseCard(1) && !hand.CanUseCard(2) && !hand.CanUseCard(3),
                "Подготовка допускает только метку перед битвой.");
            hand.UseCard(1);
            Require(HandCount(hand) == 4, "Запрещённая карта не расходуется.");
            hand.UseCard(0);
            game.AdvancePhase();
            Require(game.State == GameState.InBattle && battle.HasDiceRoll && game.TurnNumber == 1,
                "После подготовки бой остаётся открытым для карт во время битвы.");
            hand.UseCard(0);
            hand.UseCard(0);
            Require((int)typeof(BattleManager).GetField("_enemyDebuff", BindingFlags.NonPublic | BindingFlags.Instance)
                .GetValue(battle) == 4, "Переброс не отменяет ослабление врага.");
            slot.Setup(oath, 0);
            Require(!useButton.interactable && continueLabel.text == "Завершить бой",
                "Во время боя карта после битвы должна быть выключена, а кнопка — завершать бой.");
            Require(!hand.CanUseCard(0), "Клятва Пути недоступна до результата боя.");
            game.AdvancePhase();
            Require(game.State == GameState.PostEncounter && battle.IsDefeat && inventory.Money == 1000,
                "Поражение открывает посткомбат до списания штрафа.");
            slot.Setup(oath, 0);
            Require(useButton.interactable && continueLabel.text == "Завершить ход",
                "Посткомбат должен включать карту после поражения и кнопку завершения хода.");
            hand.UseCard(0);
            Require(HandCount(hand) == 0, "Карта после поражения должна расходоваться.");
            Invoke(game, "CompleteEncounter");
            Require(inventory.Money == 940 && game.State == GameState.EndingTurn && game.TurnNumber == 1,
                "После посткомбата штраф 100 должен уменьшиться до 60.");
            battle.SettleBattle();
            Require(inventory.Money == 940, "Повторный расчёт не списывает штраф.");
            cards.HideEventCard();

            // Проверяем устойчивость ослабления и бонуса на исходе, а не только в полях.
            enemy.requiredAttack = team.GetTotalAttack() + 10;
            cards.DrawCard();
            hand.AddCard(bonus);
            hand.UseCard(0);
            game.AdvancePhase();
            hand.AddCard(debuff);
            hand.UseCard(0);
            game.OnDiceRolled(3);
            game.AdvancePhase();
            Require(!battle.IsDefeat && inventory.Money == 990,
                "Бонус подготовки +3 и ослабление -4 должны сохраниться до победы.");
            battle.FinalizeBattle();
            Require(inventory.Money == 990, "Повторная финализация не выдаёт награду.");
            hand.AddCard(oath);
            Require(!hand.CanUseCard(1), "Снижение штрафа нельзя тратить после победы.");
            Invoke(game, "CompleteEncounter");
            Set(hand, "currentHand", new List<HandCardData>());
            cards.HideEventCard();

            var shadow = Create<ShadowCardData>(objects);
            shadow.effectType = ShadowEffectType.Money;
            shadow.value = 20;
            Set(cards, "allBattleCards", new List<BattleCardData>());
            Set(cards, "allShadowCards", new List<ShadowCardData> { shadow });
            Invoke(cards, "RebuildDeck");
            cards.DrawCard();
            Require(game.State == GameState.PreparingEncounter && inventory.Money == 990,
                "Событие не применяется на раскрытии.");
            game.OnDiceRolled(6);
            Require(game.State == GameState.PreparingEncounter, "Посторонний бросок не пропускает подготовку события.");
            game.AdvancePhase();
            Require(game.State == GameState.ResolvingEvent && inventory.Money == 990,
                "Окно во время события должно предшествовать эффекту.");
            var eventCard = Create<HandCardData>(objects);
            eventCard.effectType = HandCardData.CardEffectType.GoldBoost;
            eventCard.playTiming = HandCardData.PlayTiming.DuringEvent;
            hand.AddCard(eventCard);
            Require(hand.CanUseCard(0), "Метка во время события разрешена в ResolvingEvent.");
            hand.UseCard(0);
            game.CompleteEventPhase();
            Require(game.State == GameState.PostEncounter && inventory.Money == 1010,
                "Применение события должно открыть постфазу.");
            cards.ResolveCurrentEvent();
            Require(inventory.Money == 1010, "Событие применяется один раз.");
            eventCard.playTiming = HandCardData.PlayTiming.AfterEvent;
            hand.AddCard(eventCard);
            Require(hand.CanUseCard(0), "Метка после события разрешена в постфазе.");
            hand.UseCard(0);
            Invoke(game, "CompleteEncounter");
            cards.HideEventCard();

            var cancel = Create<HandCardData>(objects);
            cancel.effectType = HandCardData.CardEffectType.CancelCard;
            cancel.playTiming = HandCardData.PlayTiming.BeforeEvent | HandCardData.PlayTiming.DuringEvent;
            for (int i = 0; i < 2; i++)
            {
                cards.DrawCard();
                if (i == 1) game.AdvancePhase();
                hand.AddCard(cancel);
                hand.UseCard(0);
                Require(game.State == GameState.PostEncounter && inventory.Money == 1010,
                    "Отмена в подготовке или событии должна открыть постфазу без эффекта.");
                cards.ResolveCurrentEvent();
                Require(inventory.Money == 1010, "Отменённое событие нельзя применить прямым вызовом.");
                Invoke(game, "CompleteEncounter");
                cards.HideEventCard();
            }

            Set(cards, "allShadowCards", new List<ShadowCardData>());
            Set(cards, "allBattleCards", new List<BattleCardData> { enemy });
            Invoke(cards, "RebuildDeck");
            var escape = Create<HandCardData>(objects);
            escape.effectType = HandCardData.CardEffectType.EscapeBattle;
            escape.playTiming = HandCardData.PlayTiming.BeforeBattle | HandCardData.PlayTiming.DuringBattle;
            for (int i = 0; i < 2; i++)
            {
                cards.DrawCard();
                if (i == 1) game.AdvancePhase();
                hand.AddCard(escape);
                hand.UseCard(0);
                Require(game.State == GameState.PostEncounter && !battle.IsActive && inventory.Money == 1010,
                    "Побег до или во время битвы должен открыть посткомбат без выплат.");
                Invoke(game, "CompleteEncounter");
                cards.HideEventCard();
            }

            var choose = Create<HandCardData>(objects);
            choose.effectType = HandCardData.CardEffectType.ChooseDice;
            choose.playTiming = HandCardData.PlayTiming.BeforeBattle | HandCardData.PlayTiming.DuringBattle;
            cards.DrawCard();
            hand.AddCard(choose);
            hand.UseCard(0);
            Require(game.State == GameState.PreparingEncounter && !battle.HasDiceRoll,
                "Выбор кубика до боя не завершает подготовку.");
            game.AdvancePhase();
            Require((int)typeof(BattleManager).GetField("_lastDiceRoll", BindingFlags.NonPublic | BindingFlags.Instance)
                .GetValue(battle) == 6, "Выбранное число используется вместо случайного броска.");
            hand.AddCard(choose);
            var choicePanel = CreateRoot(objects);
            Set(game, "diceChoicePanel", choicePanel);
            hand.UseCard(0);
            Require(!game.CanAdvancePhase && game.IsChoosingDice && !continueButton.interactable,
                "Ожидающий выбор блокирует продолжение и кнопку UI.");
            game.AdvancePhase();
            Require(game.State == GameState.InBattle, "Нельзя завершить бой до выбора числа.");
            game.OnDiceChoiceSelected(2);
            Require(game.CanAdvancePhase && !game.IsChoosingDice, "Выбор числа разблокирует бой.");
            game.OnBattleForceEnded();
            battle.CancelBattle();
            Invoke(game, "CompleteEncounter");
            cards.HideEventCard();
            cards.DrawCard(() => false);
            Require(game.State == GameState.PostEncounter && cards.CurrentCard == null,
                "Несостоявшееся вытягивание разрешает завершение через постфазу.");
        }
        finally
        {
            Invoke(game, "Unsubscribe");
            gameInstance.SetValue(null, oldGame);
            handInstance.SetValue(null, oldHand);
            cardInstance.SetValue(null, oldCards);
        }
    }

    private static int HandCount(HandManager hand) =>
        ((List<HandCardData>)typeof(HandManager).GetField("currentHand", BindingFlags.NonPublic | BindingFlags.Instance)
            .GetValue(hand)).Count;

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
