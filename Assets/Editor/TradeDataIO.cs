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
                    Debug.LogWarning($"Пропуск строки {i}: неверный формат (найдено {parts.Length} колонок, нужно минимум 6)");
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
                    city.cityGold = 1000; // Начальное золото города
                    city.items = new List<CityData.CityItem>();
                    string cityAssetPath = $"{CitiesPath}/{cityName}.asset";
                    AssetDatabase.CreateAsset(city, cityAssetPath);
                    allCities.Add(city);
                }

                // Создание/поиск товара
                var item = allItems.FirstOrDefault(it => it.name == itemName);
                if (item == null)
                {
                    Debug.Log($"Создание нового товара: {itemName}");
                    item = ScriptableObject.CreateInstance<Item>();
                    item.name = itemName;
                    item.itemName = itemName;
                    item.weight = TryParseInt(parts[2], 1);
                    string itemAssetPath = $"{ItemsPath}/{itemName}.asset";
                    AssetDatabase.CreateAsset(item, itemAssetPath);
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
            string folderPath = $"{BaseDataPath}/{folder}";
            if (!AssetDatabase.IsValidFolder(folderPath))
            {
                Debug.Log($"Папка {folderPath} не существует");
                return new List<T>();
            }
            
            string[] guids = AssetDatabase.FindAssets($"t:{typeof(T).Name}", new[] { folderPath });
            var assets = new List<T>();
            
            foreach (string guid in guids)
            {
                string assetPath = AssetDatabase.GUIDToAssetPath(guid);
                T asset = AssetDatabase.LoadAssetAtPath<T>(assetPath);
                if (asset != null)
                {
                    assets.Add(asset);
                }
            }
            
            return assets;
        }

        private static void EnsureDirectoryExists(string path)
        {
            if (!AssetDatabase.IsValidFolder(path))
            {
                string parentFolder = Path.GetDirectoryName(path).Replace("\\", "/");
                string folderName = Path.GetFileName(path);
                
                // Убедимся, что родительская папка существует
                if (!AssetDatabase.IsValidFolder(parentFolder))
                {
                    string grandParent = Path.GetDirectoryName(parentFolder).Replace("\\", "/");
                    string parentName = Path.GetFileName(parentFolder);
                    if (!AssetDatabase.IsValidFolder(grandParent))
                    {
                        EnsureDirectoryExists(grandParent);
                    }
                    AssetDatabase.CreateFolder(grandParent, parentName);
                    Debug.Log($"Создана папка: {parentFolder}");
                }
                
                string parent = Path.GetDirectoryName(path).Replace("\\", "/");
                AssetDatabase.CreateFolder(parent, folderName);
                Debug.Log($"Создана папка: {path}");
            }
        }

        private static int TryParseInt(string value, int defaultValue)
        {
            if (string.IsNullOrEmpty(value))
                return defaultValue;
                
            // Убираем пробелы и специальные символы
            value = value.Trim();
            if (int.TryParse(value, out int result))
                return result;
                
            return defaultValue;
        }

        private static float TryParseFloat(string value, float defaultValue)
        {
            if (string.IsNullOrEmpty(value))
                return defaultValue;
                
            value = value.Trim();
            if (float.TryParse(value, out float result))
                return result;
                
            return defaultValue;
        }
    }
}