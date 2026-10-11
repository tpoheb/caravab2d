using System;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using UnityEngine;

/// <summary>Одно проверяемое автосохранение с атомарной заменой файла.</summary>
public static class GameSaveService
{
    [Serializable] private sealed class Envelope { public string checksum, payload; }
    public static string SavePath => Path.Combine(Application.persistentDataPath, "caravan-autosave.json");
    // Проверки редактора используют отдельный каталог, не затрагивая партии пользователя.
    public static string OverridePath { get; set; }
    private static string CurrentPath => OverridePath ?? SavePath;

    public static bool TryRead(out CaravanSaveData data, out string error)
    {
        data = null; error = null;
        if (!File.Exists(CurrentPath)) return false;
        try
        {
            var envelope = JsonUtility.FromJson<Envelope>(File.ReadAllText(CurrentPath));
            if (envelope == null || string.IsNullOrEmpty(envelope.payload) || Hash(envelope.payload) != envelope.checksum)
                throw new InvalidDataException("Файл сохранения повреждён.");
            data = JsonUtility.FromJson<CaravanSaveData>(envelope.payload);
            if (data == null || data.version != 1 || data.turn < 1 || !IsStablePhase(data.phase))
                throw new InvalidDataException("Сохранение несовместимо с этой версией игры.");
            return true;
        }
        catch (Exception exception) when (exception is IOException || exception is ArgumentException ||
            exception is UnauthorizedAccessException || exception is NullReferenceException)
        { error = exception.Message; data = null; return false; }
    }

    public static bool TrySave(out string error)
    {
        error = null;
        try
        {
            if (GameManager.Instance == null || ApplicationFlow.Instance == null)
                throw new InvalidOperationException("Системы сохранения ещё не готовы.");
            Write(GameManager.Instance.CaptureSave(ApplicationFlow.Instance.Catalog));
            return true;
        }
        catch (Exception exception)
        { error = "Не удалось сохранить партию: " + exception.Message; Debug.LogWarning(error); return false; }
    }

    public static void TryAutosave()
    {
        if (ApplicationFlow.Instance != null && !ApplicationFlow.Instance.IsRestoring)
            ApplicationFlow.Instance.NotifySaveResult(TrySave(out var error), error);
    }

    public static void Write(CaravanSaveData data)
    {
        string payload = JsonUtility.ToJson(data);
        string json = JsonUtility.ToJson(new Envelope { checksum = Hash(payload), payload = payload }, true);
        Directory.CreateDirectory(Path.GetDirectoryName(CurrentPath));
        string temporary = CurrentPath + ".tmp";
        using (var stream = new FileStream(temporary, FileMode.Create, FileAccess.Write, FileShare.None))
        using (var writer = new StreamWriter(stream, new UTF8Encoding(false)))
        { writer.Write(json); writer.Flush(); stream.Flush(true); }
        if (File.Exists(CurrentPath)) File.Replace(temporary, CurrentPath, null);
        else File.Move(temporary, CurrentPath);
    }

    private static string Hash(string payload)
    {
        using var sha = SHA256.Create();
        return Convert.ToBase64String(sha.ComputeHash(Encoding.UTF8.GetBytes(payload)));
    }

    public static T Find<T>() where T : Component => UnityEngine.Object.FindObjectsByType<T>(
        FindObjectsInactive.Include, FindObjectsSortMode.None).FirstOrDefault(value =>
            GameManager.Instance != null && value.gameObject.scene == GameManager.Instance.gameObject.scene);

    public static string SceneId(Component value)
    {
        if (value == null) return null;
        string key = value.name;
        for (var node = value.transform; node.parent != null; node = node.parent)
            key = node.parent.name + "/" + node.GetSiblingIndex() + ":" + key;
        return key;
    }
    public static T Resolve<T>(string id) where T : Component
    {
        if (string.IsNullOrEmpty(id)) return null;
        var found = UnityEngine.Object.FindObjectsByType<T>(FindObjectsInactive.Include, FindObjectsSortMode.None)
            .Where(value => GameManager.Instance != null &&
                value.gameObject.scene == GameManager.Instance.gameObject.scene && SceneId(value) == id).ToArray();
        if (found.Length == 1) return found[0];
        throw new InvalidDataException("Объект сцены недоступен в сохранении: " + id);
    }

    public static void Validate(CaravanSaveData data, SaveGameCatalog catalog)
    {
        if (!IsStablePhase(data.phase) || data.turn < 1 || data.money < 0 || data.capacity < 0)
            throw new InvalidDataException("Некорректное состояние партии.");
        if (catalog.Get<PlayerStats>(data.statsAsset) == null) throw new InvalidDataException("Нет характеристик игрока.");
        Resolve<City>(data.city); ValidateRoute(data.playerRoute);
        foreach (var item in data.goods)
        { RequireAsset<Item>(catalog, item.id); if (item.quantity <= 0 || !Finite(item.averagePrice)) throw new InvalidDataException("Некорректный груз."); }
        foreach (var id in data.crew) RequireAsset<UnitData>(catalog, id);
        catalog.Get<GuildData>(data.guild);
        foreach (var id in data.hand) RequireAsset<HandCardData>(catalog, id);
        foreach (var id in data.deck.Concat(data.discard)) RequireAsset<ICard>(catalog, id);
        catalog.Get<ICard>(data.currentCard); catalog.Get<BattleCardData>(data.battle.card);
        var allCards = data.deck.Concat(data.discard).Concat(string.IsNullOrEmpty(data.currentCard) ? Array.Empty<string>() : new[] { data.currentCard }).ToArray();
        if (allCards.Distinct().Count() != allCards.Length || data.selectedDice < 0 || data.selectedDice > 6 || data.battle.dice < 0 || data.battle.dice > 6)
            throw new InvalidDataException("Некорректное состояние карт.");
        var cities = Find<CityManager>().AllCities;
        if (data.markets.Count != cities.Count || data.markets.Select(value => value.city).Distinct().Count() != cities.Count)
            throw new InvalidDataException("Состав городов изменился.");
        foreach (var market in data.markets)
        {
            var city = Resolve<City>(market.city);
            if (!cities.Contains(city) || market.items.Count != city.RuntimeData.items.Count ||
                market.items.Select(item => item.id).Distinct().Count() != market.items.Count)
                throw new InvalidDataException("Состав рынка изменился.");
            foreach (var saved in market.items)
                if (saved.stock < 0 || !Finite(saved.buy) || !Finite(saved.sell) ||
                    !city.RuntimeData.items.Any(item => item.item == catalog.Get<Item>(saved.id)))
                    throw new InvalidDataException("Некорректный рынок.");
        }
        foreach (var trader in data.traders)
        { RequireAsset<TraderProfile>(catalog, trader.profile); Resolve<City>(trader.city); ValidateRoute(trader.route); }
        if (AITurnManager.Instance != null && !AITurnManager.Instance.MatchesSave(data, catalog))
            throw new InvalidDataException("Состав караванов ИИ изменился.");
        if (Find<StorySystem.StoryManager>() != null && !Find<StorySystem.StoryManager>().MatchesSave(data.story))
            throw new InvalidDataException("История сохранения несовместима с этой версией.");
    }
    private static void RequireAsset<T>(SaveGameCatalog catalog, string id) where T : class
    { if (catalog.Get<T>(id) == null) throw new InvalidDataException("Пустая ссылка на ассет."); }
    private static bool Finite(float value) => !float.IsNaN(value) && !float.IsInfinity(value);
    private static void ValidateRoute(RouteSave route)
    {
        var path = Resolve<PathCellInitializer>(route.path);
        if (path != null && (route.cell < -1 || route.cell >= path.CellCount))
            throw new InvalidDataException("Сохранённая клетка маршрута отсутствует.");
    }
    private static bool IsStablePhase(GameState phase) => phase == GameState.InCity || phase == GameState.DrawingCard ||
        phase == GameState.PreparingEncounter || phase == GameState.InBattle || phase == GameState.ResolvingEvent || phase == GameState.PostEncounter;
}
