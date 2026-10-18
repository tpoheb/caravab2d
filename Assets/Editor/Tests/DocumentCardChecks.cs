using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

/// <summary>Проверки состава документа, новых эффектов и сохранения их состояния.</summary>
public static class DocumentCardChecks
{
    public static void RunBatch() { Run(); GameMenuPlayModeChecks.RunBatch(); }

    [MenuItem("Tools/Проверки/Карты из документа")]
    public static void Run()
    {
        CheckAssets();
        CheckCardLayout();
        CheckScene("Assets/Scenes/TabletopCameraStudy.unity");
        CheckScene("Assets/Scenes/MapPrototipe.unity");
        using (var fixture = new Fixture())
        {
            CheckDuration(fixture);
            CheckBattle(fixture);
            CheckEvents(fixture);
            CheckAvailability(fixture);
        }
        Debug.Log("DOCUMENT_CARD_CHECKS_PASSED");
    }

    private static void CheckAssets()
    {
        var source = DocumentCardImporter.ReadSource();
        Require(source.cards.Length == 29 && source.actualCount == 33, "Из 33 описанных карт исключены четыре карты с отсутствующими механиками.");
        Require(source.cards.Count(card => card.group == "Event") == 10 &&
            source.cards.Count(card => card.group == "General") == 10 &&
            source.cards.Count(card => card.group == "Battle") == 9, "Состав набора после исключений пользователя.");
        var catalog = AssetDatabase.LoadAssetAtPath<SaveGameCatalog>(GameMenuSetup.CatalogPath);
        foreach (var definition in source.cards)
        {
            var asset = AssetDatabase.LoadAssetAtPath<ScriptableObject>(definition.path);
            Require(asset != null && !string.IsNullOrEmpty(catalog.Id(asset)), "Карта и её GUID доступны: " + definition.name);
            if (asset is ShadowCardData shadow)
            {
                Require(shadow.cardName == definition.name && shadow.description == definition.description &&
                    shadow.value == definition.value && shadow.duration == definition.duration &&
                    shadow.effectType == ShadowEffectType.Attack && shadow.isTemporary, "Данные события: " + definition.name);
            }
            else if (asset is HandCardData hand)
            {
                Require(hand.cardName == definition.name && hand.description == definition.description &&
                    hand.effectType.ToString() == definition.effect && hand.value == definition.value &&
                    (int)hand.playTiming == definition.timing && hand.group.ToString() == definition.group &&
                    hand.unavailableReason == definition.unavailableReason && hand.momentLabel == definition.moment,
                    "Данные руки: " + definition.name);
            }
            else throw new InvalidOperationException("Неверный тип карты: " + definition.name);
        }
    }

    private static void CheckCardLayout()
    {
        var root = new GameObject("DocumentCardLayout", typeof(RectTransform), typeof(Canvas));
        root.GetComponent<Canvas>().renderMode = RenderMode.ScreenSpaceOverlay;
        var prefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/HandCard.prefab");
        var view = UnityEngine.Object.Instantiate(prefab, root.transform);
        try
        {
            var slot = view.GetComponent<CardSlotUI>();
            foreach (var definition in DocumentCardImporter.ReadSource().cards.Where(card => card.group != "Event"))
            {
                slot.Setup(AssetDatabase.LoadAssetAtPath<HandCardData>(definition.path), 0);
                Canvas.ForceUpdateCanvases();
                var description = Get<TMPro.TextMeshProUGUI>(slot, "descriptionText");
                description.ForceMeshUpdate(true);
                Require(!description.isTextOverflowing, "Описание не выходит за границы карты: " + definition.name);
            }
        }
        finally { UnityEngine.Object.DestroyImmediate(root); }
    }

    private static void CheckScene(string path)
    {
        var scene = EditorSceneManager.OpenPreviewScene(path);
        try
        {
            var roots = scene.GetRootGameObjects();
            var cards = roots.SelectMany(root => root.GetComponentsInChildren<CardManager>(true)).Single();
            var hand = roots.SelectMany(root => root.GetComponentsInChildren<HandManager>(true)).Single();
            var source = DocumentCardImporter.ReadSource().cards;
            Require(Get<List<ShadowCardData>>(cards, "allShadowCards").Select(AssetDatabase.GetAssetPath)
                .SequenceEqual(source.Where(card => card.group == "Event").Select(card => card.path)), "События сцены: " + path);
            Require(Get<List<HandCardData>>(hand, "rewardPool").Select(AssetDatabase.GetAssetPath)
                .SequenceEqual(source.Where(card => card.group != "Event").Select(card => card.path)), "Награды сцены: " + path);
            Require(Get<List<BattleCardData>>(cards, "allBattleCards").Count == 20, "Противники сохранены: " + path);
            Require(Get<List<HandCardData>>(hand, "currentHand").Select(card => card.cardName)
                .OrderBy(name => name).SequenceEqual(new[] { "Боевой клич", "Переброс" }.OrderBy(name => name)), "Начальная рука: " + path);
        }
        finally { EditorSceneManager.ClosePreviewScene(scene); }
    }

    private static void CheckDuration(Fixture f)
    {
        foreach (var definition in DocumentCardImporter.ReadSource().cards.Where(card => card.group == "Event"))
        {
            f.Stats.Initialize();
            var card = AssetDatabase.LoadAssetAtPath<ShadowCardData>(definition.path);
            f.Effects.ApplyCard(card);
            Require(f.Stats.Attack == 10 + definition.value, "Сила события: " + definition.name);
            for (int turn = 1; turn < definition.duration; turn++) f.Effects.ProcessTurn();
            Require(f.Stats.Attack == 10 + definition.value, "Событие не истекает раньше срока.");
            f.Effects.ProcessTurn();
            Require(f.Stats.Attack == 10, "Атака восстановлена после срока: " + definition.name);
        }
    }

    private static void CheckBattle(Fixture f)
    {
        f.Stats.Initialize();
        var penalty = f.Event;
        penalty.cardName = "Проверочный штраф"; penalty.value = -20; penalty.duration = 15;
        f.Effects.ApplyCard(penalty);
        f.BeginBattle(30, 1);
        Require(f.Battle.TotalAttack == -9, "Штраф события действует в бою.");
        f.Play("Запасной клинок", GameState.InBattle);
        Require(f.Battle.TotalAttack == 11 && f.Stats.Attack == -10, "Клинок компенсирует штраф только в этом бою.");
        f.BeginBattle(30, 1);
        f.Play("Верная сталь", GameState.InBattle);
        Require(f.Battle.TotalAttack == 10, "Минимум равен базовой атаке без штрафов события.");
        Require(f.Effects.UndoAttackPenalty(penalty), "Очистка штрафа.");

        f.BeginBattle(14, 1);
        Require(f.Battle.IsCloseBattle, "Разница 3 разрешает тактический бонус.");
        f.Play("Решающий момент", GameState.InBattle);
        Require(f.Battle.TotalAttack == 15, "Бонус близкого боя +4.");
        f.BeginBattle(15, 1);
        Require(!f.Battle.IsCloseBattle, "Разница 4 запрещает близкий бой.");
        f.BeginBattle(100, 5);
        f.Play("Выверенный удар", GameState.InBattle);
        Require(f.Battle.TotalAttack == 16, "Кубик не превышает 6.");

        f.Inventory.Money = 1000;
        f.BeginBattle(100, 1);
        f.Play("На пределе", GameState.InBattle);
        f.Battle.FinalizeBattle();
        var saved = new CaravanSaveData(); f.Battle.CaptureSave(saved, f.Catalog);
        Require(saved.battle.penaltyIncreasePercent == 50, "Риск сохраняется.");
        f.Battle.CancelBattle(); f.Battle.RestoreSave(saved, f.Catalog); f.Battle.SettleBattle();
        Require(f.Inventory.Money == 850, "Поражение с риском списывает 150% штрафа после загрузки.");

        f.Inventory.Money = 1000; f.Enemy.retreatPenaltyMoney = 25;
        f.BeginBattle(100, 1); f.Play("На пределе", GameState.InBattle); f.Battle.FinalizeBattle();
        f.Play("Шаг назад", GameState.PostEncounter);
        Require(!f.Battle.CanRetry, "Отступление завершает повторный расчёт.");
        f.Battle.SettleBattle();
        Require(f.Inventory.Money == 975, "При отступлении действует только предусмотренный штраф отступления.");
        f.Enemy.retreatPenaltyMoney = 0;
        f.BeginBattle(100, 1); f.Battle.FinalizeBattle();
        f.Play("Шаг назад", GameState.PostEncounter); f.Battle.SettleBattle();
        Require(f.Inventory.Money == 975, "Если штраф отступления не предусмотрен, он не выдумывается.");

        f.Inventory.Money = 1000;
        f.BeginBattle(20, 1); f.Battle.FinalizeBattle();
        f.Play("Последний натиск", GameState.PostEncounter);
        Require(!f.Battle.IsDefeat && !f.Battle.CanRetry && f.Inventory.Money == 1050, "Повтор +10 превращает поражение в победу и выдаёт одну награду.");
        f.Battle.CaptureSave(saved, f.Catalog);
        f.Battle.CancelBattle(); f.Battle.RestoreSave(saved, f.Catalog);
        f.Battle.RetryBattle(10); f.Battle.SettleBattle(); f.Battle.SettleBattle();
        Require(f.Inventory.Money == 1050, "Загрузка и повторный вызов не дублируют награду.");
        f.BeginBattle(15, 1); f.Battle.FinalizeBattle();
        f.Play("Последний рубеж", GameState.PostEncounter);
        Require(!f.Battle.IsDefeat, "Последний рубеж пересчитывает с +5.");
        f.Battle.SettleBattle();
        f.BeginBattle(100, 1); f.Battle.FinalizeBattle();
        f.Play("Последний натиск", GameState.PostEncounter);
        Require(f.Battle.IsDefeat && !f.Battle.CanRetry, "Второго пересчёта нет даже после повторного поражения.");
        f.Battle.CancelBattle();
        f.BeginBattle(100, 1); f.Play("Дымовая завеса", GameState.InBattle);
        Require(!f.Battle.IsActive && !f.Battle.IsDefeat, "Дымовая завеса завершает бой без штрафа.");
    }

    private static void CheckEvents(Fixture f)
    {
        f.Stats.Initialize(); f.Event.cardName = "Штраф для отмены"; f.Event.value = -7; f.Event.duration = 7;
        Set(f.Cards, "allShadowCards", new List<ShadowCardData> { f.Event });
        Set(f.Cards, "allBattleCards", new List<BattleCardData>());
        Invoke(f.Cards, "RebuildDeck"); f.Phase(GameState.DrawingCard);
        int turn = f.Game.TurnNumber;
        f.Play("Старая Карта", GameState.DrawingCard);
        var data = new CaravanSaveData(); f.Cards.CaptureSave(data, f.Catalog);
        Require(data.previewCard == "event" && f.Cards.RemainingCards == 1 && f.Cards.CurrentCard == null &&
            f.Game.State == GameState.DrawingCard, "Предпросмотр не применяет и не удаляет карту.");
        f.Cards.RestoreSave(data, f.Catalog); f.Cards.DrawCard();
        Require(ReferenceEquals(f.Cards.CurrentCard, f.Event) && f.Cards.RemainingCards == 0, "После загрузки вытягивается просмотренная карта.");
        f.Game.AdvancePhase(); f.Game.AdvancePhase();
        Require(f.Stats.Attack == 3 && f.Game.State == GameState.PostEncounter, "Событие применяется после подтверждения.");
        f.Play("Не сегодня", GameState.PostEncounter);
        Require(f.Stats.Attack == 10 && !f.Cards.CanUndoEventPenalty && f.Cards.CurrentCard == f.Event, "Отменяется только штраф, один раз.");
        f.Cards.CaptureSave(data, f.Catalog); f.Cards.RestoreSave(data, f.Catalog);
        Require(!f.Cards.CanUndoEventPenalty, "Отмена штрафа сохраняется.");
        f.Cards.HideEventCard();

        var second = f.Create<ShadowCardData>(); second.cardName = "Следующее событие"; second.effectType = ShadowEffectType.Attack; second.value = 2; second.duration = 15;
        Set(f.Cards, "allShadowCards", new List<ShadowCardData> { f.Event, second });
        Invoke(f.Cards, "RebuildDeck"); f.Cards.DrawCard();
        var previous = f.Cards.CurrentCard;
        f.Play("На шаг впереди", GameState.PreparingEncounter);
        Require(f.Cards.CurrentCard != previous && f.Cards.DiscardCount == 1 && f.Game.TurnNumber == turn &&
            f.Game.State == GameState.PreparingEncounter, "Замена события не меняет клетку, ход или фазу подготовки.");
        f.Play("Дурное предчувствие", GameState.PreparingEncounter);
        Require(f.Game.State == GameState.PostEncounter && f.Stats.Attack == 10, "Отмена события не применяет эффект.");
        f.Cards.HideEventCard();
        // Отмена нового события не позволяет снять оставшийся эффект одноимённой старой карты.
        f.Effects.ApplyCard(f.Event);
        Set(f.Cards, "allShadowCards", new List<ShadowCardData> { f.Event });
        Invoke(f.Cards, "RebuildDeck"); f.Cards.DrawCard();
        f.Play("Дурное предчувствие", GameState.PreparingEncounter);
        Require(!f.Cards.CanUndoEventPenalty && f.Stats.Attack == 3, "Отмена события не снимает штраф предыдущей карты.");
        f.Effects.UndoAttackPenalty(f.Event); f.Cards.HideEventCard();
    }

    private static void CheckAvailability(Fixture f)
    {
        var cards = DocumentCardImporter.ReadSource().cards.Where(card => card.group != "Event")
            .Select(card => AssetDatabase.LoadAssetAtPath<HandCardData>(card.path)).ToList();
        Set(f.Hand, "rewardPool", cards);
        Require(cards.Count == 19 && cards.All(card => string.IsNullOrEmpty(card.unavailableReason) &&
            card.effectType != HandCardData.CardEffectType.Unsupported), "В наборе только 19 работающих карт руки.");
        foreach (var name in new[] { "Второй шанс", "Цена ошибки", "Ложный выпад", "Сломанная удача" })
        {
            Require(cards.All(card => card.cardName != name) && Card(name) == null,
                "Исключённая карта отсутствует в наборе и ассетах: " + name);
        }
        for (int i = 0; i < 150; i++)
        {
            Set(f.Hand, "currentHand", new List<HandCardData>()); f.Hand.GiveRandomReward();
            Require(string.IsNullOrEmpty(Get<List<HandCardData>>(f.Hand, "currentHand").Single().unavailableReason), "Недоступные карты не выдаются в награду.");
        }
    }

    private sealed class Fixture : IDisposable
    {
        private readonly List<UnityEngine.Object> objects = new List<UnityEngine.Object>();
        private readonly Dictionary<Type, object> instances = new Dictionary<Type, object>();
        private readonly UnityEngine.Random.State random = UnityEngine.Random.state;
        public GameManager Game; public CardManager Cards; public HandManager Hand; public BattleManager Battle;
        public ShadowEffectManager Effects; public PlayerStats Stats; public PlayerInventory Inventory;
        public BattleCardData Enemy; public ShadowCardData Event; public SaveGameCatalog Catalog;
        public Fixture()
        {
            var root = new GameObject("DocumentCardChecks"); root.SetActive(false); objects.Add(root);
            Game = root.AddComponent<GameManager>(); Cards = root.AddComponent<CardManager>();
            Hand = root.AddComponent<HandManager>(); Battle = root.AddComponent<BattleManager>();
            Effects = root.AddComponent<ShadowEffectManager>(); Inventory = root.AddComponent<PlayerInventory>();
            var team = root.AddComponent<TeamSystem>(); Stats = Create<PlayerStats>(); Stats.Initialize();
            var ui = root.AddComponent<BattleUIManager>();
            Set(team, "playerStats", Stats); Set(team, "playerInventory", Inventory);
            Set(Inventory, "playerStats", Stats); Set(Effects, "playerStats", Stats);
            Set(Battle, "teamSystem", team); Set(Battle, "effectManager", Effects); Set(Battle, "uiManager", ui);
            Set(Game, "cardManager", Cards); Set(Game, "battleManager", Battle); Set(Game, "handManager", Hand);
            Set(Game, "shadowEffectManager", Effects); Set(Game, "diceSystem", root.AddComponent<DiceSystem>());
            Set(Cards, "gameManager", Game); Set(Cards, "battleManager", Battle); Set(Cards, "effectManager", Effects);
            Set(Hand, "battleManager", Battle); Set(Hand, "effectManager", Effects); Set(Hand, "playerInventory", Inventory);
            foreach (var component in new MonoBehaviour[] { Game, Cards, Hand })
            {
                var field = Singleton(component.GetType()); instances[component.GetType()] = field.GetValue(null); field.SetValue(null, component);
            }
            Enemy = Create<BattleCardData>(); Enemy.rewardMoney = 50; Enemy.penaltyMoney = 100;
            Event = Create<ShadowCardData>(); Event.effectType = ShadowEffectType.Attack; Event.isTemporary = true;
            Catalog = Create<SaveGameCatalog>(); Catalog.Configure(new[] {
                new SaveGameCatalog.Entry { id = "enemy", asset = Enemy }, new SaveGameCatalog.Entry { id = "event", asset = Event } });
            Set(Hand, "rewardPool", new List<HandCardData> { Card("Переброс") });
            Invoke(Game, "StartTurn");
        }
        public T Create<T>() where T : ScriptableObject { var value = ScriptableObject.CreateInstance<T>(); objects.Add(value); return value; }
        public void Phase(GameState state) => Invoke(Game, "SetState", state);
        public void BeginBattle(int strength, int dice)
        {
            Enemy.requiredAttack = strength; Set(Cards, "_currentCard", Enemy);
            Battle.PrepareBattle(Enemy); Phase(GameState.InBattle); Battle.ExecuteBattle(dice);
        }
        public void Play(string name, GameState phase)
        {
            var card = Card(name); Set(Hand, "currentHand", new List<HandCardData> { card }); Phase(phase);
            Require(Hand.CanUseCard(0), "Карта должна быть доступна: " + name + " / " + phase);
            Hand.UseCard(0); Require(!Get<List<HandCardData>>(Hand, "currentHand").Contains(card), "Карта расходуется: " + name);
        }
        public void Dispose()
        {
            foreach (var entry in instances) Singleton(entry.Key).SetValue(null, entry.Value);
            for (int i = objects.Count - 1; i >= 0; i--) if (objects[i] != null) UnityEngine.Object.DestroyImmediate(objects[i]);
            UnityEngine.Random.state = random;
        }
    }
    private static HandCardData Card(string name) => AssetDatabase.LoadAssetAtPath<HandCardData>("Assets/Data/HandCard/" + name + ".asset");
    private static FieldInfo Singleton(Type type) => type.GetField("<Instance>k__BackingField", BindingFlags.Static | BindingFlags.NonPublic);
    private static T Get<T>(object target, string field) => (T)target.GetType().GetField(field, BindingFlags.Instance | BindingFlags.NonPublic).GetValue(target);
    private static void Set(object target, string field, object value) => target.GetType().GetField(field, BindingFlags.Instance | BindingFlags.NonPublic).SetValue(target, value);
    private static object Invoke(object target, string method, params object[] arguments) => target.GetType().GetMethod(method, BindingFlags.Instance | BindingFlags.NonPublic).Invoke(target, arguments);
    private static void Require(bool value, string message) { if (!value) throw new InvalidOperationException(message); }
}