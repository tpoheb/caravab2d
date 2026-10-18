using System;
using System.IO;
using UnityEditor;
using UnityEngine;

/// <summary>Данные таблиц DOCX хранятся рядом с ассетами; повторный импорт не возвращает старые карты.</summary>
public static class DocumentCardImporter
{
    public const string SourcePath = "Assets/Data/DocumentCards.json";
    [Serializable] public sealed class Source { public string source; public int declaredCount, actualCount; public Definition[] cards; }
    [Serializable] public sealed class Definition
    {
        public string path, name, description, effect, group, unavailableReason, moment;
        public int value, duration, timing;
    }
    public static Source ReadSource() => JsonUtility.FromJson<Source>(File.ReadAllText(SourcePath));

    [MenuItem("Tools/1000 Roads/Импортировать карты из документа")]
    public static void ImportAll() { Import(true, true); }
    public static void ImportHandCards() { Import(false, true); }
    public static void ImportEvents() { Import(true, false); }

    private static void Import(bool events, bool hand)
    {
        foreach (var definition in ReadSource().cards)
        {
            if (definition.group == "Event")
            {
                if (!events) continue;
                var card = LoadOrCreate<ShadowCardData>(definition.path);
                card.cardName = definition.name; card.description = definition.description;
                card.effectType = (ShadowEffectType)Enum.Parse(typeof(ShadowEffectType), definition.effect);
                card.value = definition.value; card.duration = definition.duration; card.isTemporary = true;
                card.cardID = int.Parse(Path.GetFileName(definition.path).Split('_')[1]);
                card.weight = 10; card.minDifficulty = 0; card.maxDifficulty = 10; card.penaltyValue = 0;
                EditorUtility.SetDirty(card);
            }
            else
            {
                if (!hand) continue;
                var card = LoadOrCreate<HandCardData>(definition.path);
                card.cardName = definition.name; card.description = definition.description;
                card.effectType = (HandCardData.CardEffectType)Enum.Parse(typeof(HandCardData.CardEffectType), definition.effect);
                card.value = definition.value; card.playTiming = (HandCardData.PlayTiming)definition.timing;
                card.group = (HandCardData.CardGroup)Enum.Parse(typeof(HandCardData.CardGroup), definition.group);
                card.category = HandCardData.CardCategory.Tactical;
                card.unavailableReason = definition.unavailableReason; card.momentLabel = definition.moment;
                EditorUtility.SetDirty(card);
            }
        }
        AssetDatabase.SaveAssets();
        GameMenuSetup.RefreshCatalog();
        Debug.Log("[DocumentCardImporter] Импортирован актуальный набор карт из " + SourcePath);
    }
    private static T LoadOrCreate<T>(string path) where T : ScriptableObject
    {
        var card = AssetDatabase.LoadAssetAtPath<T>(path);
        if (card != null) return card;
        card = ScriptableObject.CreateInstance<T>(); AssetDatabase.CreateAsset(card, path); return card;
    }
}
