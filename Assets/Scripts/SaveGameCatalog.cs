using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>Стабильные GUID ассетов для сохранений, доступные и в сборке.</summary>
[CreateAssetMenu(menuName = "Game/Каталог сохранений")]
public sealed class SaveGameCatalog : ScriptableObject
{
    [Serializable] public struct Entry { public string id; public ScriptableObject asset; }
    [SerializeField] private Entry[] _entries = Array.Empty<Entry>();
    private Dictionary<string, ScriptableObject> _byId;
    private Dictionary<ScriptableObject, string> _byAsset;

    public void Configure(Entry[] entries) { _entries = entries; _byId = null; _byAsset = null; }
    private void Build()
    {
        if (_byId != null) return;
        _byId = new Dictionary<string, ScriptableObject>();
        _byAsset = new Dictionary<ScriptableObject, string>();
        foreach (var entry in _entries)
        {
            if (entry.asset == null) continue;
            _byId.Add(entry.id, entry.asset);
            _byAsset.Add(entry.asset, entry.id);
        }
    }
    public string Id(object value)
    {
        if (value == null) return null;
        Build();
        if (value is ScriptableObject asset && _byAsset.TryGetValue(asset, out var id)) return id;
        throw new InvalidOperationException("Ассет отсутствует в каталоге сохранений: " + value);
    }
    public T Get<T>(string id) where T : class
    {
        if (string.IsNullOrEmpty(id)) return null;
        Build();
        if (_byId.TryGetValue(id, out var asset) && asset is T result) return result;
        throw new InvalidOperationException("Сохранение ссылается на недоступный ассет: " + id);
    }
}
