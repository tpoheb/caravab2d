using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace Editor
{
    public static class TradeDataImporter
    {
        private const string BaseDataPath = "Assets/Data";
        private const string ItemsPath = BaseDataPath + "/Items";
        private const string CitiesPath = BaseDataPath + "/Cities";

        [MenuItem("Trade/Import Trade Data")]
        public static void ImportTradeData()
        {
            Debug.Log("=== НАЧАЛО ИМПОРТА ===");
        
            // 1. Выбор файла
            string csvPath = EditorUtility.OpenFilePanel("Выберите CSV файл", "", "csv");
            if (string.IsNullOrEmpty(csvPath)) 
            {
                Debug.Log("Импорт отменен: файл не выбран");
                return;
            }
            Debug.Log($"Выбран файл: {csvPath}");

            // 2. Проверка и создание папок
            EnsureDirectoryExists(BaseDataPath);
            EnsureDirectoryExists(ItemsPath);
            EnsureDirectoryExists(CitiesPath);
        
            // 3. Загрузка существующих данных
            var allCities = LoadAllAssets<CityData>("Cities");
            var allItems = LoadAllAssets<Item>("Items");
        
            Debug.Log($"Найдено: {allCities.Count} городов, {allItems.Count} товаров");

            // 4. Чтение CSV
            var lines = File.ReadAllLines(csvPath);
            if (lines.Length < 2)
            {
                Debug.LogError("Ошибка: CSV должен содержать заголовок и данные");
                return;
            }

            // 5. Обработка данных
            int createdItems = 0;
            int updatedCities = 0;
        
            for (int i = 1; i < lines.Length; i++)
            {
                string line = lines[i];
                if (string.IsNullOrWhiteSpace(line)) continue;

                string[] parts = line.Split('\t');
                if (parts.Length < 6)
                {
                    Debug.LogWarning($"Пропуск строки {i}: неверный формат");
                    continue;
                }

                string cityName = parts[0].Trim();
                string itemName = parts[1].Trim();

                // Создание/поиск города
                var city = allCities.FirstOrDefault(c => c.cityName == cityName);
                if (city == null)
                {
                    Debug.Log($"Создание нового города: {cityName}");
                    city = ScriptableObject.CreateInstance<CityData>();
                    city.cityName = cityName;
                    city.items = new List<CityData.CityItem>();
                    AssetDatabase.CreateAsset(city, $"{CitiesPath}/{cityName}.asset");
                    allCities.Add(city);
                }

                // Создание/поиск товара
                var item = allItems.FirstOrDefault(it => it.itemName == itemName);
                if (item == null)
                {
                    Debug.Log($"Создание нового товара: {itemName}");
                    item = ScriptableObject.CreateInstance<Item>();
                    item.itemName = itemName;
                    item.weight = TryParseInt(parts[2], 1);
                    AssetDatabase.CreateAsset(item, $"{ItemsPath}/{itemName}.asset");
                    allItems.Add(item);
                    createdItems++;
                }

                // Добавление товара в город
                var cityItem = city.items.FirstOrDefault(ci => ci.item == item);
                if (cityItem == null)
                {
                    cityItem = new CityData.CityItem { item = item };
                    city.items.Add(cityItem);
                }

                cityItem.stock = TryParseInt(parts[3], 0);
                cityItem.buyPrice = TryParseInt(parts[4], 0);
                cityItem.sellPrice = TryParseInt(parts[5], 0);

                EditorUtility.SetDirty(city);
                updatedCities++;
            }

            // 6. Сохранение
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
        
            Debug.Log("=== РЕЗУЛЬТАТ ===");
            Debug.Log($"Создано товаров: {createdItems}");
            Debug.Log($"Обновлено городов: {updatedCities}");
            Debug.Log("Импорт завершен!");
        }

        private static List<T> LoadAllAssets<T>(string folder) where T : UnityEngine.Object
        {
            string[] guids = AssetDatabase.FindAssets($"t:{typeof(T).Name}", new[] { $"{BaseDataPath}/{folder}" });
            return guids.Select(guid => 
                AssetDatabase.LoadAssetAtPath<T>(AssetDatabase.GUIDToAssetPath(guid))).ToList();
        }

        private static void EnsureDirectoryExists(string path)
        {
            if (!AssetDatabase.IsValidFolder(path))
            {
                string parent = Path.GetDirectoryName(path);
                string folder = Path.GetFileName(path);
                AssetDatabase.CreateFolder(parent, folder);
                Debug.Log($"Создана папка: {path}");
            }
        }

        private static int TryParseInt(string value, int defaultValue)
        {
            return int.TryParse(value, out int result) ? result : defaultValue;
        }

        private static float TryParseFloat(string value, float defaultValue)
        {
            return float.TryParse(value, out float result) ? result : defaultValue;
        }
    }
}