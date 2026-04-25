using System;
using System.Collections;
using UnityEngine;

/// <summary>
/// ИИ-торговец. Зеркало PlayerToken — двигается пошагово через PathController,
/// но вместо ввода игрока использует AiStrategy.
///
/// Подключается к сцене как MonoBehaviour на отдельном GameObject.
/// GameManager.AI создаёт его через Instantiate(profile.tokenPrefab).
/// </summary>
public class AITrader : MonoBehaviour, ITrader
{
    // --- ITrader ---
    public string DisplayName { get; private set; }
    public int    Initiative  { get; private set; }
    public int    Gold        { get; private set; }
    public City   CurrentCity { get; private set; }
    public Inventory Inventory { get; } = new Inventory();

    public event Action<ITrader, City>                  OnArrivedAtCity;
    public event Action<ITrader, PathCellInitializer>   OnPathBlocked;

    // --- Внутренние ---
    private TraderProfile  _profile;
    private AiStrategy     _strategy;
    private PathController _pathController; // берём с того же GameObject

    // ------------------------------------------------------------------
    // Инициализация (вызывается из AITurnManager)
    // ------------------------------------------------------------------

    public void Initialize(TraderProfile profile, City startCity, WorldEconomy economy)
    {
        _profile        = profile;
        DisplayName     = profile.displayName;
        Initiative      = profile.initiative;
        Gold            = profile.startGold;
        CurrentCity     = startCity;
        _strategy       = new AiStrategy(profile, economy);
        _pathController = GetComponent<PathController>();

        // Если PathController не добавлен на префаб — добавляем автоматически
        if (_pathController == null)
        {
            _pathController = gameObject.AddComponent<PathController>();
            Debug.Log($"[AITrader] {DisplayName}: PathController добавлен автоматически.");
        }

        // Передаём сам GameObject как визуальный токен
        // PathController.tokenObject — это [SerializeField], поэтому используем публичный метод
        _pathController.SetTokenObject(gameObject);
    }

    // ------------------------------------------------------------------
    // ITrader — планирование (вызывается из TurnQueue в фоне)
    // ------------------------------------------------------------------

    public TurnIntent PlanTurn(GameSnapshot snapshot)
    {
        return _strategy.Evaluate(snapshot, this);
    }

    // ------------------------------------------------------------------
    // ITrader — торговля (вызывается TurnQueue по инициативе)
    // ------------------------------------------------------------------

    public void ExecuteTrade(TurnIntent intent, WorldEconomy economy)
    {
        foreach (var order in intent.SellOrders)
            economy.Sell(CurrentCity, order.GoodId, order.Amount, this);

        foreach (var order in intent.BuyOrders)
            economy.Buy(CurrentCity, order.GoodId, order.Amount, this);
    }

    // ------------------------------------------------------------------
    // ITrader — движение (корутина, yield return из TurnQueue)
    // ------------------------------------------------------------------

    public IEnumerator ExecuteMovement(TurnIntent intent)
    {
        if (intent.SelectedPath == null)
        {
            Debug.Log($"[AITrader] {DisplayName}: путь не выбран — стоим.");
            yield break;
        }

        Debug.Log($"[AITrader] {DisplayName}: начинаем движение по '{intent.SelectedPath.name}' " +
                  $"из {CurrentCity?.CityName}. " +
                  $"CurrentPath совпадает: {_pathController.CurrentPath == intent.SelectedPath}");

        // Устанавливаем путь если это новый путь
        if (_pathController.CurrentPath != intent.SelectedPath)
        {
            _pathController.SetPath(intent.SelectedPath);
            Debug.Log($"[AITrader] {DisplayName}: SetPath вызван.");
        }

        bool arrived = _pathController.Step();

        Debug.Log($"[AITrader] {DisplayName}: Step() → arrived={arrived}");

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
    // Вспомогательные
    // ------------------------------------------------------------------

    private void ArriveAtDestination(PathCellInitializer path)
    {
        City finish = path.FinishCity;
        _pathController.ResetPath();

        if (finish != null)
        {
            CurrentCity = finish;
            Debug.Log($"[AITrader] {DisplayName} прибыл в {finish.CityName}");
            OnArrivedAtCity?.Invoke(this, finish);
        }
    }

    /// <summary>Вызывается TurnQueue когда путь занят другим торговцем.</summary>
    public void NotifyPathBlocked(PathCellInitializer path)
    {
        Debug.Log($"[AITrader] {DisplayName}: путь {path?.name} занят.");
        OnPathBlocked?.Invoke(this, path);
    }

    // Вызывается WorldEconomy при исполнении сделок
    public void AddGold(int amount)    => Gold += amount;
    public void SpendGold(int amount)  => Gold =  Mathf.Max(0, Gold - amount);
}