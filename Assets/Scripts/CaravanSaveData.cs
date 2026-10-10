using System;
using System.Collections.Generic;
using UnityEngine;

// Сохранение содержит только данные; ссылки Unity восстанавливаются через каталог и сцену.
[Serializable] public sealed class CaravanSaveData
{
    public int version = 1;
    public string savedAt;
    public int turn;
    public GameState phase;
    public bool pendingBattle, choosingDice;
    public int selectedDice;
    public string city;
    public RouteSave playerRoute = new RouteSave();
    public Vector3 playerPosition;
    public bool playerVisible;
    public int money;
    public List<GoodsSave> goods = new List<GoodsSave>();
    public List<string> crew = new List<string>();
    public string guild, statsAsset;
    public int attack, bargain, capacity;
    public List<int> multipliers = new List<int>();
    public List<string> hand = new List<string>();
    public float penaltyReduction;
    public List<string> deck = new List<string>(), discard = new List<string>();
    public string currentCard;
    public bool eventResolved;
    public BattleSave battle = new BattleSave();
    public List<EffectSave> effects = new List<EffectSave>();
    public bool wagePenalty, ignoreTax;
    public float bonusTrade, saleBoost, purchaseDiscount;
    public int saleBonus;
    public List<MarketSave> markets = new List<MarketSave>();
    public List<TraderSave> traders = new List<TraderSave>();
    public int aiTurn, economyTurn;
    public UnityEngine.Random.State randomState;
    public StorySave story = new StorySave();
    public List<JournalEntryData> journal = new List<JournalEntryData>();
}
[Serializable] public sealed class RouteSave { public string path; public int cell = -1; }
[Serializable] public sealed class GoodsSave { public string id; public int quantity; public float averagePrice; }
[Serializable] public sealed class MarketSave
{
    public string city;
    public int gold;
    public List<MarketItemSave> items = new List<MarketItemSave>();
}
[Serializable] public sealed class MarketItemSave { public string id; public int stock; public float buy, sell; }
[Serializable] public sealed class TraderSave
{
    public string profile, city;
    public int gold;
    public RouteSave route = new RouteSave();
    public Vector3 position;
    public bool visible;
    public List<GoodsSave> goods = new List<GoodsSave>();
}
[Serializable] public sealed class BattleSave { public string card; public int dice, bonus, debuff; public bool resolved, victory; }
[Serializable] public sealed class EffectSave { public ShadowEffectType type; public int value, remaining; public string title; }
[Serializable] public sealed class StorySave
{
    public string city, current;
    public List<string> collected = new List<string>(), hired = new List<string>();
    public List<string> shown = new List<string>(), pending = new List<string>();
    public List<StorySystem.JournalEntryData> entries = new List<StorySystem.JournalEntryData>();
}
