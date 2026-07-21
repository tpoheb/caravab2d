using System;
using System.Collections;
using UnityEngine;

/// <summary>
/// Адаптер между существующим PlayerToken и системой TurnQueue.
///
/// Не трогает PlayerToken — просто слушает его события и
/// предоставляет TurnQueue то что ему нужно (IsReady, ConsumeIntent).
///
/// Вешается на тот же GameObject что и PlayerToken,
/// или на отдельный объект в сцене.
/// </summary>
public class PlayerTraderAdapter : MonoBehaviour, ITrader
{
    [Header("Зависимости")]
    [SerializeField] private PlayerToken playerToken;
    [SerializeField] private TraderProfile profile;

    // --- ITrader ---
    public string    DisplayName { get; private set; }
    public int       Initiative  { get; private set; }
    public int       Gold        { get; private set; }
    public City      CurrentCity { get; private set; }
    public Inventory Inventory   { get; } = new Inventory();

    public event Action<ITrader, City>                OnArrivedAtCity;
    public event Action<ITrader, PathCellInitializer> OnPathBlocked { add { } remove { } }

    // --- Состояние хода ---
    public bool IsReady { get; private set; }
    private TurnIntent _pendingIntent;

    // ------------------------------------------------------------------
    // Unity
    // ------------------------------------------------------------------

    private void Awake()
    {
        if (profile != null)
        {
            DisplayName = profile.displayName;
            Initiative  = profile.initiative;
            Gold        = profile.startGold;
        }
    }

    private void OnEnable()
    {
        if (playerToken != null)
            playerToken.OnArrivedAtCity += HandleArrivedAtCity;
    }

    private void OnDisable()
    {
        if (playerToken != null)
            playerToken.OnArrivedAtCity -= HandleArrivedAtCity;
    }

    // ------------------------------------------------------------------
    // Вызывается UI кнопкой "Завершить ход"
    // (дополнительно к существующему GameManager.RequestEndTurn)
    // ------------------------------------------------------------------

    /// <summary>
    /// UI передаёт сюда выбранный путь и торговые заявки.
    /// После этого IsReady = true и TurnQueue продолжает ход.
    /// </summary>
    public void SubmitTurn(PathCellInitializer selectedPath,
        System.Collections.Generic.List<TradeOrder> buyOrders  = null,
        System.Collections.Generic.List<TradeOrder> sellOrders = null)
    {
        _pendingIntent = new TurnIntent
        {
            Trader      = this,
            SelectedPath = selectedPath,
            BuyOrders   = buyOrders  ?? new System.Collections.Generic.List<TradeOrder>(),
            SellOrders  = sellOrders ?? new System.Collections.Generic.List<TradeOrder>()
        };

        IsReady = true;
    }

    /// <summary>
    /// TurnQueue вызывает это после IsReady == true.
    /// Сбрасывает флаг для следующего хода.
    /// </summary>
    public TurnIntent ConsumeIntent()
    {
        IsReady = false;
        return _pendingIntent;
    }

    // ------------------------------------------------------------------
    // ITrader
    // ------------------------------------------------------------------

    public TurnIntent PlanTurn(GameSnapshot snapshot)
    {
        // Игрок уже сформировал интент через SubmitTurn()
        return _pendingIntent;
    }

    public void ExecuteTrade(TurnIntent intent, WorldEconomy economy)
    {
        foreach (var order in intent.SellOrders)
            economy.Sell(CurrentCity, order.GoodId, order.Amount, this);

        foreach (var order in intent.BuyOrders)
            economy.Buy(CurrentCity, order.GoodId, order.Amount, this);
    }

    /// <summary>
    /// Движение игрока делает существующий PlayerToken.
    /// Мы просто ждём пока он доберётся до города.
    /// </summary>
    public IEnumerator ExecuteMovement(TurnIntent intent)
    {
        if (intent.SelectedPath == null) yield break;

        // Передаём путь в PlayerToken — он движется сам
        playerToken.StartPath(intent.SelectedPath);

        // Ждём прибытия в город (OnArrivedAtCity сбросит флаг)
        bool arrived = false;
        Action<City> handler = _ => arrived = true;
        playerToken.OnArrivedAtCity += handler;

        while (!arrived)
        {
            // PlayerToken.AdvanceToken() вызывается из старого GameManager.
            // Если ты интегрируешь TurnQueue в GameManager — вызывай здесь.
            yield return null;
        }

        playerToken.OnArrivedAtCity -= handler;
    }

    // ------------------------------------------------------------------
    // Вспомогательные
    // ------------------------------------------------------------------

    private void HandleArrivedAtCity(City city)
    {
        CurrentCity = city;
        OnArrivedAtCity?.Invoke(this, city);
    }

    public void AddGold(int amount)   => Gold += amount;
    public void SpendGold(int amount) => Gold  = Mathf.Max(0, Gold - amount);
}
