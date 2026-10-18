using System;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

/// <summary>Отложенные городские эффекты, неуспешные сделки и восстановление.</summary>
public static class CityEffectChecks
{
    public static void RunBatch()
    {
        try { Run(); DocumentCardChecks.Run(); GameMenuPlayModeChecks.RunBatch(); }
        catch (Exception exception)
        {
            Debug.LogException(exception);
            if (Application.isBatchMode) EditorApplication.Exit(1);
            else throw;
        }
    }

    [MenuItem("Tools/Проверки/Отложенные эффекты в городе")]
    public static void Run()
    {
        CheckScene("Assets/Scenes/TabletopCameraStudy.unity");
        CheckScene("Assets/Scenes/MapPrototipe.unity");
        using (var f = new Fixture())
        {
            f.Inventory.Money = 1000;
            f.Effect(ShadowEffectType.WagePenalty, 0);
            f.Effect(ShadowEffectType.BonusTrade, 15);
            f.Effect(ShadowEffectType.BonusTrade, 10);
            var pending = f.Save();
            f.Effects.RestoreSave(JsonUtility.FromJson<CaravanSaveData>(JsonUtility.ToJson(pending)));
            f.Arrive();
            Require(f.Inventory.Money == 970 && !f.Save().wagePenalty, "Одна доплата жалованья при прибытии; флаг погашен после загрузки.");
            Require(f.Save().bonusTrade == 0 && Mathf.Approximately(f.Modifiers.PendingSalePriceBoost, .25f), "Проценты суммируются и переходят в следующую продажу.");
            Invoke(f.Manager, "HandleTradeClosed");
            f.Manager.RestoreCity(f.City, true);
            Require(f.Inventory.Money == 970 && Mathf.Approximately(f.Modifiers.PendingSalePriceBoost, .25f), "Переоткрытие и восстановление панели не повторяют эффекты.");
            var arrived = f.Save();
            f.Modifiers.RestoreSave(JsonUtility.FromJson<CaravanSaveData>(JsonUtility.ToJson(arrived)));
            f.Effects.RestoreSave(arrived); f.Manager.RestoreCity(f.City, true);
            Require(f.Inventory.Money == 970 && Mathf.Approximately(f.Modifiers.PendingSalePriceBoost, .25f), "Бонус сохраняется после прибытия до успешной продажи.");
            Require(!f.Sell(1, out _) && !f.Sell(0, out _) && !f.Sell(-1, out _), "Нельзя продавать отсутствующий товар или неверное количество.");
            Require(!TradeTransactionHandler.ProcessSellTransaction(null, 1, f.Market, f.Inventory, f.Stats, out _), "Null-запрос отклоняется.");
            Require(f.Inventory.AddItem(f.Item, 3), "Товар добавлен для проверки продаж.");
            f.Market.cityGold = 110;
            Require(!f.Sell(1, out _) && Mathf.Approximately(f.Modifiers.PendingSalePriceBoost, .25f), "Недостаток денег у города не расходует бонус.");
            f.Market.cityGold = 10000;
            Require(f.Buy(1, out var cost) && cost == 100 && Mathf.Approximately(f.Modifiers.PendingSalePriceBoost, .25f), "Покупка сохраняет бонус продажи.");
            int money = f.Inventory.Money, gold = f.Market.cityGold, stock = f.Row.stock;
            Require(f.Sell(1, out var revenue) && revenue == 125 && f.Inventory.Money == money + revenue &&
                f.Market.cityGold == gold - revenue && f.Row.stock == stock + 1 && f.Modifiers.PendingSalePriceBoost == 0,
                "Бонус применяется к одной успешной продаже; обе стороны получают правильный итог.");
            Require(f.Sell(1, out revenue) && revenue == 100, "Следующая продажа без бонуса.");
            f.Arrive(); Require(f.Inventory.Money == money + 225, "Следующий въезд не повторяет доплату.");

            Set(f.Modifiers, "_pendingSaleBonus", 50);
            f.Market.cityGold = 120;
            Require(!f.Sell(1, out _) && f.Modifiers.PendingSaleBonus == 50, "Городу нужны деньги и на фиксированный бонус.");
            f.Market.cityGold = 10000;
            Require(f.Sell(1, out revenue) && revenue == 150 && f.Modifiers.PendingSaleBonus == 0, "Итог фиксированного бонуса входит в результат сделки.");
            Set(f.Modifiers, "_pendingPurchaseDiscount", .2f); f.Inventory.Money = 0;
            Require(!f.Buy(1, out _) && Mathf.Approximately(f.Modifiers.PendingPurchaseDiscount, .2f), "Неудачная покупка сохраняет скидку.");
            f.Inventory.Money = 1000;
            Require(f.Buy(1, out cost) && cost == 80 && f.Modifiers.PendingPurchaseDiscount == 0, "Успешная покупка использует скидку один раз.");
            Require(f.Row.currentBuyPrice == 100 && f.Row.currentSellPrice == 100, "Одноразовые бонусы не меняют базовые рыночные цены.");

            f.Inventory.Money = 5; f.Effect(ShadowEffectType.WagePenalty, 0); f.Arrive();
            Require(f.Inventory.Money == 0 && !f.Save().wagePenalty, "При нехватке денег доплата ограничена кошельком и не создаёт долг.");
            f.Team.CurrentTeam.Clear(); f.Inventory.Money = 100; f.Effect(ShadowEffectType.WagePenalty, 0); f.Arrive();
            Require(f.Inventory.Money == 100 && !f.Save().wagePenalty, "Пустая команда не получает доплату; флаг расходуется.");
        }
        Debug.Log("CITY_EFFECT_CHECKS_PASSED");
    }

    private static void CheckScene(string path)
    {
        var scene = EditorSceneManager.OpenPreviewScene(path);
        try
        {
            var manager = scene.GetRootGameObjects().SelectMany(root => root.GetComponentsInChildren<CityManager>(true)).Single();
            foreach (var field in new[] { "_effectManager", "_teamSystem", "_tradeModifiers" })
            {
                var component = (Component)Get(manager, field);
                Require(component != null && component.gameObject.scene == scene, "Зависимость города: " + field + " / " + path);
            }
            Require(scene.GetRootGameObjects().SelectMany(root => root.GetComponentsInChildren<TradeCardModifiers>(true)).Count() == 1,
                "В сцене один торговый менеджер: " + path);
        }
        finally { EditorSceneManager.ClosePreviewScene(scene); }
    }

    private sealed class Fixture : IDisposable
    {
        private readonly GameObject root;
        private readonly object oldModifiers;
        public PlayerInventory Inventory; public PlayerStats Stats; public ShadowEffectManager Effects;
        public CityManager Manager; public TeamSystem Team; public TradeCardModifiers Modifiers;
        public City City; public CityData Market; public Item Item; public UnitData Unit; public CityData.CityItem Row;
        private readonly ShadowCardData card;
        public Fixture()
        {
            root = new GameObject("CityEffectChecks"); root.SetActive(false);
            Inventory = root.AddComponent<PlayerInventory>(); Effects = root.AddComponent<ShadowEffectManager>();
            Team = root.AddComponent<TeamSystem>(); Manager = root.AddComponent<CityManager>();
            Modifiers = root.AddComponent<TradeCardModifiers>(); City = root.AddComponent<City>();
            oldModifiers = Singleton.GetValue(null); Singleton.SetValue(null, Modifiers);
            Stats = ScriptableObject.CreateInstance<PlayerStats>(); Stats.Initialize(); Stats.ModifyBargain(-Stats.Bargain);
            Set(Inventory, "playerStats", Stats); Set(Team, "playerInventory", Inventory); Set(Team, "playerStats", Stats);
            Unit = ScriptableObject.CreateInstance<UnitData>(); Unit.salaryPerTurn = 30; Team.CurrentTeam.Add(new TeamMember(Unit));
            Market = ScriptableObject.CreateInstance<CityData>(); Market.cityName = "Проверка города"; Market.cityGold = 10000; City.RuntimeData = Market;
            Item = ScriptableObject.CreateInstance<Item>(); Item.weight = 1;
            Row = new CityData.CityItem { item = Item, stock = 10, baseBuyPrice = 100, currentBuyPrice = 100,
                currentSellPrice = 100, minBuyPrice = 100, maxBuyPrice = 100, minSellPrice = 100, maxSellPrice = 100, volatility = 0 };
            Market.items.Add(Row);
            var panelObject = new GameObject("Panel"); panelObject.transform.SetParent(root.transform); panelObject.SetActive(false);
            var panel = panelObject.AddComponent<CityPanel>();
            var buttonObject = new GameObject("Button", typeof(RectTransform), typeof(Button)); buttonObject.transform.SetParent(root.transform);
            Set(panel, "hireTeamButton", buttonObject.GetComponent<Button>()); Set(panel, "buyGoodsButton", buttonObject.GetComponent<Button>());
            Set(Manager, "cityPanel", panel); Set(Manager, "_effectManager", Effects); Set(Manager, "_teamSystem", Team); Set(Manager, "_tradeModifiers", Modifiers);
            card = ScriptableObject.CreateInstance<ShadowCardData>();
        }
        public void Effect(ShadowEffectType type, int value) { card.effectType = type; card.value = value; Effects.ApplyCard(card); }
        public void Arrive() => Invoke(Manager, "HandlePlayerArrivedAtCity", City);
        public CaravanSaveData Save() { var data = new CaravanSaveData(); Effects.CaptureSave(data); Modifiers.CaptureSave(data); return data; }
        public bool Sell(int quantity, out int value) => TradeTransactionHandler.ProcessSellTransaction(Row, quantity, Market, Inventory, Stats, out value);
        public bool Buy(int quantity, out int value) => TradeTransactionHandler.ProcessBuyTransaction(Row, quantity, Market, Inventory, Stats, out value);
        public void Dispose()
        {
            Singleton.SetValue(null, oldModifiers);
            UnityEngine.Object.DestroyImmediate(root);
            foreach (var asset in new UnityEngine.Object[] { Stats, Market, Item, Unit, card }) UnityEngine.Object.DestroyImmediate(asset);
        }
    }
    private static FieldInfo Singleton => typeof(TradeCardModifiers).GetField("<Instance>k__BackingField", BindingFlags.Static | BindingFlags.NonPublic);
    private static object Get(object target, string field) => target.GetType().GetField(field, BindingFlags.Instance | BindingFlags.NonPublic).GetValue(target);
    private static void Set(object target, string field, object value) => target.GetType().GetField(field, BindingFlags.Instance | BindingFlags.NonPublic).SetValue(target, value);
    private static object Invoke(object target, string method, params object[] args) => target.GetType().GetMethod(method, BindingFlags.Instance | BindingFlags.NonPublic).Invoke(target, args);
    private static void Require(bool value, string message) { if (!value) throw new InvalidOperationException(message); }
}
