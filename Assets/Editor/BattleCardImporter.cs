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
            string line = lines[i];
            // Разделяем строку, используя запятую (,) как разделитель
            string[] values = line.Split(','); 

            if (values.Length < 5) 
            {
                Debug.LogWarning($"Пропущена строка {i}: Недостаточно данных.");
                continue;
            }

            // Создаем или находим существующий ассет по ID
            int id = int.Parse(values[0]);
            string enemyName = values[1];
            
            BattleCardData card = FindOrCreateAsset(id, enemyName);
            
            // Заполнение данных
            card.requiredAttack = int.Parse(values[2]);
            card.rewardMoney = int.Parse(values[3]);
            card.penaltyMoney = int.Parse(values[4]);

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