using UnityEngine;
using UnityEditor;
using System.IO;
using System.Linq;
using System.Text;

public static class TradeDataIO
{
    private const string ItemsPath = "Assets/Data/Items";
    private const string CitiesPath = "Assets/Data/Cities";

    [MenuItem("Trade/Export to CSV")]
    public static void ExportToCsv()
    {
        try
        {
            string path = EditorUtility.SaveFilePanel("Export CSV", "", "trade_data", "csv");
            if (string.IsNullOrEmpty(path))
            {
                Debug.Log("Export cancelled by user");
                return;
            }

            var cities = Resources.FindObjectsOfTypeAll<CityData>();
            if (cities == null || cities.Length == 0)
            {
                Debug.LogError("No cities found for export!");
                return;
            }

            var stringBuilder = new StringBuilder();
            // New header matching the import format
            stringBuilder.AppendLine("City\tProductID\tProductName\tWeight\tStock\tBuyPrice\tSellPrice");

            int exportedRecords = 0;
            foreach (var city in cities)
            {
                if (city.items == null || city.items.Count == 0)
                {
                    Debug.LogWarning($"City {city.cityName} has no items");
                    continue;
                }

                foreach (var cityItem in city.items)
                {
                    if (cityItem.item == null)
                    {
                        Debug.LogWarning($"Empty item found in city {city.cityName}");
                        continue;
                    }

                    stringBuilder.AppendLine(
                        $"{city.cityName}\t" +
                        $"{cityItem.item.name}\t" + // Using name as ID if separate ID field doesn't exist
                        $"{cityItem.item.itemName}\t" +
                        $"{cityItem.item.weight}\t" +
                        $"{cityItem.stock}\t" +
                        $"{cityItem.buyPrice}\t" +
                        $"{cityItem.sellPrice}");

                    exportedRecords++;
                }
            }

            File.WriteAllText(path, stringBuilder.ToString());
            Debug.Log($"Successfully exported {exportedRecords} records to {path}");
        }
        catch (System.Exception e)
        {
            Debug.LogError($"Export error: {e.Message}\n{e.StackTrace}");
        }
        finally
        {
            AssetDatabase.Refresh();
        }
    }

    [MenuItem("Trade/Import from CSV")]
    public static void ImportFromCsv()
    {
        try
        {
            string path = EditorUtility.OpenFilePanel("Import CSV", "", "csv");
            if (string.IsNullOrEmpty(path))
            {
                Debug.Log("Import cancelled by user");
                return;
            }

            if (!File.Exists(path))
            {
                Debug.LogError($"File not found: {path}");
                return;
            }

            var lines = File.ReadAllLines(path);
            if (lines.Length <= 1)
            {
                Debug.LogError("CSV file is empty or contains only header");
                return;
            }

            EnsureDirectoryExists(ItemsPath);
            EnsureDirectoryExists(CitiesPath);

            var existingItems = Resources.LoadAll<Item>("");
            var existingCities = Resources.LoadAll<CityData>("");

            int importedRecords = 0;
            int skippedRecords = 0;

            // Parse header to check format
            var header = lines[0].Split('\t');
            if (header.Length < 7 || header[0] != "City" || header[1] != "ProductID")
            {
                Debug.LogError("Invalid CSV format. Expected: City\tProductID\tProductName\tWeight\tStock\tBuyPrice\tSellPrice");
                return;
            }

            for (int i = 1; i < lines.Length; i++)
            {
                string line = lines[i];
                if (string.IsNullOrWhiteSpace(line))
                {
                    skippedRecords++;
                    continue;
                }

                var parts = line.Split('\t');
                if (parts.Length < 7)
                {
                    Debug.LogWarning($"Invalid line #{i}: '{line}' - skipped");
                    skippedRecords++;
                    continue;
                }

                if (!ProcessImportRecord(parts, existingItems, existingCities, out string error))
                {
                    Debug.LogWarning($"Error processing line #{i}: {error}");
                    skippedRecords++;
                    continue;
                }

                importedRecords++;
            }

            AssetDatabase.SaveAssets();
            Debug.Log($"Import completed. Success: {importedRecords}, Skipped: {skippedRecords}");
        }
        catch (System.Exception e)
        {
            Debug.LogError($"Import error: {e.Message}\n{e.StackTrace}");
        }
        finally
        {
            AssetDatabase.Refresh();
        }
    }

    private static bool ProcessImportRecord(string[] parts, Item[] existingItems, CityData[] existingCities, out string error)
    {
        error = string.Empty;
        
        string cityName = parts[0].Trim();
        string productId = parts[1].Trim();
        string productName = parts[2].Trim();
        
        if (!float.TryParse(parts[3], out float weight))
        {
            error = $"Invalid Weight: {parts[3]}";
            return false;
        }
        
        if (!int.TryParse(parts[4], out int stock))
        {
            error = $"Invalid Stock: {parts[4]}";
            return false;
        }
        
        if (!int.TryParse(parts[5], out int buyPrice))
        {
            error = $"Invalid BuyPrice: {parts[5]}";
            return false;
        }
        
        if (!int.TryParse(parts[6], out int sellPrice))
        {
            error = $"Invalid SellPrice: {parts[6]}";
            return false;
        }

        // Find or create city
        var city = existingCities.FirstOrDefault(c => c.cityName == cityName);
        if (city == null)
        {
            city = ScriptableObject.CreateInstance<CityData>();
            city.cityName = cityName;
            city.cityGold = 1000; // Default value
            AssetDatabase.CreateAsset(city, $"{CitiesPath}/{cityName}.asset");
            Debug.Log($"Created new city: {cityName}");
        }

        // Find or create item
        var item = existingItems.FirstOrDefault(i => i.name == productId || i.itemName == productName);
        if (item == null)
        {
            item = ScriptableObject.CreateInstance<Item>();
            item.name = productId; // Unity object name
            item.itemName = productName;
            item.weight = weight;
            AssetDatabase.CreateAsset(item, $"{ItemsPath}/{productId}.asset");
            Debug.Log($"Created new item: {productName} (ID: {productId})");
        }
        else
        {
            // Update existing item properties
            item.itemName = productName;
            item.weight = weight;
            EditorUtility.SetDirty(item);
        }

        // Find or create city item entry
        var cityItem = city.items.FirstOrDefault(i => i.item == item);
        if (cityItem == null)
        {
            cityItem = new CityData.CityItem { item = item };
            city.items.Add(cityItem);
        }

        cityItem.stock = stock;
        cityItem.buyPrice = buyPrice;
        cityItem.sellPrice = sellPrice;

        EditorUtility.SetDirty(city);
        return true;
    }

    private static void EnsureDirectoryExists(string path)
    {
        if (!AssetDatabase.IsValidFolder(path))
        {
            string parent = Path.GetDirectoryName(path);
            string folder = Path.GetFileName(path);
            AssetDatabase.CreateFolder(parent, folder);
        }
    }
}