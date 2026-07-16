// =============================================================================
// BattleCardImporter.cs
// Инструмент редактора Unity для импорта/обновления карт битвы из CSV-файла.
//
// КАК ИСПОЛЬЗОВАТЬ:
//   1. В меню Unity: Tools → Battle Cards → Import from CSV
//   2. Нажмите "Выбрать CSV..." и укажите файл с данными.
//   3. При необходимости измените папку сохранения SO (поле "Папка для SO:").
//   4. Нажмите "Импортировать / Обновить карты".
//
// ФОРМАТ CSV (первая строка — заголовок, игнорируется):
//   ID, EnemyName, RequiredAttack, RewardMoney, PenaltyMoney, CrewLoss, RewardHandCardID
//
//   Колонки:
//   - ID              (int)    — уникальный числовой идентификатор карты.
//   - EnemyName       (string) — имя врага / название карты.
//   - RequiredAttack  (int)    — минимальная атака для победы.
//   - RewardMoney     (int)    — золото, получаемое при победе (>= 0).
//   - PenaltyMoney    (int)    — золото, теряемое при поражении (>= 0).
//   - CrewLoss        (int)    — количество случайных членов команды,
//                               теряемых при поражении (>= 0, 0 = потерь нет).
//   - RewardHandCardID (int)   — ID карты руки (HandCardData), выдаваемой
//                               при победе. 0 = карта руки не выдаётся.
//
// ПРИМЕР СТРОКИ CSV:
//   1, Desert Bandit, 5, 100, 50, 1, 0
//   2, Sand Worm,     12, 250, 80, 0, 7
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
//       int cardID, string enemyName, int requiredAttack,
//       int rewardMoney, int penaltyMoney,
//       int crewLoss, int rewardHandCardID
//   - HandCardData.cs   — ScriptableObject карты руки (только для справки,
//                         здесь хранится лишь числовой ID).
// =============================================================================

using UnityEngine;
using UnityEditor;
using System.IO;

public class BattleCardImporter : EditorWindow
{
    // -------------------------------------------------------------------------
    // Поля окна редактора
    // -------------------------------------------------------------------------

    /// <summary>Абсолютный путь к выбранному CSV-файлу.</summary>
    private string csvFilePath = "";

    /// <summary>Путь внутри проекта (Assets/...) для сохранения SO-ассетов.</summary>
    private string soSavePath = "Assets/Data/BattleCards/";

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

        // --- Выбор CSV-файла ---
        EditorGUILayout.LabelField("CSV-файл с данными карт:");
        EditorGUILayout.BeginHorizontal();
        {
            // Текстовое поле (только для чтения — путь задаётся через диалог)
            EditorGUI.BeginDisabledGroup(true);
            EditorGUILayout.TextField(
                string.IsNullOrEmpty(csvFilePath) ? "(файл не выбран)" : csvFilePath
            );
            EditorGUI.EndDisabledGroup();

            if (GUILayout.Button("Выбрать CSV...", GUILayout.Width(120)))
            {
                // Открываем системный диалог выбора файла
                string selected = EditorUtility.OpenFilePanel(
                    title:      "Выберите CSV-файл карт битвы",
                    directory:  Application.dataPath,
                    extension:  "csv"
                );

                // OpenFilePanel возвращает пустую строку при отмене
                if (!string.IsNullOrEmpty(selected))
                {
                    csvFilePath = selected;
                }
            }
        }
        EditorGUILayout.EndHorizontal();

        EditorGUILayout.Space();

        // --- Папка сохранения SO ---
        soSavePath = EditorGUILayout.TextField("Папка для SO:", soSavePath);

        EditorGUILayout.Space();

        // --- Подсказка по формату ---
        EditorGUILayout.HelpBox(
            "Ожидаемые колонки CSV (первая строка — заголовок):\n" +
            "ID | EnemyName | RequiredAttack | RewardMoney | PenaltyMoney | CrewLoss | RewardHandCardID",
            MessageType.Info
        );

        EditorGUILayout.Space();

        // --- Кнопка импорта ---
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

    /// <summary>
    /// Читает CSV построчно и создаёт/обновляет ScriptableObject-ассеты BattleCardData.
    /// Ожидаемый порядок колонок (начиная с индекса 0):
    ///   0: ID | 1: EnemyName | 2: RequiredAttack | 3: RewardMoney |
    ///   4: PenaltyMoney | 5: CrewLoss | 6: RewardHandCardID
    /// </summary>
    private void ImportData()
    {
        // Проверяем существование файла
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

        int imported  = 0;
        int skipped   = 0;

        // Строка 0 — заголовок, начинаем с 1
        for (int i = 1; i < lines.Length; i++)
        {
            string line = lines[i].Trim();

            // Пропускаем пустые строки (например, в конце файла)
            if (string.IsNullOrEmpty(line))
                continue;

            string[] values = line.Split(',');

            // Минимально необходимое количество колонок
            const int REQUIRED_COLUMNS = 7;
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

            // --- Парсинг ID ---
            if (!int.TryParse(values[0].Trim(), out int id))
            {
                Debug.LogError(
                    $"[BattleCardImporter] Строка {i + 1}: не удалось распарсить ID. " +
                    $"Значение: '{values[0]}'"
                );
                skipped++;
                continue;
            }

            string enemyName = values[1].Trim();

            // Находим или создаём ассет
            BattleCardData card = FindOrCreateAsset(id, enemyName);
            bool hasErrors = false;

            // --- Парсинг RequiredAttack (колонка 2) ---
            if (int.TryParse(values[2].Trim(), out int reqAttack))
                card.requiredAttack = reqAttack;
            else
            {
                Debug.LogError(
                    $"[BattleCardImporter] Строка {i + 1} (ID={id}): " +
                    $"ошибка requiredAttack. Значение: '{values[2]}'"
                );
                hasErrors = true;
            }

            // --- Парсинг RewardMoney (колонка 3) ---
            if (int.TryParse(values[3].Trim(), out int rewardMoney))
                card.rewardMoney = rewardMoney;
            else
            {
                Debug.LogError(
                    $"[BattleCardImporter] Строка {i + 1} (ID={id}): " +
                    $"ошибка rewardMoney. Значение: '{values[3]}'"
                );
                hasErrors = true;
            }

            // --- Парсинг PenaltyMoney (колонка 4) ---
            if (int.TryParse(values[4].Trim(), out int penaltyMoney))
                card.penaltyMoney = penaltyMoney;
            else
            {
                Debug.LogError(
                    $"[BattleCardImporter] Строка {i + 1} (ID={id}): " +
                    $"ошибка penaltyMoney. Значение: '{values[4]}'"
                );
                hasErrors = true;
            }

            // --- Парсинг CrewLoss (колонка 5) ---
            // Количество случайных членов команды, теряемых при поражении.
            // 0 означает «потерь нет».
            if (int.TryParse(values[5].Trim(), out int crewLoss))
                card.crewLoss = crewLoss;
            else
            {
                Debug.LogError(
                    $"[BattleCardImporter] Строка {i + 1} (ID={id}): " +
                    $"ошибка crewLoss. Значение: '{values[5]}'"
                );
                hasErrors = true;
            }

            // --- Парсинг RewardHandCardID (колонка 6) ---
            // ID карты руки (HandCardData), получаемой при победе.
            // 0 означает «карта руки не выдаётся».
            if (int.TryParse(values[6].Trim(), out int rewardHandCardID))
                card.rewardHandCardID = rewardHandCardID;
            else
            {
                Debug.LogError(
                    $"[BattleCardImporter] Строка {i + 1} (ID={id}): " +
                    $"ошибка rewardHandCardID. Значение: '{values[6]}'"
                );
                hasErrors = true;
            }

            // Помечаем ассет изменённым, чтобы Unity сохранил его
            EditorUtility.SetDirty(card);

            if (!hasErrors)
                imported++;
            else
                skipped++;
        }

        // Сохраняем все изменения на диск
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        Debug.Log(
            $"[BattleCardImporter] Импорт завершён. " +
            $"Успешно: {imported}. Пропущено/с ошибками: {skipped}."
        );

        // Показываем итоговый диалог
        EditorUtility.DisplayDialog(
            "Импорт завершён",
            $"Успешно обработано карт: {imported}\nПропущено (ошибки): {skipped}\n\n" +
            "Подробности — в консоли Unity.",
            "OK"
        );
    }

    // -------------------------------------------------------------------------
    // Вспомогательный метод: поиск или создание SO-ассета
    // -------------------------------------------------------------------------

    /// <summary>
    /// Ищет существующий BattleCardData-ассет по ожидаемому пути.
    /// Если не найден — создаёт новый.
    /// Имя файла: Card_<id>_<enemyName>.asset
    /// </summary>
    private BattleCardData FindOrCreateAsset(int id, string enemyName)
    {
        // Нормализуем имя: убираем символы, недопустимые в именах файлов
        string safeName  = string.Concat(enemyName.Split(Path.GetInvalidFileNameChars()));
        string assetName = $"Card_{id}_{safeName}.asset";

        // Путь внутри проекта (Assets/...) — требуется для AssetDatabase
        string normalizedSoPath = soSavePath.TrimEnd('/', '\\') + "/";
        string fullPath = normalizedSoPath + assetName;

        // Пробуем загрузить существующий ассет
        BattleCardData existing = AssetDatabase.LoadAssetAtPath<BattleCardData>(fullPath);
        if (existing != null)
        {
            Debug.Log($"[BattleCardImporter] Обновление: ID={id} ({enemyName})");
            return existing;
        }

        // Создаём папку, если её нет
        // Directory.CreateDirectory понимает как абсолютные, так и относительные пути;
        // здесь нам нужен абсолютный путь.
        string absoluteSoPath = Path.Combine(
            Application.dataPath.Replace("Assets", ""),
            normalizedSoPath
        );
        if (!Directory.Exists(absoluteSoPath))
        {
            Directory.CreateDirectory(absoluteSoPath);
            AssetDatabase.Refresh(); // Unity должен увидеть новую папку
        }

        // Создаём новый ScriptableObject
        BattleCardData newCard = ScriptableObject.CreateInstance<BattleCardData>();
        newCard.cardID    = id;
        newCard.enemyName = enemyName;

        AssetDatabase.CreateAsset(newCard, fullPath);
        Debug.Log($"[BattleCardImporter] Создана новая карта: ID={id} ({enemyName})");
        return newCard;
    }
}