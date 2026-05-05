using UnityEngine;
using UnityEditor; // Необходимо для доступа к функционалу редактора
using System.IO;   // Для работы с файлами
using System.Linq; // Для работы с LINQ (разделение строк)

public class BattleCardImporter : EditorWindow
{
    private string csvFilePath = "Assets/Data/BattleCards.csv";
    private string soSavePath = "Assets/Data/BattleCards/";

    [MenuItem("Tools/Battle Cards/Import from CSV")]
    public static void ShowWindow()
    {
        GetWindow<BattleCardImporter>("Card Importer");
    }

    private void OnGUI()
    {
        GUILayout.Label("Импорт данных карт битвы", EditorStyles.boldLabel);
        
        csvFilePath = EditorGUILayout.TextField("Путь к CSV:", csvFilePath);
        soSavePath = EditorGUILayout.TextField("Путь сохранения SO:", soSavePath);

        if (GUILayout.Button("Импортировать/Обновить Карты"))
        {
            ImportData();
        }
    }

    private void ImportData()
    {
        if (!File.Exists(csvFilePath))
        {
            Debug.LogError($"Файл не найден по пути: {csvFilePath}");
            return;
        }

        string[] lines = File.ReadAllLines(csvFilePath);

        // Пропускаем первую строку (заголовки)
        for (int i = 1; i < lines.Length; i++)
        {
            // 1. Убираем лишние пробелы по краям самой строки
            string line = lines[i].Trim();
        
            // 2. Защита от пустых строк в конце файла
            if (string.IsNullOrEmpty(line)) continue;

            string[] values = line.Split(','); 

            if (values.Length < 5) 
            {
                Debug.LogWarning($"[Строка {i + 1}] Пропущена: Недостаточно данных. Текст строки: '{line}'");
                continue;
            }

            // 3. Безопасный парсинг ID
            if (!int.TryParse(values[0].Trim(), out int id))
            {
                Debug.LogError($"[Строка {i + 1}] Ошибка парсинга ID! Значение: '{values[0]}'");
                continue; // Пропускаем эту карту и идем дальше
            }

            string enemyName = values[1].Trim();
            BattleCardData card = FindOrCreateAsset(id, enemyName);
        
            // 4. Безопасный парсинг остальных характеристик
            if (!int.TryParse(values[2].Trim(), out card.requiredAttack))
                Debug.LogError($"[Строка {i + 1}] Ошибка requiredAttack: '{values[2]}'");

            if (!int.TryParse(values[3].Trim(), out card.rewardMoney))
                Debug.LogError($"[Строка {i + 1}] Ошибка rewardMoney: '{values[3]}'");

            if (!int.TryParse(values[4].Trim(), out card.penaltyMoney))
                Debug.LogError($"[Строка {i + 1}] Ошибка penaltyMoney: '{values[4]}'");

            // Сохранение изменений
            EditorUtility.SetDirty(card);
        }

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log("Импорт карт битвы завершен!");
    }
    
    // Вспомогательный метод для поиска/создания SO
    private BattleCardData FindOrCreateAsset(int id, string name)
    {
        string assetName = $"Card_{id}_{name}.asset";
        string fullPath = Path.Combine(soSavePath, assetName);
        
        // Поиск по пути, чтобы избежать дублирования
        BattleCardData existingCard = AssetDatabase.LoadAssetAtPath<BattleCardData>(fullPath);

        if (existingCard != null)
        {
            Debug.Log($"Обновление карты ID: {id} ({name})");
            return existingCard;
        }
        else
        {
            // Создание нового ассета
            BattleCardData newCard = ScriptableObject.CreateInstance<BattleCardData>();
            newCard.cardID = id;
            newCard.enemyName = name;
            
            // Убедимся, что путь существует
            if (!Directory.Exists(soSavePath))
            {
                Directory.CreateDirectory(soSavePath);
            }
            
            AssetDatabase.CreateAsset(newCard, fullPath);
            Debug.Log($"Создана новая карта ID: {id} ({name})");
            return newCard;
        }
    }
}