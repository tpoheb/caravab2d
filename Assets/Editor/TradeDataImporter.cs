using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
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
        private const char   CsvSeparator      = ',';
        private const int    MinColumnsBasic    = 6;   // City,Item,Weight,Stock,BuyPrice,SellPrice
        private const int    MinColumnsExtended = 9;   // + MinBuy,MaxBuy,Volatility[,RegenRate]

        // Индексы колонок
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

        // ─── Дефолтные значения для расширенного формата ────────────────────
        private const float DefaultPriceRangeMin  = 0.5f;   // минимум = base × 0.5
        private const float DefaultPriceRangeMax  = 2.0f;   // максимум = base × 2.0
        private const float DefaultVolatility     = 0.02f;
        private const float DefaultRegenRate      = 0.10f;
        private const int   DefaultWeight         = 1;
        private const int   DefaultStock          = 0;

        // ─── Импорт ──────────────────────────────────────────────────────────
        [MenuItem("Trade/Import Trade Data")]
        public static void ImportTradeData()
        {
            Debug.Log("=== НАЧАЛО ИМПОРТА ===");

            string csvPath = EditorUtility.OpenFilePanel("Выберите CSV файл", "", "csv");
            if (string.IsNullOrEmpty(csvPath))
            {
                Debug.Log("Импорт отменён: файл не выбран");
                return;
            }
            Debug.Log($"Выбран файл: {csvPath}");

            EnsureDirectoryExists(BaseDataPath);
            EnsureDirectoryExists(ItemsPath);
            EnsureDirectoryExists(CitiesPath);

            var allCities = LoadAllAssets<CityData>("Cities");
            var allItems  = LoadAllAssets<Item>("Items");
            Debug.Log($"Найдено: {allCities.Count} городов, {allItems.Count} товаров");

            string[] lines = File.ReadAllLines(csvPath);
            if (lines.Length < 2)
            {
                Debug.LogError("Ошибка: CSV должен содержать заголовок и данные");
                return;
            }

            int createdItems    = 0;
            int createdCities   = 0;
            int processedRows   = 0;
            int skippedRows     = 0;
            var updatedCitySet  = new HashSet<string>();

            for (int i = 1; i < lines.Length; i++)
            {
                string line = lines[i];
                if (string.IsNullOrWhiteSpace(line)) continue;

                string[] parts = line.Split(CsvSeparator);

                if (parts.Length < MinColumnsBasic)
                {
                    Debug.LogWarning($"Строка {i + 1}: пропуск — нужно минимум {MinColumnsBasic} колонок, найдено {parts.Length}");
                    skippedRows++;
                    continue;
                }

                string cityName = parts[ColCity].Trim();
                string itemName = parts[ColItem].Trim();

                if (string.IsNullOrEmpty(cityName) || string.IsNullOrEmpty(itemName))
                {
                    Debug.LogWarning($"Строка {i + 1}: пропуск — пустое название города или товара");
                    skippedRows++;
                    continue;
                }

                float buyPrice  = TryParseFloat(parts[ColBuyPrice],  10f);
                float sellPrice = TryParseFloat(parts[ColSellPrice],  8f);



                // Город
                var city = allCities.FirstOrDefault(c => c.cityName == cityName);
                if (city == null)
                {
                    city = ScriptableObject.CreateInstance<CityData>();
                    city.cityName = cityName;
                    city.items    = new List<CityData.CityItem>();
                    AssetDatabase.CreateAsset(city, $"{CitiesPath}/{cityName}.asset");
                    allCities.Add(city);
                    createdCities++;
                    Debug.Log($"Создан город: {cityName}");
                }

                // Товар
                var item = allItems.FirstOrDefault(it => it.itemName == itemName);
                if (item == null)
                {
                    item = ScriptableObject.CreateInstance<Item>();
                    item.itemName = itemName;
                    item.weight   = TryParseInt(parts[ColWeight], DefaultWeight);
                    AssetDatabase.CreateAsset(item, $"{ItemsPath}/{itemName}.asset");
                    allItems.Add(item);
                    createdItems++;
                    Debug.Log($"Создан товар: {itemName}");
                }

                // Запись в город
                var cityItem = city.items.FirstOrDefault(ci => ci.item == item);
                if (cityItem == null)
                {
                    cityItem = new CityData.CityItem { item = item };
                    city.items.Add(cityItem);
                }

                cityItem.stock = TryParseInt(parts[ColStock], DefaultStock);

                // Цены покупки
                cityItem.baseBuyPrice    = buyPrice;
                cityItem.currentBuyPrice = buyPrice;

                // Цены продажи
                cityItem.baseSellPrice    = sellPrice;
                cityItem.currentSellPrice = sellPrice;

                // Расширенный формат
                if (parts.Length >= MinColumnsExtended)
                {
                    cityItem.minBuyPrice = TryParseFloat(parts[ColMinBuyPrice], buyPrice  * DefaultPriceRangeMin);
                    cityItem.maxBuyPrice = TryParseFloat(parts[ColMaxBuyPrice], buyPrice  * DefaultPriceRangeMax);

                    float spreadRatio      = buyPrice > 0 ? sellPrice / buyPrice : DefaultPriceRangeMin;
                    cityItem.minSellPrice  = cityItem.minBuyPrice * spreadRatio;
                    cityItem.maxSellPrice  = cityItem.maxBuyPrice * spreadRatio;

                    cityItem.volatility = TryParseFloat(parts[ColVolatility], DefaultVolatility);
                    cityItem.regenRate  = parts.Length > ColRegenRate
                        ? TryParseFloat(parts[ColRegenRate], DefaultRegenRate)
                        : DefaultRegenRate;
                }
                else
                {
                    cityItem.minBuyPrice  = Mathf.Max(1f, buyPrice  * DefaultPriceRangeMin);
                    cityItem.maxBuyPrice  = buyPrice  * DefaultPriceRangeMax;
                    cityItem.minSellPrice = Mathf.Max(1f, sellPrice * DefaultPriceRangeMin);
                    cityItem.maxSellPrice = sellPrice * DefaultPriceRangeMax;
                    cityItem.volatility   = DefaultVolatility;
                    cityItem.regenRate    = DefaultRegenRate;
                }

                EditorUtility.SetDirty(city);
                updatedCitySet.Add(cityName);
                processedRows++;
            }

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            Debug.Log("=== РЕЗУЛЬТАТ ===");
            Debug.Log($"Обработано строк:  {processedRows}");
            Debug.Log($"Пропущено строк:   {skippedRows}");
            Debug.Log($"Создано городов:   {createdCities}");
            Debug.Log($"Создано товаров:   {createdItems}");
            Debug.Log($"Обновлено городов: {updatedCitySet.Count}");
            Debug.Log("Импорт завершён!");
        }

        // ─── Вспомогательные методы ──────────────────────────────────────────
        private static List<T> LoadAllAssets<T>(string folder) where T : Object
        {
            string[] guids = AssetDatabase.FindAssets(
                $"t:{typeof(T).Name}",
                new[] { $"{BaseDataPath}/{folder}" }
            );
            return guids
                .Select(guid => AssetDatabase.LoadAssetAtPath<T>(AssetDatabase.GUIDToAssetPath(guid)))
                .Where(asset => asset != null)
                .ToList();
        }

        private static void EnsureDirectoryExists(string path)
        {
            if (AssetDatabase.IsValidFolder(path)) return;
            string parent = Path.GetDirectoryName(path);
            string folder = Path.GetFileName(path);
            AssetDatabase.CreateFolder(parent, folder);
            Debug.Log($"Создана папка: {path}");
        }

        private static int TryParseInt(string value, int defaultValue) =>
            int.TryParse(value.Trim(), out int result) ? result : defaultValue;

        private static float TryParseFloat(string value, float defaultValue)
        {
            string normalized = value.Replace(',', '.').Trim();
            return float.TryParse(normalized, NumberStyles.Float, CultureInfo.InvariantCulture, out float result)
                ? result
                : defaultValue;
        }
    }
}