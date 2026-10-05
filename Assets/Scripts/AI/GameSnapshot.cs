using System.Collections.Generic;

/// <summary>Копия рынка и доступных путей для планирования без обращения к Unity API.</summary>
public class GameSnapshot
{
    public IReadOnlyList<CitySnapshot> Cities { get; }
    public IReadOnlyList<TraderSnapshot> Traders { get; }
    public IReadOnlyList<string> AllGoods { get; }
    public int TurnNumber { get; }

    public GameSnapshot(IReadOnlyList<CitySnapshot> cities, IReadOnlyList<TraderSnapshot> traders,
        IReadOnlyList<string> allGoods, int turnNumber)
    {
        Cities = new List<CitySnapshot>(cities).AsReadOnly();
        Traders = new List<TraderSnapshot>(traders).AsReadOnly();
        AllGoods = new List<string>(allGoods).AsReadOnly();
        TurnNumber = turnNumber;
    }

    public CitySnapshot GetCity(City city)
    {
        foreach (var snapshot in Cities)
            if (ReferenceEquals(snapshot.City, city)) return snapshot;
        return null;
    }
}

public class CitySnapshot
{
    public City City { get; }
    public IReadOnlyList<PathCellInitializer> Paths { get; }
    private readonly Dictionary<string, float> _prices;
    private readonly Dictionary<string, float> _sellPrices;
    private readonly Dictionary<string, int> _stocks;

    public CitySnapshot(City city, Dictionary<string, float> prices, Dictionary<string, int> stocks,
        Dictionary<string, float> sellPrices = null, IReadOnlyList<PathCellInitializer> paths = null)
    {
        City = city;
        _prices = new Dictionary<string, float>(prices);
        _stocks = new Dictionary<string, int>(stocks);
        _sellPrices = sellPrices == null ? new Dictionary<string, float>()
            : new Dictionary<string, float>(sellPrices);
        Paths = paths == null ? new List<PathCellInitializer>().AsReadOnly()
            : new List<PathCellInitializer>(paths).AsReadOnly();
    }

    public float GetPrice(string goodId) => _prices.TryGetValue(goodId, out var price) ? price : 0f;
    public float GetSellPrice(string goodId) => _sellPrices.TryGetValue(goodId, out var price) ? price : 0f;
    public int GetStock(string goodId) => _stocks.TryGetValue(goodId, out var stock) ? stock : 0;
}

public class TraderSnapshot
{
    public ITrader Trader { get; }
    public City CurrentCity { get; }
    public int Gold { get; }
    public int Initiative { get; }

    public TraderSnapshot(ITrader trader, City currentCity, int gold, int initiative)
    {
        Trader = trader;
        CurrentCity = currentCity;
        Gold = gold;
        Initiative = initiative;
    }
}