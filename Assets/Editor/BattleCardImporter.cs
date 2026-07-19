// =============================================================================
// BattleCardImporter.cs
// Инструмент редактора Unity для импорта/обновления карт битвы из CSV-файла.
//
// КАК ИСПОЛЬЗОВАТЬ:
//   1. В меню Unity: Tools → Battle Cards → Import from CSV
//   2. Нажмите "Выбрать CSV..." и укажите файл с данными. Важно! нужно оборачивать двойными кавычками
//   3. При необходимости измените папку сохранения SO (поле "Папка для SO:").
//   4. Нажмите "Импортировать / Обновить карты".
//
// ФОРМАТ CSV (первая строка — заголовок, игнорируется):
//   ID, EnemyName, Description, RequiredAttack, RewardMoney, PenaltyMoney, CrewLoss, RewardHandCardID
//
//   Колонки:
//   - ID              (int)    — уникальный числовой идентификатор карты.
//   - EnemyName       (string) — имя врага / название карты.
//   - Description     (string) — текст описания карты (можно обернуть в кавычки если есть запятые).
//   - RequiredAttack  (int)    — минимальная атака для победы.
//   - RewardMoney     (int)    — золото, получаемое при победе (>= 0).
//   - PenaltyMoney    (int)    — золото, теряемое при поражении (>= 0).
//   - CrewLoss        (int)    — количество случайных членов команды,
//                               теряемых при поражении (>= 0, 0 = потерь нет).
//   - RewardHandCardID (int)   — ID карты руки (HandCardData), выдаваемой
//                               при победе. 0 = карта руки не выдаётся.
//
// ПРИМЕР СТРОК CSV:
//   ID, EnemyName, Description, RequiredAttack, RewardMoney, PenaltyMoney, CrewLoss, RewardHandCardID
//   1, Desert Bandit, Разбойники из пустыни напали на твой караван., 5, 100, 50, 1, 0
//   2, Sand Worm, "Огромный червь, поднявшийся из песка.", 12, 250, 80, 0, 7
//
// ЛОГИКА РАБОТЫ:
//   - Если ScriptableObject с таким ID уже существует по ожидаемому пути —
//     он обновляется (не создаётся дубликат).
//   - Если SO не найден — создаётся новый файл Card_<ID>_<Name>.asset.
//   - Папка сохранения создаётся автоматически, если не существует.
//   - Все ошибки парсинга выводятся в Console с указанием номера строки.
//
// ЗАВИСИМОСТИ:
//   - BattleCardData.cs — ScriptableObject с полями:
//       int cardID, string enemyName, string description, int requiredAttack,
//       int rewardMoney, int penaltyMoney, int crewLoss, int rewardHandCardID
// =============================================================================

using UnityEngine;
using UnityEditor;
using System.IO;

public class BattleCardImporter : EditorWindow
{
    // -------------------------------------------------------------------------
    // Поля окна редактора
    // -------------------------------------------------------------------------

    private string csvFilePath = "";
    private string soSavePath  = "Assets/Data/BattleCards/";

    // Индексы колонок — менять здесь, если структура CSV изменится
    private const int COL_ID              = 0;
    private const int COL_ENEMY_NAME      = 1;
    private const int COL_DESCRIPTION     = 2;
    private const int COL_REQUIRED_ATTACK = 3;
    private const int COL_REWARD_MONEY    = 4;
    private const int COL_PENALTY_MONEY   = 5;
    private const int COL_CREW_LOSS       = 6;
    private const int COL_REWARD_HANDCARD = 7;
    private const int REQUIRED_COLUMNS    = 8;

    // -------------------------------------------------------------------------
    // Открытие окна
    // -------------------------------------------------------------------------

    [MenuItem("Tools/Battle Cards/Import from CSV")]
    public static void ShowWindow()
    {
        GetWindow<BattleCardImporter>("Battle Card Importer");
    }

    // -------------------------------------------------------------------------
    // Интерфейс окна
    // -------------------------------------------------------------------------

    private void OnGUI()
    {
        GUILayout.Label("Импорт карт битвы из CSV", EditorStyles.boldLabel);
        EditorGUILayout.Space();

        EditorGUILayout.LabelField("CSV-файл с данными карт:");
        EditorGUILayout.BeginHorizontal();
        {
            EditorGUI.BeginDisabledGroup(true);
            EditorGUILayout.TextField(
                string.IsNullOrEmpty(csvFilePath) ? "(файл не выбран)" : csvFilePath
            );
            EditorGUI.EndDisabledGroup();

            if (GUILayout.Button("Выбрать CSV...", GUILayout.Width(120)))
            {
                string selected = EditorUtility.OpenFilePanel(
                    title:     "Выберите CSV-файл карт битвы",
                    directory: Application.dataPath,
                    extension: "csv"
                );
                if (!string.IsNullOrEmpty(selected))
                    csvFilePath = selected;
            }
        }
        EditorGUILayout.EndHorizontal();

        EditorGUILayout.Space();

        soSavePath = EditorGUILayout.TextField("Папка для SO:", soSavePath);

        EditorGUILayout.Space();

        EditorGUILayout.HelpBox(
            "Ожидаемые колонки CSV (первая строка — заголовок):\n" +
            "ID | EnemyName | Description | RequiredAttack | RewardMoney | PenaltyMoney | CrewLoss | RewardHandCardID\n\n" +
            "Если Description содержит запятые — оберните поле в двойные кавычки.",
            MessageType.Info
        );

        EditorGUILayout.Space();

        GUI.enabled = !string.IsNullOrEmpty(csvFilePath);
        if (GUILayout.Button("Импортировать / Обновить карты", GUILayout.Height(30)))
        {
            ImportData();
        }
        GUI.enabled = true;
    }

    // -------------------------------------------------------------------------
    // Основная логика импорта
    // -------------------------------------------------------------------------

    private void ImportData()
    {
        if (!File.Exists(csvFilePath))
        {
            Debug.LogError($"[BattleCardImporter] Файл не найден: {csvFilePath}");
            return;
        }

        string[] lines = File.ReadAllLines(csvFilePath);

        if (lines.Length < 2)
        {
            Debug.LogWarning("[BattleCardImporter] CSV-файл пуст или содержит только заголовок.");
            return;
        }

        // Определяем разделитель по заголовочной строке
        char delimiter = DetectDelimiter(lines[0]);

        int imported = 0;
        int skipped  = 0;

        for (int i = 1; i < lines.Length; i++)
        {
            string line = lines[i].Trim();
            if (string.IsNullOrEmpty(line)) continue;

            string[] values = ParseCsvLine(line, delimiter);

            if (values.Length < REQUIRED_COLUMNS)
            {
                Debug.LogWarning(
                    $"[BattleCardImporter] Строка {i + 1}: пропущена — " +
                    $"ожидается {REQUIRED_COLUMNS} колонок, найдено {values.Length}. " +
                    $"Содержимое: '{line}'"
                );
                skipped++;
                continue;
            }

            // --- ID ---
            if (!int.TryParse(values[COL_ID].Trim(), out int id))
            {
                Debug.LogError(
                    $"[BattleCardImporter] Строка {i + 1}: не удалось распарсить ID. " +
                    $"Значение: '{values[COL_ID]}'"
                );
                skipped++;
                continue;
            }

            string enemyName   = values[COL_ENEMY_NAME].Trim();
            string description = values[COL_DESCRIPTION].Trim();

            BattleCardData card = FindOrCreateAsset(id, enemyName);
            bool hasErrors = false;

            // Описание
            card.description = description;

            // --- RequiredAttack ---
            if (int.TryParse(values[COL_REQUIRED_ATTACK].Trim(), out int reqAttack))
                card.requiredAttack = reqAttack;
            else
            {
                Debug.LogError($"[BattleCardImporter] Строка {i + 1} (ID={id}): ошибка requiredAttack. Значение: '{values[COL_REQUIRED_ATTACK]}'");
                hasErrors = true;
            }

            // --- RewardMoney ---
            if (int.TryParse(values[COL_REWARD_MONEY].Trim(), out int rewardMoney))
                card.rewardMoney = rewardMoney;
            else
            {
                Debug.LogError($"[BattleCardImporter] Строка {i + 1} (ID={id}): ошибка rewardMoney. Значение: '{values[COL_REWARD_MONEY]}'");
                hasErrors = true;
            }

            // --- PenaltyMoney ---
            if (int.TryParse(values[COL_PENALTY_MONEY].Trim(), out int penaltyMoney))
                card.penaltyMoney = penaltyMoney;
            else
            {
                Debug.LogError($"[BattleCardImporter] Строка {i + 1} (ID={id}): ошибка penaltyMoney. Значение: '{values[COL_PENALTY_MONEY]}'");
                hasErrors = true;
            }

            // --- CrewLoss ---
            if (int.TryParse(values[COL_CREW_LOSS].Trim(), out int crewLoss))
                card.crewLoss = crewLoss;
            else
            {
                Debug.LogError($"[BattleCardImporter] Строка {i + 1} (ID={id}): ошибка crewLoss. Значение: '{values[COL_CREW_LOSS]}'");
                hasErrors = true;
            }

            // --- RewardHandCardID ---
            if (int.TryParse(values[COL_REWARD_HANDCARD].Trim(), out int rewardHandCardID))
                card.rewardHandCardID = rewardHandCardID;
            else
            {
                Debug.LogError($"[BattleCardImporter] Строка {i + 1} (ID={id}): ошибка rewardHandCardID. Значение: '{values[COL_REWARD_HANDCARD]}'");
                hasErrors = true;
            }

            EditorUtility.SetDirty(card);

            if (!hasErrors) imported++;
            else skipped++;
        }

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        Debug.Log($"[BattleCardImporter] Импорт завершён. Успешно: {imported}. Пропущено/с ошибками: {skipped}.");

        EditorUtility.DisplayDialog(
            "Импорт завершён",
            $"Успешно обработано карт: {imported}\nПропущено (ошибки): {skipped}\n\nПодробности — в консоли Unity.",
            "OK"
        );
    }

    // -------------------------------------------------------------------------
    // Определение разделителя
    // -------------------------------------------------------------------------

    /// <summary>
    /// Определяет разделитель по заголовочной строке файла.
    /// Поддерживает табуляцию (TSV из Excel) и запятую (CSV).
    /// </summary>
    private char DetectDelimiter(string headerLine)
    {
        // Если в заголовке есть таб — файл TSV (экспорт из Excel по умолчанию)
        if (headerLine.Contains('\t'))
        {
            Debug.Log("[BattleCardImporter] Определён разделитель: табуляция (TSV).");
            return '\t';
        }

        Debug.Log("[BattleCardImporter] Определён разделитель: запятая (CSV).");
        return ',';
    }

    // -------------------------------------------------------------------------
    // CSV/TSV-парсер с поддержкой кавычек
    // -------------------------------------------------------------------------

    /// <summary>
    /// Разбивает строку по указанному разделителю.
    /// Для запятой поддерживает поля в двойных кавычках (стандарт CSV RFC 4180).
    /// Для табуляции — простое разбиение (Excel не экранирует табы кавычками).
    /// </summary>
    private string[] ParseCsvLine(string line, char delimiter)
    {
        // Для табуляции — простое разбиение, кавычки не нужны
        if (delimiter == '\t')
            return line.Split('\t');

        // Для запятой — полный парсер с поддержкой кавычек
        var fields  = new System.Collections.Generic.List<string>();
        var current = new System.Text.StringBuilder();
        bool inQuotes = false;

        for (int i = 0; i < line.Length; i++)
        {
            char c = line[i];

            if (c == '"')
            {
                // Экранированная кавычка внутри поля: "" → "
                if (inQuotes && i + 1 < line.Length && line[i + 1] == '"')
                {
                    current.Append('"');
                    i++;
                }
                else
                {
                    inQuotes = !inQuotes;
                }
            }
            else if (c == ',' && !inQuotes)
            {
                fields.Add(current.ToString());
                current.Clear();
            }
            else
            {
                current.Append(c);
            }
        }

        fields.Add(current.ToString()); // последнее поле
        return fields.ToArray();
    }

    // -------------------------------------------------------------------------
    // Поиск или создание SO-ассета
    // -------------------------------------------------------------------------

    private BattleCardData FindOrCreateAsset(int id, string enemyName)
    {
        string safeName  = string.Concat(enemyName.Split(Path.GetInvalidFileNameChars()));
        string assetName = $"Card_{id}_{safeName}.asset";

        string normalizedSoPath = soSavePath.TrimEnd('/', '\\') + "/";
        string fullPath         = normalizedSoPath + assetName;

        BattleCardData existing = AssetDatabase.LoadAssetAtPath<BattleCardData>(fullPath);
        if (existing != null)
        {
            Debug.Log($"[BattleCardImporter] Обновление: ID={id} ({enemyName})");
            return existing;
        }

        string absoluteSoPath = Path.Combine(
            Application.dataPath.Replace("Assets", ""),
            normalizedSoPath
        );
        if (!Directory.Exists(absoluteSoPath))
        {
            Directory.CreateDirectory(absoluteSoPath);
            AssetDatabase.Refresh();
        }

        BattleCardData newCard = ScriptableObject.CreateInstance<BattleCardData>();
        newCard.cardID    = id;
        newCard.enemyName = enemyName;

        AssetDatabase.CreateAsset(newCard, fullPath);
        Debug.Log($"[BattleCardImporter] Создана новая карта: ID={id} ({enemyName})");
        return newCard;
    }
}