using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

// Частичные классы держат запись состояния рядом с моделью, без доступа через Reflection.
public partial class GameManager
{
    public bool CanSave => State == GameState.InCity || State == GameState.DrawingCard ||
        State == GameState.PreparingEncounter || State == GameState.InBattle ||
        State == GameState.ResolvingEvent || State == GameState.PostEncounter;

    public CaravanSaveData CaptureSave(SaveGameCatalog catalog)
    {
        if (!CanSave) throw new InvalidOperationException("Дождитесь окончания движения и хода ИИ.");
        var data = new CaravanSaveData
        {
            savedAt = DateTime.UtcNow.ToString("O"), turn = TurnNumber, phase = State,
            pendingBattle = _pendingBattle, choosingDice = _awaitingDiceChoice,
            selectedDice = _selectedDiceValue ?? 0, statsAsset = catalog.Id(playerStats),
            randomState = UnityEngine.Random.state
        };
        playerInventory.CaptureSave(data, catalog);
        playerStats.CaptureSave(data);
        var team = GameSaveService.Find<TeamSystem>();
        data.crew = team.CurrentTeam.Select(member => catalog.Id(member.unitData)).ToList();
        var guild = GameSaveService.Find<GuildSystem>();
        if (guild != null) data.guild = catalog.Id(guild.CurrentGuild);
        var cities = GameSaveService.Find<CityManager>();
        data.city = GameSaveService.SceneId(cities.PlayerCurrentCity);
        foreach (var city in cities.AllCities)
        {
            var market = new MarketSave { city = GameSaveService.SceneId(city), gold = city.RuntimeData.cityGold };
            foreach (var item in city.RuntimeData.items)
                market.items.Add(new MarketItemSave { id = catalog.Id(item.item), stock = item.stock,
                    buy = item.currentBuyPrice, sell = item.currentSellPrice });
            data.markets.Add(market);
        }
        data.playerRoute = playerToken.GetComponent<PathController>().CaptureRoute();
        data.playerPosition = playerToken.transform.position;
        data.playerVisible = playerToken.gameObject.activeSelf;
        cardManager.CaptureSave(data, catalog);
        battleManager.CaptureSave(data, catalog);
        handManager.CaptureSave(data, catalog);
        shadowEffectManager.CaptureSave(data);
        GameSaveService.Find<TradeCardModifiers>()?.CaptureSave(data);
        AITurnManager.Instance?.CaptureSave(data, catalog);
        GameSaveService.Find<WorldEconomy>()?.CaptureSave(data);
        GameSaveService.Find<StorySystem.StoryStateAdapter>()?.CaptureSave(data.story);
        GameSaveService.Find<StorySystem.StoryManager>()?.CaptureSave(data.story);
        if (GameJournal.Instance != null) data.journal = GameJournal.Instance.GetQuestEntries();
        return data;
    }

    public void RestoreSave(CaravanSaveData data, SaveGameCatalog catalog)
    {
        // Все ссылки проверены сервисом до первой мутации мира.
        playerStats.RestoreSave(data);
        GameSaveService.Find<TeamSystem>().RestoreCrew(data.crew, catalog);
        GameSaveService.Find<GuildSystem>()?.RestoreGuild(catalog.Get<GuildData>(data.guild));
        playerInventory.RestoreSave(data, catalog);
        foreach (var market in data.markets)
        {
            var city = GameSaveService.Resolve<City>(market.city);
            city.RuntimeData.cityGold = market.gold;
            foreach (var saved in market.items)
            {
                var item = city.RuntimeData.items.Single(entry => entry.item == catalog.Get<Item>(saved.id));
                item.stock = saved.stock; item.currentBuyPrice = saved.buy; item.currentSellPrice = saved.sell;
                item.UpdateDemand();
            }
        }
        playerToken.GetComponent<PathController>().RestoreRoute(data.playerRoute);
        playerToken.transform.position = data.playerPosition;
        playerToken.gameObject.SetActive(data.playerVisible);
        GameSaveService.Find<CityManager>().RestoreCity(GameSaveService.Resolve<City>(data.city), data.phase == GameState.InCity);
        cardManager.RestoreSave(data, catalog);
        handManager.RestoreSave(data, catalog);
        shadowEffectManager.RestoreSave(data);
        GameSaveService.Find<TradeCardModifiers>()?.RestoreSave(data);
        AITurnManager.Instance?.RestoreSave(data, catalog);
        GameSaveService.Find<WorldEconomy>()?.RestoreSave(data);
        GameSaveService.Find<StorySystem.StoryStateAdapter>()?.RestoreSave(data.story);
        GameSaveService.Find<StorySystem.StoryManager>()?.RestoreSave(data.story);
        GameJournal.Instance?.RestoreEntries(data.journal);
        TurnNumber = data.turn; _pendingBattle = data.pendingBattle;
        _selectedDiceValue = data.selectedDice == 0 ? null : (int?)data.selectedDice;
        _awaitingDiceChoice = data.choosingDice;
        if (diceChoicePanel != null) diceChoicePanel.SetActive(_awaitingDiceChoice);
        battleManager.RestoreSave(data, catalog);
        SetState(data.phase);
        UnityEngine.Random.state = data.randomState;
    }
}

public partial class PlayerInventory
{
    public void CaptureSave(CaravanSaveData data, SaveGameCatalog catalog)
    {
        data.money = Money;
        data.goods = items.Select(entry => new GoodsSave { id = catalog.Id(entry.item),
            quantity = entry.quantity, averagePrice = entry.averagePurchasePrice }).ToList();
    }
    public void RestoreSave(CaravanSaveData data, SaveGameCatalog catalog)
    {
        items.Clear();
        foreach (var entry in data.goods)
            items.Add(new InventoryItem { item = catalog.Get<Item>(entry.id), quantity = entry.quantity,
                averagePurchasePrice = entry.averagePrice });
        Money = data.money;
        OnMoneyChanged?.Invoke(); OnInventoryChanged?.Invoke();
    }
}
public partial class PlayerStats
{
    public void CaptureSave(CaravanSaveData data)
    {
        data.attack = _currentAttack; data.bargain = _currentBargain; data.capacity = _currentCapacity;
        data.multipliers = new List<int>(_teamMultiplierStack);
    }
    public void RestoreSave(CaravanSaveData data)
    {
        _currentAttack = data.attack; _currentBargain = data.bargain; _currentCapacity = data.capacity;
        _teamMultiplierStack = new List<int>(data.multipliers);
        OnStatsChanged?.Invoke();
    }
}
public partial class TeamSystem
{
    public void RestoreCrew(List<string> crew, SaveGameCatalog catalog)
    {
        // Бонусы уже включены в снимок PlayerStats.
        CurrentTeam.Clear();
        foreach (var id in crew) CurrentTeam.Add(new TeamMember(catalog.Get<UnitData>(id)));
    }
}
public partial class GuildSystem
{
    public void RestoreGuild(GuildData guild) { CurrentGuild = guild; OnGuildChanged?.Invoke(); }
}
public partial class CityManager
{
    public void RestoreCity(City city, bool open)
    {
        lastArrivedCity = city;
        cityPanel.ClosePanel();
        if (open && city != null) OpenCityPanel(city);
    }
}
public partial class PathController
{
    public RouteSave CaptureRoute() => new RouteSave { path = GameSaveService.SceneId(currentPath), cell = currentCellIndex };
    public void RestoreRoute(RouteSave saved)
    {
        if (string.IsNullOrEmpty(saved.path)) { ResetPath(); return; }
        SetPath(GameSaveService.Resolve<PathCellInitializer>(saved.path));
        currentCellIndex = saved.cell;
        if (IsValidIndex(currentCellIndex)) MoveCurrent();
    }
}
public partial class CardManager
{
    public void CaptureSave(CaravanSaveData data, SaveGameCatalog catalog)
    {
        data.deck = _deck.Cards.Select(catalog.Id).ToList();
        data.discard = _discard.Select(catalog.Id).ToList();
        data.currentCard = catalog.Id(_currentCard); data.eventResolved = _eventResolved;
    }
    public void RestoreSave(CaravanSaveData data, SaveGameCatalog catalog)
    {
        _deck.Clear(); _deck.AddRange(data.deck.Select(catalog.Get<ICard>));
        _discard.Clear(); _discard.AddRange(data.discard.Select(catalog.Get<ICard>));
        _currentCard = catalog.Get<ICard>(data.currentCard); _eventResolved = data.eventResolved;
        deckUI?.HideCurrentCard();
        // Показ без RouteCard: не запускает подготовку или эффект повторно.
        if (_currentCard != null)
        {
            if (_currentCard is BattleCardData && !data.pendingBattle) deckUI?.ShowCancelledCard(_currentCard);
            else deckUI?.ShowCard(_currentCard);
        }
        UpdateDeckCounts();
    }
}
public partial class HandManager
{
    public void CaptureSave(CaravanSaveData data, SaveGameCatalog catalog)
    {
        data.hand = currentHand.Select(catalog.Id).ToList(); data.penaltyReduction = _pendingPenaltyReduction;
    }
    public void RestoreSave(CaravanSaveData data, SaveGameCatalog catalog)
    {
        currentHand = data.hand.Select(catalog.Get<HandCardData>).ToList();
        _pendingPenaltyReduction = data.penaltyReduction; RefreshUI();
    }
}
public partial class BattleManager
{
    public void CaptureSave(CaravanSaveData data, SaveGameCatalog catalog)
    {
        data.battle = new BattleSave { card = catalog.Id(_card), dice = _lastDiceRoll, bonus = _attackBonus,
            debuff = _enemyDebuff, resolved = _resolved, victory = _victory };
    }
    public void RestoreSave(CaravanSaveData data, SaveGameCatalog catalog)
    {
        var saved = data.battle;
        _card = catalog.Get<BattleCardData>(saved.card); _lastDiceRoll = saved.dice;
        _attackBonus = saved.bonus; _enemyDebuff = saved.debuff; _resolved = saved.resolved; _victory = saved.victory;
        if (_card == null) return;
        if (_resolved) uiManager?.DisplayBattleResult(_victory, _card, PlayerBaseAttack + _lastDiceRoll);
        else RefreshPreview();
        RefreshPostCombatPreview();
    }
}
public partial class ShadowEffectManager
{
    public void CaptureSave(CaravanSaveData data)
    {
        data.wagePenalty = _wagePenaltyActive; data.bonusTrade = _bonusTradePercent;
        data.effects = _activeEffects.Select(effect => new EffectSave { type = effect.Data.effectType,
            value = effect.Data.value, remaining = effect.RemainingTurns, title = effect.Data.cardName }).ToList();
    }
    public void RestoreSave(CaravanSaveData data)
    {
        _activeEffects.Clear(); _wagePenaltyActive = data.wagePenalty; _bonusTradePercent = data.bonusTrade;
        foreach (var effect in data.effects)
        {
            var card = ScriptableObject.CreateInstance<ShadowCardData>();
            card.hideFlags = HideFlags.DontSave; card.effectType = effect.type; card.value = effect.value;
            card.cardName = effect.title; card.duration = effect.remaining;
            _activeEffects.Add(new ActiveEffect(card));
        }
    }
}
public partial class TradeCardModifiers
{
    public void CaptureSave(CaravanSaveData data)
    {
        data.saleBonus = _pendingSaleBonus; data.ignoreTax = _ignoreTaxActive;
        data.saleBoost = _pendingSalePriceBoost; data.purchaseDiscount = _pendingPurchaseDiscount;
    }
    public void RestoreSave(CaravanSaveData data)
    {
        _pendingSaleBonus = data.saleBonus; _ignoreTaxActive = data.ignoreTax;
        _pendingSalePriceBoost = data.saleBoost; _pendingPurchaseDiscount = data.purchaseDiscount;
    }
}
public partial class AITrader
{
    public TraderSave CaptureSave(SaveGameCatalog catalog)
    {
        var saved = new TraderSave { profile = catalog.Id(_profile), city = GameSaveService.SceneId(CurrentCity),
            gold = Gold, route = _pathController.CaptureRoute(), position = transform.position, visible = gameObject.activeSelf };
        foreach (var item in Inventory.GetAll()) saved.goods.Add(new GoodsSave { id = item.Key,
            quantity = item.Value, averagePrice = GetAveragePurchasePrice(item.Key) });
        return saved;
    }
    public void RestoreSave(TraderSave saved)
    {
        Gold = saved.gold; CurrentCity = GameSaveService.Resolve<City>(saved.city);
        Inventory.Clear(); _avgPurchasePrices.Clear();
        foreach (var item in saved.goods)
        { Inventory.Add(item.id, item.quantity); _avgPurchasePrices[item.id] = item.averagePrice; }
        _pathController.RestoreRoute(saved.route); _chosenPath = _pathController.CurrentPath;
        _state = _chosenPath != null ? TraderState.OnPath : TraderState.InCity;
        transform.position = saved.position; gameObject.SetActive(saved.visible);
    }
}
public partial class AITurnManager
{
    public bool IsReady => _turnQueue != null;
    public bool IsProcessing => _isProcessing;
    public void CaptureSave(CaravanSaveData data, SaveGameCatalog catalog)
    {
        if (_isProcessing) throw new InvalidOperationException("Сохранение во время исполнения ИИ запрещено.");
        data.aiTurn = _turnQueue.TurnNumber;
        data.traders = _aiTraders.Select(trader => trader.CaptureSave(catalog)).ToList();
    }
    public void RestoreSave(CaravanSaveData data, SaveGameCatalog catalog)
    {
        for (int i = 0; i < _aiTraders.Count; i++) _aiTraders[i].RestoreSave(data.traders[i]);
        _turnQueue.RestoreTurnNumber(data.aiTurn);
    }
    public bool MatchesSave(CaravanSaveData data, SaveGameCatalog catalog) =>
        data.traders.Count == _aiTraders.Count && _aiTraders.Select(trader => trader.CaptureSave(catalog).profile)
            .SequenceEqual(data.traders.Select(trader => trader.profile));
}
public partial class WorldEconomy
{
    public void CaptureSave(CaravanSaveData data) => data.economyTurn = _turnNumber;
    public void RestoreSave(CaravanSaveData data) => _turnNumber = data.economyTurn;
}
public partial class GameJournal
{
    public void RestoreEntries(List<JournalEntryData> entries)
    { Clear(); foreach (var entry in entries) { _questEntries.Add(entry); SpawnEntry(entry); } }
}
namespace StorySystem
{
    public partial class StoryStateAdapter
    {
        public void CaptureSave(StorySave data)
        { data.city = _lastVisitedCityId; data.collected = _collectedEventCardIds.ToList(); data.hired = _hiredUnitTypes.ToList(); }
        public void RestoreSave(StorySave data)
        {
            _lastVisitedCityId = data.city; _collectedEventCardIds.Clear(); _hiredUnitTypes.Clear();
            foreach (var id in data.collected) _collectedEventCardIds.Add(id);
            foreach (var id in data.hired) _hiredUnitTypes.Add(id);
        }
    }
    public partial class StoryManager
    {
        public void CaptureSave(StorySave data)
        {
            data.shown = GetShownBeatIds(); data.pending = _pendingBeats.Select(beat => beat.beatId).ToList();
            data.current = _currentBeat != null ? _currentBeat.beatId : null;
            if (questJournal != null) data.entries = questJournal.GetAllEntries();
        }
        public bool MatchesSave(StorySave data) => data.shown.Concat(data.pending)
            .Concat(string.IsNullOrEmpty(data.current) ? Array.Empty<string>() : new[] { data.current })
            .All(id => allBeats.Any(beat => beat != null && beat.beatId == id));
        public void RestoreSave(StorySave data)
        {
            LoadShownBeatIds(data.shown); _pendingBeats.Clear(); _currentBeat = null;
            storyWindowUI?.Hide();
            foreach (var id in data.pending) _pendingBeats.Enqueue(allBeats.Single(beat => beat.beatId == id));
            questJournal?.LoadEntries(data.entries);
            if (!string.IsNullOrEmpty(data.current))
            {
                _currentBeat = allBeats.Single(beat => beat.beatId == data.current);
                storyWindowUI?.Show(_currentBeat, OnWindowClosed);
            }
        }
    }
}
