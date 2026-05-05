using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// ИИ-торговец. Зеркало PlayerToken:
///   - В городе: выбирает путь, торгует
///   - В пути: делает один шаг за ход
///   - Прибыл: торгует, ждёт следующего хода для выбора пути
/// </summary>
public class AITrader : MonoBehaviour, ITrader
{
    // --- ITrader ---
    public string    DisplayName { get; private set; }
    public int       Initiative  { get; private set; }
    public int       Gold        { get; private set; }
    public City      CurrentCity { get; private set; }
    public Inventory Inventory   { get; } = new Inventory();

    // Средние цены покупки — для принятия решения о продаже
    private readonly Dictionary<string, float> _avgPurchasePrices
        = new Dictionary<string, float>();

    public event Action<ITrader, City>                OnArrivedAtCity;
    public event Action<ITrader, PathCellInitializer> OnPathBlocked;

    // --- Состояние ---
    private enum TraderState { InCity, OnPath }
    private TraderState _state = TraderState.InCity;

    // --- Внутренние ---
    private TraderProfile  _profile;
    private AiStrategy     _strategy;
    private PathController _pathController;

    // Путь выбранный в городе — сохраняем до момента движения
    private PathCellInitializer _chosenPath;

    // ------------------------------------------------------------------
    // Инициализация
    // ------------------------------------------------------------------

    public void Initialize(TraderProfile profile, City startCity, WorldEconomy economy)
    {
        _profile        = profile;
        DisplayName     = profile.displayName;
        Initiative      = profile.initiative;
        Gold            = profile.startGold;
        CurrentCity     = startCity;
        _state          = TraderState.InCity;
        _strategy       = new AiStrategy(profile, economy);

        _pathController = GetComponent<PathController>();
        if (_pathController == null)
        {
            _pathController = gameObject.AddComponent<PathController>();
            Debug.Log($"[AITrader] {DisplayName}: PathController добавлен автоматически.");
        }
        _pathController.SetTokenObject(gameObject);
    }

    // ------------------------------------------------------------------
    // ITrader — планирование
    // Вызывается из Task.Run() в начале каждого хода
    // ------------------------------------------------------------------

    public TurnIntent PlanTurn(GameSnapshot snapshot)
    {
        var intent = new TurnIntent { Trader = this };

        if (_state == TraderState.InCity)
        {
            // В городе: выбираем путь через стратегию
            var fullIntent   = _strategy.Evaluate(snapshot, this);
            intent.SelectedPath = fullIntent.SelectedPath;
            intent.BuyOrders    = fullIntent.BuyOrders;
            intent.SellOrders   = fullIntent.SellOrders;

            _chosenPath = intent.SelectedPath;
        }
        else
        {
            // На пути: просто продолжаем идти, торговли нет
            intent.SelectedPath = _chosenPath;
        }

        return intent;
    }

    // ------------------------------------------------------------------
    // ITrader — торговля
    // Вызывается TurnQueue только если ИИ в городе
    // ------------------------------------------------------------------

    public void ExecuteTrade(TurnIntent intent, WorldEconomy economy)
    {
        if (_state != TraderState.InCity) return;

        foreach (var order in intent.SellOrders)
            economy.Sell(CurrentCity, order.GoodId, order.Amount, this);

        foreach (var order in intent.BuyOrders)
            economy.Buy(CurrentCity, order.GoodId, order.Amount, this);
    }

    // ------------------------------------------------------------------
    // ITrader — движение (один шаг за ход)
    // ------------------------------------------------------------------

    public IEnumerator ExecuteMovement(TurnIntent intent)
    {
        if (intent.SelectedPath == null)
        {
            Debug.Log($"[AITrader] {DisplayName}: путь не выбран — стоим в {CurrentCity?.CityName}.");
            yield break;
        }

        // Новый путь — устанавливаем
        if (_pathController.CurrentPath != intent.SelectedPath)
        {
            _pathController.SetPath(intent.SelectedPath);
            _state = TraderState.OnPath;
            Debug.Log($"[AITrader] {DisplayName}: выходит из {CurrentCity?.CityName} " +
                      $"по пути '{intent.SelectedPath.name}'.");
        }

        // Один шаг
        bool arrived = _pathController.Step();

        if (arrived)
        {
            ArriveAtDestination(intent.SelectedPath);
        }
        else
        {
            _pathController.MoveCurrent();
            yield return new WaitForSeconds(AITurnManager.Instance.StepDelay);
        }
    }

    // ------------------------------------------------------------------
    // Прибытие в город
    // ------------------------------------------------------------------

    private void ArriveAtDestination(PathCellInitializer path)
    {
        City finish = path.FinishCity;
        _pathController.ResetPath();
        _chosenPath = null;
        _state      = TraderState.InCity;

        if (finish != null)
        {
            CurrentCity = finish;
            Debug.Log($"[AITrader] {DisplayName} прибыл в {finish.CityName}");
            OnArrivedAtCity?.Invoke(this, finish);
        }
    }

    // ------------------------------------------------------------------
    // Уведомления
    // ------------------------------------------------------------------

    public void NotifyPathBlocked(PathCellInitializer path)
    {
        Debug.Log($"[AITrader] {DisplayName}: путь '{path?.name}' занят — стоим.");
        _chosenPath = null;
        OnPathBlocked?.Invoke(this, path);
    }

    // ------------------------------------------------------------------
    // Экономика
    // ------------------------------------------------------------------

    public void AddGold(int amount)   => Gold += amount;
    public void SpendGold(int amount) => Gold  = Mathf.Max(0, Gold - amount);

    /// <summary>Средняя цена покупки товара — для решения о продаже.</summary>
    public float GetAveragePurchasePrice(string goodId) =>
        _avgPurchasePrices.TryGetValue(goodId, out var p) ? p : 0f;

    /// <summary>Обновить среднюю цену после покупки batch товаров.</summary>
    public void RecordPurchase(string goodId, int amount, float totalCost)
    {
        int   held       = Inventory.GetAmount(goodId);
        float currentAvg = GetAveragePurchasePrice(goodId);
        float prevTotal  = currentAvg * held;
        int   newTotal   = held + amount;

        _avgPurchasePrices[goodId] = newTotal > 0
            ? (prevTotal + totalCost) / newTotal
            : 0f;
    }
}