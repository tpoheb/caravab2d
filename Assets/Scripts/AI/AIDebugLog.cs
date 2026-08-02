using System;
using System.Collections.Generic;

/// <summary>
/// Статическая шина событий для дебажного оверлея ИИ.
/// Никаких зависимостей от Unity — можно вызывать из любого потока.
///
/// Использование:
///   AIDebugLog.RecordTrade(...)   — из WorldEconomy.Buy / Sell
///   AIDebugLog.RecordMove(...)    — из TurnQueue после AssignPaths
///   AIDebugLog.RecordFinance(...) — из TurnQueue до и после хода
///   AIDebugLog.NewTurn(n)         — из TurnQueue.ProcessTurnCoroutine
/// </summary>
public static class AIDebugLog
{
    // ------------------------------------------------------------------
    // Типы записей
    // ------------------------------------------------------------------

    public enum TradeType { Buy, Sell }

    public readonly struct TradeEntry
    {
        public readonly string    TraderName;
        public readonly TradeType Kind;
        public readonly string    ItemName;
        public readonly int       Amount;
        public readonly int       TotalCost;
        public readonly string    CityName;

        public TradeEntry(string trader, TradeType kind, string item, int amount, int cost, string city)
        {
            TraderName = trader;
            Kind       = kind;
            ItemName   = item;
            Amount     = amount;
            TotalCost  = cost;
            CityName   = city;
        }

        public override string ToString()
        {
            string arrow = Kind == TradeType.Buy ? "←" : "→";
            string verb  = Kind == TradeType.Buy ? "Куп" : "Прод";
            return $"[{verb}] {TraderName} {arrow} {Amount}x {ItemName} @ {CityName} ({TotalCost}g)";
        }
    }

    public readonly struct MoveEntry
    {
        public readonly string TraderName;
        public readonly string PathName;     // null = нет пути
        public readonly string Destination;  // название города-назначения

        public MoveEntry(string trader, string pathName, string destination)
        {
            TraderName  = trader;
            PathName    = pathName;
            Destination = destination;
        }

        public override string ToString()
        {
            if (PathName == null)
                return $"[Ход] {TraderName} — остался на месте";
            return $"[Ход] {TraderName} → {Destination} (путь: {PathName})";
        }
    }

    public readonly struct FinanceEntry
    {
        public readonly string TraderName;
        public readonly int    GoldBefore;
        public readonly int    GoldAfter;

        public FinanceEntry(string trader, int before, int after)
        {
            TraderName  = trader;
            GoldBefore  = before;
            GoldAfter   = after;
        }

        public override string ToString()
        {
            int delta = GoldAfter - GoldBefore;
            string sign = delta >= 0 ? "+" : "";
            return $"[Золото] {TraderName}: {GoldBefore}g → {GoldAfter}g ({sign}{delta}g)";
        }
    }

    // ------------------------------------------------------------------
    // Состояние
    // ------------------------------------------------------------------

    public static int CurrentTurn { get; private set; }

    // Все записи текущего хода (смешанный список строк для простоты отображения)
    private static readonly List<string> _entries = new List<string>();

    // ------------------------------------------------------------------
    // События для подписки из AIDebugOverlay
    // ------------------------------------------------------------------

    /// <summary>Вызывается при старте нового хода (entries уже очищены).</summary>
    public static event Action<int> OnNewTurn;

    /// <summary>Вызывается при каждой новой записи — строка готова к отображению.</summary>
    public static event Action<string> OnEntryAdded;

    // ------------------------------------------------------------------
    // API для вызова из игровых систем
    // ------------------------------------------------------------------

    /// <summary>Начало нового хода — очищает лог текущего хода.</summary>
    public static void NewTurn(int turnNumber)
    {
        CurrentTurn = turnNumber;
        _entries.Clear();
        OnNewTurn?.Invoke(turnNumber);
    }

    /// <summary>Торговая операция (покупка или продажа).</summary>
    public static void RecordTrade(string traderName, TradeType kind,
                                   string itemName, int amount, int totalCost, string cityName)
    {
        var entry = new TradeEntry(traderName, kind, itemName, amount, totalCost, cityName);
        AddEntry(entry.ToString());
    }

    /// <summary>Движение торговца.</summary>
    public static void RecordMove(string traderName, string pathName, string destination)
    {
        var entry = new MoveEntry(traderName, pathName, destination);
        AddEntry(entry.ToString());
    }

    /// <summary>Финансовый итог хода торговца.</summary>
    public static void RecordFinance(string traderName, int goldBefore, int goldAfter)
    {
        var entry = new FinanceEntry(traderName, goldBefore, goldAfter);
        AddEntry(entry.ToString());
    }

    /// <summary>Снимок всех записей текущего хода (копия).</summary>
    public static IReadOnlyList<string> GetCurrentEntries() => _entries;

    // ------------------------------------------------------------------
    // Внутренние
    // ------------------------------------------------------------------

    private static void AddEntry(string line)
    {
        _entries.Add(line);
        OnEntryAdded?.Invoke(line);
    }
}
