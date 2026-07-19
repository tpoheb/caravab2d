using System.Collections.Generic;
using System.Globalization;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace Editor
{
    public static class TradeDataImporter
    {
        // ─── Пути ────────────────────────────────────────────────────────────
        private const string BaseDataPath = "Assets/Data";
        private const string ItemsPath    = BaseDataPath + "/Items";
        private const string CitiesPath   = BaseDataPath + "/Cities";

        // ─── Формат CSV ──────────────────────────────────────────────────────
        private const char CsvSeparator       = ',';
        private const int  MinColumnsBasic    = 6;
        private const int  MinColumnsExtended = 10;

        private const int ColCity        = 0;
        private const int ColItem        = 1;
        private const int ColWeight      = 2;
        private const int ColStock       = 3;
        private const int ColBuyPrice    = 4;
        private const int ColSellPrice   = 5;
        private const int ColMinBuyPrice = 6;
        private const int ColMaxBuyPrice = 7;
        private const int ColVolatility  = 8;
        private const int ColRegenRate   = 9;
        private const int ColDemand = 10;

        // ─── Дефолтные значения ──────────────────────────────────────────────
        private const float DefaultPriceRangeMin = 0.5f;
        private const float DefaultPriceRangeMax = 2.0f;
        private const float DefaultVolatility    = 0.02f;
        private const float DefaultRegenRate     = 0.10f;
        private const int   DefaultWeight        = 1;
        private const int   DefaultStock         = 0;

        [MenuItem("Tools/Trade/Import Trade Data")]
        public static void ImportTradeData()
        {
            Debug.Log("=== НАЧАЛО ИМПОРТА ===");

            string csvPath = EditorUtility.OpenFilePanel("Выберите CSV файл", "", "csv");
            if (string.IsNullOrEmpty(csvPath))
            {
                Debug.Log("Импорт отменён: файл не выбран");
                return;
            }

            string[] lines = File.ReadAllLines(csvPath);
            if (lines.Length < 2)
            {
                Debug.LogError("Ошибка: CSV должен содержать заголовок и данные");
                return;
            }

            // Создаем папки напрямую через системный IO ДО блокировки ассетов
            EnsureDirectoryExists(BaseDataPath);
            EnsureDirectoryExists(ItemsPath);
            EnsureDirectoryExists(CitiesPath);
            AssetDatabase.Refresh();

            var itemByName = new Dictionary<string, Item>();
            var cityByName = new Dictionary<string, CityData>();
            
            int processedRows = 0;
            int skippedRows   = 0;

            AssetDatabase.StartAssetEditing();
            try
            {
                // ── Шаг 1: создаём / обновляем Item assets ───────────────────
                for (int i = 1; i < lines.Length; i++)
                {
                    string line = lines[i];
                    if (string.IsNullOrWhiteSpace(line)) continue;

                    string[] parts = line.Split(CsvSeparator);
                    if (parts.Length < MinColumnsBasic) continue;

                    string itemName = SanitizeName(parts[ColItem]);
                    if (string.IsNullOrEmpty(itemName) || itemByName.ContainsKey(itemName)) continue;

                    string itemPath = $"{ItemsPath}/{itemName}.asset";
                    var item = AssetDatabase.LoadAssetAtPath<Item>(itemPath);

                    if (item == null)
                    {
                        item          = ScriptableObject.CreateInstance<Item>();
                        item.itemName = itemName;
                        item.weight   = TryParseInt(parts[ColWeight], DefaultWeight);
                        AssetDatabase.CreateAsset(item, itemPath);
                    }
                    else
                    {
                        item.weight = TryParseInt(parts[ColWeight], DefaultWeight);
                        EditorUtility.SetDirty(item);
                    }

                    itemByName[itemName] = item;
                }

                // ── Шаг 2: создаём / обновляем города и заполняем товарами ───
                for (int i = 1; i < lines.Length; i++)
                {
                    string line = lines[i];
                    if (string.IsNullOrWhiteSpace(line)) continue;

                    string[] parts = line.Split(CsvSeparator);

                    if (parts.Length < MinColumnsBasic)
                    {
                        skippedRows++;
                        continue;
                    }

                    string cityName = SanitizeName(parts[ColCity]);
                    string itemName = SanitizeName(parts[ColItem]);

                    if (string.IsNullOrEmpty(cityName) || string.IsNullOrEmpty(itemName) || !itemByName.TryGetValue(itemName, out Item item))
                    {
                        skippedRows++;
                        continue;
                    }

                    // Загружаем город. Если его нет в cityByName, значит в этом цикле импорта мы видим его впервые.
                    if (!cityByName.TryGetValue(cityName, out CityData city))
                    {
                        string cityPath = $"{CitiesPath}/{cityName}.asset";
                        city = AssetDatabase.LoadAssetAtPath<CityData>(cityPath);

                        if (city == null)
                        {
                            // Города не существует, создаем с нуля
                            city          = ScriptableObject.CreateInstance<CityData>();
                            city.cityName = cityName;
                            city.items    = new List<CityData.CityItem>();
                            AssetDatabase.CreateAsset(city, cityPath);
                        }
                        else
                        {
                            // Город существует. ОЧИЩАЕМ его, а не удаляем сам файл.
                            city.items.Clear();
                            EditorUtility.SetDirty(city);
                        }

                        cityByName[cityName] = city;
                    }

                    // Собираем данные товара
                    float buyPrice  = TryParseFloat(parts[ColBuyPrice], 10f);
                    float sellPrice = TryParseFloat(parts[ColSellPrice], 8f);

                    var cityItem = new CityData.CityItem { item = item };
                    cityItem.stock            = TryParseInt(parts[ColStock], DefaultStock);
                    cityItem.baseBuyPrice     = buyPrice;
                    cityItem.currentBuyPrice  = buyPrice;
                    cityItem.baseSellPrice    = sellPrice;
                    cityItem.currentSellPrice = sellPrice;

                    if (parts.Length >= MinColumnsExtended)
                    {
                        cityItem.minBuyPrice = TryParseFloat(parts[ColMinBuyPrice], buyPrice * DefaultPriceRangeMin);
                        cityItem.maxBuyPrice = TryParseFloat(parts[ColMaxBuyPrice], buyPrice * DefaultPriceRangeMax);

                        float spread = buyPrice > 0 ? sellPrice / buyPrice : DefaultPriceRangeMin;
                        cityItem.minSellPrice = cityItem.minBuyPrice * spread;
                        cityItem.maxSellPrice = cityItem.maxBuyPrice * spread;

                        cityItem.volatility = TryParseFloat(parts[ColVolatility], DefaultVolatility);
                        cityItem.regenRate = parts.Length > ColRegenRate
                            ? TryParseFloat(parts[ColRegenRate], DefaultRegenRate)
                            : DefaultRegenRate;

                        if (parts.Length > ColDemand)
                        {
                            string demandStr = parts[ColDemand].Trim().ToLowerInvariant();
                            cityItem.demand = demandStr switch
                            {
                                "high" or "1" or "высокий" => DemandLevel.High,
                                "low" or "-1" or "низкий" => DemandLevel.Low,
                                _ => DemandLevel.Normal
                            };
                        }
                        
                    }
                    else
                    {
                        cityItem.minBuyPrice  = Mathf.Max(1f, buyPrice * DefaultPriceRangeMin);
                        cityItem.maxBuyPrice  = buyPrice * DefaultPriceRangeMax;
                        cityItem.minSellPrice = Mathf.Max(1f, sellPrice * DefaultPriceRangeMin);
                        cityItem.maxSellPrice = sellPrice * DefaultPriceRangeMax;
                        cityItem.volatility   = DefaultVolatility;
                        cityItem.regenRate    = DefaultRegenRate;
                    }

                    city.items.Add(cityItem);
                    EditorUtility.SetDirty(city);
                    processedRows++;
                }
            }
            finally
            {
                AssetDatabase.StopAssetEditing();
            }

            AssetDatabase.SaveAssets();

            Debug.Log("=== РЕЗУЛЬТАТ ===");
            Debug.Log($"Обработано строк: {processedRows}");
            Debug.Log($"Пропущено строк:  {skippedRows}");
            Debug.Log($"Товаров:          {itemByName.Count}");
            Debug.Log($"Городов:          {cityByName.Count}");
            Debug.Log("Импорт завершён успешно!");
        }

        // ─── Вспомогательные методы ──────────────────────────────────────────

        private static void EnsureDirectoryExists(string unityPath)
        {
            // Переводим "Assets/..." в абсолютный путь ОС для безопасного создания
            string absolutePath = Path.Combine(Application.dataPath, unityPath.Substring(7));
            if (!Directory.Exists(absolutePath))
            {
                Directory.CreateDirectory(absolutePath);
            }
        }

        private static string SanitizeName(string value)
        {
            string clean = value.Trim().Trim('"').Trim('\'').Trim();
            return string.Join("", clean.Split(Path.GetInvalidFileNameChars()));
        }

        private static int TryParseInt(string value, int defaultValue) =>
            int.TryParse(value.Trim(), out int result) ? result : defaultValue;

        private static float TryParseFloat(string value, float defaultValue)
        {
            string normalized = value.Replace(',', '.').Trim();
            return float.TryParse(
                normalized,
                NumberStyles.Float,
                CultureInfo.InvariantCulture,
                out float result
            ) ? result : defaultValue;
        }
    }
}