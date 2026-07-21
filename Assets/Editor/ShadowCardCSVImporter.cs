using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;
using UnityEditor;

/// <summary>
/// Импортёр карт тени из CSV.
/// 
/// Формат CSV (разделитель — табуляция или запятая):
/// ID	Name	Description	System	Intensity	Tone	EffectType	Value	IsTemporary	Duration	MinDifficulty	MaxDifficulty	Weight	PenaltyValue
/// 
/// Меню: Tools → Cards → Import Shadow Cards from CSV
/// </summary>
public static class ShadowCardCSVImporter
{
    private const string OUTPUT_PATH = "Assets/Data/ShadowEvent";
    private const string CSV_PATH = "Assets/Data/ShadowCards.csv";

    [MenuItem("Tools/Cards/Import Shadow Cards from CSV")]
    public static void ImportFromCSV()
    {
        if (!File.Exists(CSV_PATH))
        {
            EditorUtility.DisplayDialog("Ошибка", $"CSV файл не найден:\n{CSV_PATH}\n\nСоздайте файл и заполните данные.", "OK");
            return;
        }

        // Создаём папку если нет
        if (!AssetDatabase.IsValidFolder("Assets/Data"))
            AssetDatabase.CreateFolder("Assets", "Data");
        if (!AssetDatabase.IsValidFolder(OUTPUT_PATH))
            AssetDatabase.CreateFolder("Assets/Data", "ShadowEvent");

        string[] lines = File.ReadAllLines(CSV_PATH);
        if (lines.Length < 2)
        {
            EditorUtility.DisplayDialog("Ошибка", "CSV файл пуст или содержит только заголовок.", "OK");
            return;
        }

        // Парсим заголовок
        string[] headers = ParseLine(lines[0]);
        var columnMap = new Dictionary<string, int>();
        for (int i = 0; i < headers.Length; i++)
            columnMap[headers[i].Trim().ToLower()] = i;

        // Проверяем обязательные колонки
        string[] required = { "id", "name", "description", "effecttype" };
        foreach (var req in required)
        {
            if (!columnMap.ContainsKey(req))
            {
                EditorUtility.DisplayDialog("Ошибка", $"Отсутствует обязательная колонка: {req}", "OK");
                return;
            }
        }

        int created = 0;
        int updated = 0;
        int skipped = 0;

        for (int i = 1; i < lines.Length; i++)
        {
            if (string.IsNullOrWhiteSpace(lines[i])) continue;

            string[] fields = ParseLine(lines[i]);
            if (fields.Length < 4) continue;

            try
            {
                int id = int.Parse(GetField(fields, columnMap, "id"));
                string fileName = $"Shadow_{id:000}_{SanitizeFileName(GetField(fields, columnMap, "name"))}";
                string assetPath = $"{OUTPUT_PATH}/{fileName}.asset";

                ShadowCardData card;

                // Обновляем существующий или создаём новый
                var existing = AssetDatabase.LoadAssetAtPath<ShadowCardData>(assetPath);
                if (existing != null)
                {
                    card = existing;
                    updated++;
                }
                else
                {
                    card = ScriptableObject.CreateInstance<ShadowCardData>();
                    AssetDatabase.CreateAsset(card, assetPath);
                    created++;
                }

                // Заполняем данные
                card.cardID = id;
                card.cardName = GetField(fields, columnMap, "name");
                card.description = GetField(fields, columnMap, "description");
                card.effectType = ParseEffectType(GetField(fields, columnMap, "effecttype"));
                card.value = GetIntField(fields, columnMap, "value", 0);
                card.isTemporary = GetBoolField(fields, columnMap, "istemporary", false);
                card.duration = GetIntField(fields, columnMap, "duration", 1);
                card.minDifficulty = GetIntField(fields, columnMap, "mindifficulty", 0);
                card.maxDifficulty = GetIntField(fields, columnMap, "maxdifficulty", 10);
                card.weight = GetIntField(fields, columnMap, "weight", 10);
                card.penaltyValue = GetIntField(fields, columnMap, "penaltyvalue", 200);

                EditorUtility.SetDirty(card);
            }
            catch (Exception ex)
            {
                Debug.LogError($"[ShadowCardCSVImporter] Ошибка в строке {i + 1}: {ex.Message}");
                skipped++;
            }
        }

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        EditorUtility.DisplayDialog(
            "Импорт завершён",
            $"Создано: {created}\nОбновлено: {updated}\nПропущено/ошибок: {skipped}\n\nПуть: {OUTPUT_PATH}",
            "OK"
        );

        Debug.Log($"[ShadowCardCSVImporter] Создано: {created}, обновлено: {updated}, пропущено: {skipped}");
    }

    // ── Парсеры ──────────────────────────────────────────────────────────

    private static string[] ParseLine(string line)
    {
        // Поддержка CSV с кавычками и запятыми внутри полей
        var result = new List<string>();
        bool inQuotes = false;
        var currentField = new System.Text.StringBuilder();

        for (int i = 0; i < line.Length; i++)
        {
            char c = line[i];

            if (c == '"')
            {
                inQuotes = !inQuotes;
                continue;
            }

            if ((c == ',' || c == '\t') && !inQuotes)
            {
                result.Add(currentField.ToString().Trim());
                currentField.Clear();
                continue;
            }

            currentField.Append(c);
        }

        result.Add(currentField.ToString().Trim());
        return result.ToArray();
    }

    private static string GetField(string[] fields, Dictionary<string, int> map, string column)
    {
        if (!map.TryGetValue(column.ToLower(), out int index)) return "";
        if (index >= fields.Length) return "";
        return fields[index].Trim('"').Trim();
    }

    private static int GetIntField(string[] fields, Dictionary<string, int> map, string column, int defaultValue)
    {
        string value = GetField(fields, map, column);
        if (string.IsNullOrEmpty(value)) return defaultValue;
        return int.TryParse(value, out int result) ? result : defaultValue;
    }

    private static bool GetBoolField(string[] fields, Dictionary<string, int> map, string column, bool defaultValue)
    {
        string value = GetField(fields, map, column).ToLower();
        if (string.IsNullOrEmpty(value)) return defaultValue;
        return value == "1" || value == "true" || value == "yes";
    }

    private static ShadowEffectType ParseEffectType(string value)
    {
        value = value.Trim().Replace(" ", "").Replace("_", "");
        
        return value.ToLower() switch
        {
            "money" or "деньги" => ShadowEffectType.Money,
            "attack" or "атака" => ShadowEffectType.Attack,
            "capacity" or "грузоподъемность" or "вместимость" => ShadowEffectType.Capacity,
            "bargain" or "торговля" => ShadowEffectType.Bargain,
            "addgoods" or "добавитьтовар" => ShadowEffectType.AddGoods,
            "removegoods" or "удалитьтовар" => ShadowEffectType.RemoveGoods,
            "firecrewmember" or "уволить" or "покинутькоманду" => ShadowEffectType.FireCrewMember,
            "wagepenalty" or "штрафжалованья" => ShadowEffectType.WagePenalty,
            "confiscation" or "конфискация" => ShadowEffectType.Confiscation,
            "teamstats" or "характеристикикоманды" => ShadowEffectType.TeamStats,
            "bonustrade" or "бонусторговли" or "ценытоваров" => ShadowEffectType.BonusTrade,
            _ => ShadowEffectType.Money
        };
    }

    private static string SanitizeFileName(string name)
    {
        foreach (char c in Path.GetInvalidFileNameChars())
            name = name.Replace(c, '_');
        return name.Replace(" ", "_").Replace("-", "_");
    }
}