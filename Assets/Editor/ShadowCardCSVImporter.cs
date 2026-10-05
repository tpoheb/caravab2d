using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;
using UnityEditor;

/// <summary>
/// Импортёр карт тени из CSV.
///
/// Меню: Tools → Cards → Import Shadow Cards from CSV
///
/// ─── ФОРМАТ CSV ──────────────────────────────────────────────────────────────
/// Разделитель: табуляция (\t) или запятая (,). Первая строка — заголовок.
///
/// Колонка        Тип      Обяз.  Описание и примеры
/// ─────────────────────────────────────────────────────────────────────────────
/// ID             int      ДА     Уникальный числовой идентификатор карты.
///                                Используется в имени .asset файла.
///                                Пример: 1, 42, 100
///
/// Name           string   ДА     Название карты, отображается игроку.
///                                Пример: «Ограбление каравана», «Налог на торговлю»
///
/// Description    string   ДА     Текст события, показывается в диалоге карты.
///                                Может содержать запятые — оберни в кавычки.
///                                Пример: «Бандиты напали на ваш отряд ночью.»
///
/// System         string   нет    Зарезервировано. Можно оставить пустым.
///
/// Intensity      int      нет    Зарезервировано. Можно оставить пустым.
///
/// Tone           string   нет    Зарезервировано. Можно оставить пустым.
///
/// EffectType     string   ДА     Тип эффекта карты. Определяет, что произойдёт
///                                при розыгрыше. Допустимые значения:
///
///                                  Money           / Деньги          — изменение золота
///                                  Attack          / Атака           — изменение атаки каравана
///                                  Capacity        / Грузоподъемность — изменение вместимости
///                                  Bargain         / Торговля        — бонус/штраф к торговле
///                                  AddGoods        / ДобавитьТовар   — добавить товар в инвентарь
///                                  RemoveGoods     / УдалитьТовар    — удалить товар из инвентаря
///                                  FireCrewMember  / Уволить         — уволить члена команды
///                                  WagePenalty     / ШтрафЖалованья — штраф к зарплате команды
///                                  Confiscation    / Конфискация     — конфисковать случайный товар
///                                  TeamStats       / ХарактеристикиКоманды — изменить стат команды
///                                  BonusTrade      / БонусТорговли   — бонус к ценам товаров
///
///                                Пример: Money, Attack, AddGoods
///
/// Value          int      нет    Числовое значение эффекта. Знак важен:
///                                  +200  — игрок получает 200 золота
///                                  -150  — игрок теряет 150 золота
///                                  +2    — атака увеличивается на 2
///                                По умолчанию: 0
///
/// IsTemporary    bool     нет    Временный ли эффект (снимается через N ходов).
///                                Допустимые значения: true/false, 1/0, yes/no
///                                По умолчанию: false
///
/// Duration       int      нет    Сколько ходов действует временный эффект.
///                                Игнорируется если IsTemporary = false.
///                                По умолчанию: 1
///                                Пример: 3 (эффект продлится 3 хода)
///
/// MinDifficulty  int      нет    Минимальная сложность кампании, при которой
///                                карта может выпасть. 0 — любая сложность.
///                                По умолчанию: 0
///
/// MaxDifficulty  int      нет    Максимальная сложность кампании, при которой
///                                карта может выпасть. 10 — любая сложность.
///                                По умолчанию: 10
///
/// Weight         int      нет    Вес карты в пуле розыгрыша. Чем больше —
///                                тем чаще выпадает относительно других карт.
///                                По умолчанию: 10
///                                Пример: 1 (редкая), 10 (обычная), 30 (частая)
///
/// PenaltyValue   int      нет    Базовый штраф в золоте для эффектов конфискации
///                                и подобных. Используется как fallback если
///                                Value не задан или равен 0.
///                                По умолчанию: 200
/// ─────────────────────────────────────────────────────────────────────────────
///
/// Пример строки:
/// 1   Ограбление   Бандиты напали ночью.      Money   -200   false   0   0   10   10   200
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
                card.cardID       = id;
                card.cardName     = GetField(fields, columnMap, "name");
                card.description  = GetField(fields, columnMap, "description");
                card.effectType   = ParseEffectType(GetField(fields, columnMap, "effecttype"));
                card.value        = GetIntField(fields, columnMap, "value", 0);
                card.isTemporary  = GetBoolField(fields, columnMap, "istemporary", false);
                card.duration     = GetIntField(fields, columnMap, "duration", 1);
                card.minDifficulty = GetIntField(fields, columnMap, "mindifficulty", 0);
                card.maxDifficulty = GetIntField(fields, columnMap, "maxdifficulty", 10);
                card.weight       = GetIntField(fields, columnMap, "weight", 10);
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
            "money"          or "деньги"                   => ShadowEffectType.Money,
            "attack"         or "атака"                    => ShadowEffectType.Attack,
            "capacity"       or "грузоподъемность"
                             or "вместимость"              => ShadowEffectType.Capacity,
            "bargain"        or "торговля"                 => ShadowEffectType.Bargain,
            "addgoods"       or "добавитьтовар"            => ShadowEffectType.AddGoods,
            "removegoods"    or "удалитьтовар"             => ShadowEffectType.RemoveGoods,
            "firecrewmember" or "уволить"
                             or "покинутькоманду"          => ShadowEffectType.FireCrewMember,
            "wagepenalty"    or "штрафжалованья"           => ShadowEffectType.WagePenalty,
            "confiscation"   or "конфискация"              => ShadowEffectType.Confiscation,
            "teamstats"      or "характеристикикоманды"    => ShadowEffectType.TeamStats,
            "bonustrade"     or "бонусторговли"
                             or "ценытоваров"              => ShadowEffectType.BonusTrade,
            _                                              => ShadowEffectType.Money
        };
    }

    private static string SanitizeFileName(string name)
    {
        foreach (char c in Path.GetInvalidFileNameChars())
            name = name.Replace(c, '_');
        return name.Replace(" ", "_").Replace("-", "_");
    }
}