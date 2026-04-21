using System.Collections.Generic;

/// <summary>
/// Неизменяемый снимок состояния мира на момент начала хода.
/// Все торговцы планируют по одному снимку — гарантия честности.
/// Использует City напрямую — никаких координат.
/// </summary>
public class GameSnapshot
{
    /// <summary>Все города на карте с их текущими ценами и запасами.</summary>
    public IReadOnlyList<CitySnapshot> Cities { get; private set; }

    /// <summary>Состояния всех торговцев (позиция, золото).</summary>
    public IReadOnlyList<TraderSnapshot> Traders { get; private set; }

    /// <summary>Все доступные товары.</summary>
    public IReadOnlyList<string> AllGoods { get; private set; }

    public int TurnNumber { get; private set; }

    public GameSnapshot(
        IReadOnlyList<CitySnapshot>   cities,
        IReadOnlyList<TraderSnapshot> traders,
        IReadOnlyList<string>         allGoods,
        int                           turnNumber)
    {
        Cities     = cities;
        Traders    = traders;
        AllGoods   = allGoods;
        TurnNumber = turnNumber;
    }
}

/// <summary>
/// Снимок одного города: ссылка на City + цены и запасы товаров.
/// </summary>
public class CitySnapshot
{
    public City City { get; private set; }

    private readonly Dictionary<string, float> _prices;
    private readonly Dictionary<string, int>   _stocks;

    public CitySnapshot(City city,
        Dictionary<string, float> prices,
        Dictionary<string, int>   stocks)
    {
        City    = city;
        _prices = prices;
        _stocks = stocks;
    }

    public float GetPrice(string goodId) =>
        _prices.TryGetValue(goodId, out var p) ? p : 0f;

    public int GetStock(string goodId) =>
        _stocks.TryGetValue(goodId, out var s) ? s : 0;
}

/// <summary>
/// Снимок одного торговца: в каком городе находится и сколько золота.
/// </summary>
public class TraderSnapshot
{
    public ITrader Trader      { get; private set; }
    public City    CurrentCity { get; private set; }
    public int     Gold        { get; private set; }
    public int     Initiative  { get; private set; }

    public TraderSnapshot(ITrader trader, City currentCity, int gold, int initiative)
    {
        Trader      = trader;
        CurrentCity = currentCity;
        Gold        = gold;
        Initiative  = initiative;
    }
}