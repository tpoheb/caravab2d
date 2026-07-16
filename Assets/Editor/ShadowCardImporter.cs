using UnityEngine;
using UnityEditor;

/// <summary>
/// Автоматически создаёт ассеты ShadowCardData из встроенных данных.
/// Меню: Tools → Cards → Create Shadow Cards
/// Ассеты сохраняются в Assets/ScriptableObjects/Cards/Shadow/
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
            card.cardName    = entry.cardName;
            card.description = entry.description;
            card.effectType  = entry.effectType;
            card.value       = entry.value;
            card.isTemporary = entry.isTemporary;
            card.duration    = entry.duration;
            card.penaltyValue = entry.penaltyValue;

            AssetDatabase.CreateAsset(card, assetPath);
            created++;
        }

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        EditorUtility.DisplayDialog(
            "Shadow Cards Import",
            $"Готово!\nСоздано: {created}\nПропущено: {skipped}\n\nПуть: {OUTPUT_PATH}",
            "OK"
        );

        Debug.Log($"[ShadowCardImporter] Создано: {created}, пропущено: {skipped}. Путь: {OUTPUT_PATH}");
    }

    // ─────────────────────────────────────────────────────────────────────
    // Данные карт
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
    }

    private static CardEntry[] GetCardData() => new CardEntry[]
    {
        // ── Деньги ──────────────────────────────────────────────────────

        new CardEntry {
            fileName    = "Shadow_NaydennyKoshel",
            cardName    = "Найденный кошель",
            description = "На обочине дороги блестит что-то в песке. Забытый кем-то кошель, лёгкий, но приятный.",
            effectType  = ShadowEffectType.Money,
            value       = 50,   // +10% от среднего капитала — подберите под свою экономику
            isTemporary = false,
        },

        new CardEntry {
            fileName    = "Shadow_PoshlinaUMosta",
            cardName    = "Пошлина у моста",
            description = "Местный сборщик щурится на ваш караван и называет сумму, которая явно выше обычной.",
            effectType  = ShadowEffectType.Money,
            value       = -100, // −20% от среднего капитала
            isTemporary = false,
        },

        // ── Цены товаров → BonusTrade ────────────────────────────────────

        new CardEntry {
            fileName    = "Shadow_SluhSRynka",
            cardName    = "Слух с рынка",
            description = "Кто-то в чайхане обмолвился, какой товар сейчас в цене в соседнем городе.",
            effectType  = ShadowEffectType.BonusTrade,
            value       = 10,   // +10% к цене продажи
            isTemporary = false,
        },

        new CardEntry {
            fileName    = "Shadow_PerekupshchikiPodsuetilos",
            cardName    = "Перекупщики подсуетились",
            description = "Ушлые перекупщики скупили половину товара на местном рынке ещё до вашего приезда.",
            effectType  = ShadowEffectType.BonusTrade,
            value       = -25,  // −25% к цене продажи
            isTemporary = false,
        },

        new CardEntry {
            fileName    = "Shadow_PrazdnikUrozaya",
            cardName    = "Праздник урожая",
            description = "Город празднует богатый урожай — товары нарасхват, любые излишки уходят влёт.",
            effectType  = ShadowEffectType.BonusTrade,
            value       = 45,   // +45% к цене продажи всех товаров
            isTemporary = false,
        },

        // ── Груз и трюм ──────────────────────────────────────────────────

        new CardEntry {
            fileName    = "Shadow_KrepkieTyuki",
            cardName    = "Крепкие тюки",
            description = "Ваш караванщик удачно перевязал груз — ничего не болтается, ничего не бьётся.",
            effectType  = ShadowEffectType.Capacity,
            value       = 10,   // +10% к вместимости (в единицах — подберите)
            isTemporary = true,
            duration    = 3,    // до конца маршрута (≈ 3 хода)
        },

        new CardEntry {
            fileName    = "Shadow_PodmokshiyGruz",
            cardName    = "Подмокший груз",
            description = "Внезапный ливень застал караван врасплох. Часть тюков отсырела.",
            effectType  = ShadowEffectType.RemoveGoods,
            value       = 1,    // потеря части одного товара
            isTemporary = false,
        },

        new CardEntry {
            fileName    = "Shadow_VerblydOstupilsya",
            cardName    = "Верблюд оступился",
            description = "Крутой спуск, неудачный шаг — и часть груза летит вниз по склону.",
            effectType  = ShadowEffectType.RemoveGoods,
            value       = 2,    // потеря одного типа товара полностью
            isTemporary = false,
        },

        // ── Состав команды ───────────────────────────────────────────────

        new CardEntry {
            fileName    = "Shadow_VtoroeDyhanie",
            cardName    = "Второе дыхание",
            description = "Ночной отдых у костра пошёл всем на пользу — команда бодра как никогда.",
            effectType  = ShadowEffectType.TeamStats,
            value       = 10,   // +10% к характеристикам
            isTemporary = true,
            duration    = 2,
        },

        new CardEntry {
            fileName    = "Shadow_ProstudaVPuti",
            cardName    = "Простуда в пути",
            description = "Холодный горный ветер не щадит никого — один из ваших людей слёг с кашлем.",
            effectType  = ShadowEffectType.TeamStats,
            value       = -20,  // −20% к характеристикам
            isTemporary = true,
            duration    = 2,
        },

        new CardEntry {
            fileName    = "Shadow_ZovStaroyZhizni",
            cardName    = "Зов старой жизни",
            description = "Один из членов команды получает весть из дома. Он должен вернуться — и не обещает вернуться.",
            effectType  = ShadowEffectType.FireCrewMember,
            value       = 0,
            isTemporary = false,
        },

        // ── Сюжетный прогресс → AddGoods (заглушки до реализации системы)

        new CardEntry {
            fileName    = "Shadow_ObryvoKRazgovora",
            cardName    = "Обрывок разговора",
            description = "В таверне двое шептались о старых временах и амулетах, каких давно не делают.",
            effectType  = ShadowEffectType.AddGoods, // TODO: заменить на сюжетный тип, когда будет готов
            value       = 0,
            isTemporary = false,
        },

        new CardEntry {
            fileName    = "Shadow_TrevozniyeVesti",
            cardName    = "Тревожные вести",
            description = "Торговец с севера рассказывает — где-то в его родном городе люди стали забывать привычные вещи.",
            effectType  = ShadowEffectType.AddGoods, // TODO: сюжетный эффект
            value       = 0,
            isTemporary = false,
        },

        new CardEntry {
            fileName    = "Shadow_TyoplyAmulet",
            cardName    = "Тёплый амулет",
            description = "Ночью амулет на груди становится ощутимо тёплым, будто указывая направление, о котором вы не спрашивали.",
            effectType  = ShadowEffectType.AddGoods, // TODO: сюжетный эффект
            value       = 0,
            isTemporary = false,
        },
    };
}
