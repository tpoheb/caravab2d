using UnityEngine;
using UnityEditor;

/// <summary>
/// Автоматически создаёт ассеты ShadowCardData из встроенных данных.
/// Меню: Tools → Cards → Create Shadow Cards
/// Ассеты сохраняются в Assets/Data/ShadowEvent/
/// 
/// Спрайты (cardFaceSprite, cardBackSprite) не назначаются автоматически —
/// заполняйте вручную в инспекторе после импорта.
/// </summary>
public static class ShadowCardImporter
{
    private const string OUTPUT_PATH = "Assets/Data/ShadowEvent";

    [MenuItem("Tools/Cards/Create Shadow Cards")]
    public static void CreateAll()
    {
        // Создаём папку если нет
        if (!AssetDatabase.IsValidFolder("Assets/Data"))
            AssetDatabase.CreateFolder("Assets", "Data");
        if (!AssetDatabase.IsValidFolder(OUTPUT_PATH))
            AssetDatabase.CreateFolder("Assets/Data", "ShadowEvent");

        int created = 0;
        int skipped = 0;

        foreach (var entry in GetCardData())
        {
            string assetPath = $"{OUTPUT_PATH}/{entry.fileName}.asset";

            // Не перезаписываем существующие
            if (AssetDatabase.LoadAssetAtPath<ShadowCardData>(assetPath) != null)
            {
                Debug.Log($"[ShadowCardImporter] Пропущен (уже существует): {entry.fileName}");
                skipped++;
                continue;
            }

            var card = ScriptableObject.CreateInstance<ShadowCardData>();
            card.cardName       = entry.cardName;
            card.description    = entry.description;
            card.effectType     = entry.effectType;
            card.value          = entry.value;
            card.isTemporary    = entry.isTemporary;
            card.duration       = entry.duration;
            card.penaltyValue   = entry.penaltyValue;
            card.minDifficulty  = entry.minDifficulty;
            card.maxDifficulty  = entry.maxDifficulty;
            card.weight         = entry.weight;
            // cardFaceSprite и cardBackSprite = null (назначаются вручную)

            AssetDatabase.CreateAsset(card, assetPath);
            created++;
        }

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        EditorUtility.DisplayDialog(
            "Shadow Cards Import",
            $"Готово!\nСоздано: {created}\nПропущено: {skipped}\n\nПуть: {OUTPUT_PATH}\n\nНе забудьте назначить спрайты вручную!",
            "OK"
        );

        Debug.Log($"[ShadowCardImporter] Создано: {created}, пропущено: {skipped}. Путь: {OUTPUT_PATH}");
    }

    // ─────────────────────────────────────────────────────────────────────
    // Данные карт (из таблицы дизайнера)
    // ─────────────────────────────────────────────────────────────────────

    private struct CardEntry
    {
        public string          fileName;
        public string          cardName;
        public string          description;
        public ShadowEffectType effectType;
        public int             value;
        public bool            isTemporary;
        public int             duration;
        public int             penaltyValue;
        public int             minDifficulty;
        public int             maxDifficulty;
        public int             weight;
    }

    private static CardEntry[] GetCardData() => new CardEntry[]
    {
        // ═════════════════════════════════════════════════════════════════
        // ДЕНЬГИ
        // ═════════════════════════════════════════════════════════════════

        // Мягкая
        new CardEntry {
            fileName       = "Shadow_NaydennyKoshel",
            cardName       = "Найденный кошель",
            description    = "На обочине дороги блестит что-то в песке. Забытый кем-то кошель, лёгкий, но приятный.",
            effectType     = ShadowEffectType.Money,
            value          = 10,        // +10% капитала — логика в ShadowEffectManager
            isTemporary    = false,
            minDifficulty  = 0,
            maxDifficulty  = 10,
            weight         = 15,
        },

        // Средняя
        new CardEntry {
            fileName       = "Shadow_PoshlinaUMosta",
            cardName       = "Пошлина у моста",
            description    = "Местный сборщик щурится на ваш караван и называет сумму, которая явно выше обычной.",
            effectType     = ShadowEffectType.Money,
            value          = -20,       // −20% капитала
            isTemporary    = false,
            minDifficulty  = 2,
            maxDifficulty  = 10,
            weight         = 12,
        },

        // Жёсткая (на выбор игрока — рискнуть или отказаться)
        new CardEntry {
            fileName       = "Shadow_AzartnySheyh",
            cardName       = "Азартный шейх",
            description    = "Богатый шейх скучает и предлагает пари на удачу — соглашаться не обязательно, но он настойчив.",
            effectType     = ShadowEffectType.Money,
            value          = 50,        // +50% / −50% — логика выбора в ShadowEffectManager
            isTemporary    = false,
            minDifficulty  = 5,
            maxDifficulty  = 10,
            weight         = 8,
        },

        // ═════════════════════════════════════════════════════════════════
        // ЦЕНЫ ТОВАРОВ (BonusTrade)
        // ═════════════════════════════════════════════════════════════════

        // Мягкая
        new CardEntry {
            fileName       = "Shadow_SluhSRynka",
            cardName       = "Слух с рынка",
            description    = "Кто-то в чайхане обмолвился, какой товар сейчас в цене в соседнем городе.",
            effectType     = ShadowEffectType.BonusTrade,
            value          = 10,        // +10% к цене одного товара
            isTemporary    = false,
            minDifficulty  = 0,
            maxDifficulty  = 10,
            weight         = 15,
        },

        // Средняя
        new CardEntry {
            fileName       = "Shadow_PerekupshchikiPodsuetilis",
            cardName       = "Перекупщики подсуетились",
            description    = "Ушлые перекупщики скупили половину товара на местном рынке ещё до вашего приезда.",
            effectType     = ShadowEffectType.BonusTrade,
            value          = -25,       // −25% к цене одного товара
            isTemporary    = false,
            minDifficulty  = 2,
            maxDifficulty  = 10,
            weight         = 12,
        },

        // Жёсткая
        new CardEntry {
            fileName       = "Shadow_PrazdnikUrozhaia",
            cardName       = "Праздник урожая",
            description    = "Город празднует богатый урожай — товары нарасхват, любые излишки уходят влёт.",
            effectType     = ShadowEffectType.BonusTrade,
            value          = 45,        // +45% ко всем товарам
            isTemporary    = false,
            minDifficulty  = 4,
            maxDifficulty  = 10,
            weight         = 8,
        },

        // ═════════════════════════════════════════════════════════════════
        // ГРУЗ И ТРЮМ
        // ═════════════════════════════════════════════════════════════════

        // Мягкая
        new CardEntry {
            fileName       = "Shadow_KrepkieTyuki",
            cardName       = "Крепкие тюки",
            description    = "Ваш караванщик удачно перевязал груз — ничего не болтается, ничего не бьётся.",
            effectType     = ShadowEffectType.Capacity,
            value          = 10,        // +10% вместимости
            isTemporary    = true,
            duration       = 10,        // до конца маршрута (≈10 ходов)
            minDifficulty  = 0,
            maxDifficulty  = 10,
            weight         = 12,
        },

        // Средняя
        new CardEntry {
            fileName       = "Shadow_PodmokshiyGruz",
            cardName       = "Подмокший груз",
            description    = "Внезапный ливень застал караван врасплох. Часть тюков отсырела.",
            effectType     = ShadowEffectType.RemoveGoods,
            value          = 1,         // −25% от одного случайного товара
            isTemporary    = false,
            minDifficulty  = 2,
            maxDifficulty  = 10,
            weight         = 10,
        },

        // Жёсткая
        new CardEntry {
            fileName       = "Shadow_VerblydOstupilsya",
            cardName       = "Верблюд оступился",
            description    = "Крутой спуск, неудачный шаг — и часть груза летит вниз по склону.",
            effectType     = ShadowEffectType.RemoveGoods,
            value          = 2,         // потеря одного товара полностью
            isTemporary    = false,
            minDifficulty  = 5,
            maxDifficulty  = 10,
            weight         = 6,
        },

        // ═════════════════════════════════════════════════════════════════
        // СОСТАВ КОМАНДЫ
        // ═════════════════════════════════════════════════════════════════

        // Мягкая
        new CardEntry {
            fileName       = "Shadow_VtoroeDyhanie",
            cardName       = "Второе дыхание",
            description    = "Ночной отдых у костра пошёл всем на пользу — команда бодра как никогда.",
            effectType     = ShadowEffectType.TeamStats,
            value          = 10,        // +10% к характеристикам случайного юнита
            isTemporary    = true,
            duration       = 10,        // до конца маршрута
            minDifficulty  = 0,
            maxDifficulty  = 10,
            weight         = 12,
        },

        // Средняя
        new CardEntry {
            fileName       = "Shadow_ProstudaVPuti",
            cardName       = "Простуда в пути",
            description    = "Холодный горный ветер не щадит никого — один из ваших людей слёг с кашлем.",
            effectType     = ShadowEffectType.TeamStats,
            value          = -20,       // −20% к характеристикам
            isTemporary    = true,
            duration       = 10,
            minDifficulty  = 2,
            maxDifficulty  = 10,
            weight         = 10,
        },

        // Жёсткая
        new CardEntry {
            fileName       = "Shadow_ZovStaroyZhizni",
            cardName       = "Зов старой жизни",
            description    = "Один из членов команды получает весть из дома. Он должен вернуться — и не обещает вернуться.",
            effectType     = ShadowEffectType.FireCrewMember,
            value          = 0,
            isTemporary    = false,
            minDifficulty  = 4,
            maxDifficulty  = 10,
            weight         = 6,
        },
    };
}