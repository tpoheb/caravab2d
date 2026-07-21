#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using System.IO;

/// <summary>
/// Editor-импортёр карт руки.
/// Меню: Tools → 1000 Roads → Import Hand Cards
/// Создаёт ScriptableObject-ассеты в Assets/Data/HandCards/
/// Иконки назначить вручную в Inspector после импорта.
/// </summary>
public static class HandCardImporter
{
    private const string OutputPath = "Assets/Data/HandCards";

    // ── Данные карт ───────────────────────────────────────────────────────

    private struct CardDefinition
    {
        public string                      Name;
        public string                      Description;
        public HandCardData.CardCategory   Category;
        public HandCardData.CardEffectType EffectType;
        public int                         Value;
    }

    private static readonly CardDefinition[] Cards = new CardDefinition[]
    {
        // ── Существующие ─────────────────────────────────────────────────

        new CardDefinition
        {
            Name        = "Старая Карта",
            Description = "Потрёпанная карта с загадочными символами. Говорят, она помнит все дороги.",
            Category    = HandCardData.CardCategory.Tactical,
            EffectType  = HandCardData.CardEffectType.ChooseDice,
            Value       = 0,
        },
        new CardDefinition
        {
            Name        = "Переброс",
            Description = "Судьба изменчива — бросьте кубик ещё раз.",
            Category    = HandCardData.CardCategory.Tactical,
            EffectType  = HandCardData.CardEffectType.Reroll,
            Value       = 0,
        },
        new CardDefinition
        {
            Name        = "Боевой клич",
            Description = "Ваш отряд воодушевлён. +3 к атаке в этом бою.",
            Category    = HandCardData.CardCategory.Tactical,
            EffectType  = HandCardData.CardEffectType.AddBonus,
            Value       = 3,
        },
        new CardDefinition
        {
            Name        = "Дополнительный тюк",
            Description = "Перераспределили груз — каравану стало немного легче.",
            Category    = HandCardData.CardCategory.Logistic,
            EffectType  = HandCardData.CardEffectType.CapacityBoost,
            Value       = 10,
        },
        new CardDefinition
        {
            Name        = "Золотой язык",
            Description = "Пара нужных слов в нужный момент. +15% к следующей сделке.",
            Category    = HandCardData.CardCategory.Economic,
            EffectType  = HandCardData.CardEffectType.GoldBoost,
            Value       = 15,
        },
        new CardDefinition
        {
            Name        = "Дымовая завеса",
            Description = "Клубы дыма скрывают караван. Разбойники теряют след.",
            Category    = HandCardData.CardCategory.Tactical,
            EffectType  = HandCardData.CardEffectType.EscapeBattle,
            Value       = 0,
        },
        new CardDefinition
        {
            Name        = "Странный Амулет",
            Description = "Древний оберег отводит беду. Следующая карта Тени или Битвы отменяется.",
            Category    = HandCardData.CardCategory.Tactical,
            EffectType  = HandCardData.CardEffectType.CancelCard,
            Value       = 0,
        },
        new CardDefinition
        {
            Name        = "Мистический Узел",
            Description = "Духи торговли благосклонны — один из товаров в вашем инвентаре удваивается.",
            Category    = HandCardData.CardCategory.Economic,
            EffectType  = HandCardData.CardEffectType.DoubleGoods,
            Value       = 0,
        },

        // ── Новые торговые ────────────────────────────────────────────────

        new CardDefinition
        {
            Name        = "Слово Менялы",
            Description = "Вы клянётесь именем духа-посредника, что цена честная. Продавец вам верит — или делает вид.",
            Category    = HandCardData.CardCategory.Economic,
            EffectType  = HandCardData.CardEffectType.SaleBonus,
            Value       = 40,
        },
        new CardDefinition
        {
            Name        = "Второе дно каравана",
            Description = "Хитрый тайник под тюками, о котором не знает ни один сборщик пошлины.",
            Category    = HandCardData.CardCategory.Logistic,
            EffectType  = HandCardData.CardEffectType.IgnoreTax,
            Value       = 0,
        },
        new CardDefinition
        {
            Name        = "Слух из первых уст",
            Description = "Ваш меняла узнал о грядущем дефиците товара за день до всех остальных.",
            Category    = HandCardData.CardCategory.Economic,
            EffectType  = HandCardData.CardEffectType.SalePriceBoost,
            Value       = 25,
        },
        new CardDefinition
        {
            Name        = "Купец купцу друг",
            Description = "Лесть, немного правды и щедрая порция обаяния — рецепт скидки, известный не первый век.",
            Category    = HandCardData.CardCategory.Economic,
            EffectType  = HandCardData.CardEffectType.PurchaseDiscount,
            Value       = 20,
        },

        // ── Новые боевые ──────────────────────────────────────────────────

        new CardDefinition
        {
            Name        = "Крепкое плечо",
            Description = "Ваш воин встаёт впереди каравана, и разбойники дважды думают, прежде чем напасть.",
            Category    = HandCardData.CardCategory.Tactical,
            EffectType  = HandCardData.CardEffectType.AddBonus,
            Value       = 3,
        },
        new CardDefinition
        {
            Name        = "Пыль в глаза",
            Description = "Горсть песка, брошенная точно в нужный момент. Не благородно, зато работает.",
            Category    = HandCardData.CardCategory.Tactical,
            EffectType  = HandCardData.CardEffectType.EnemyAttackDebuff,
            Value       = 4,
        },
        new CardDefinition
        {
            Name        = "Притвориться бедным",
            Description = "Рваньё поверх хорошей одежды и грустное лицо творят чудеса с чужой мотивацией грабить.",
            Category    = HandCardData.CardCategory.Tactical,
            EffectType  = HandCardData.CardEffectType.EscapeBattle,
            Value       = 0,
        },
        new CardDefinition
        {
            Name        = "Клятва Пути",
            Description = "Вы напоминаете себе слова странников — Дорога испытывает, но не топит. Спокойствие острее клинка.",
            Category    = HandCardData.CardCategory.Tactical,
            EffectType  = HandCardData.CardEffectType.BattlePenaltyReduce,
            Value       = 40,
        },
    };

    // ── Точка входа ───────────────────────────────────────────────────────

    [MenuItem("Tools/1000 Roads/Import Hand Cards")]
    public static void ImportHandCards()
    {
        EnsureOutputFolder();

        int created = 0;
        int skipped = 0;

        foreach (var def in Cards)
        {
            string assetPath = Path.Combine(OutputPath, $"{def.Name}.asset")
                                   .Replace("\\", "/");

            // Не перезаписываем существующие ассеты — иконки не потеряются
            if (File.Exists(assetPath))
            {
                Debug.Log($"[HandCardImporter] Пропущено (уже существует): {def.Name}");
                skipped++;
                continue;
            }

            HandCardData asset = ScriptableObject.CreateInstance<HandCardData>();
            asset.cardName   = def.Name;
            asset.description = def.Description;
            asset.category   = def.Category;
            asset.effectType = def.EffectType;
            asset.value      = def.Value;

            AssetDatabase.CreateAsset(asset, assetPath);
            created++;
            Debug.Log($"[HandCardImporter] Создан: {def.Name}");
        }

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        EditorUtility.DisplayDialog(
            "Import Hand Cards",
            $"Готово.\nСоздано: {created}\nПропущено: {skipped}\n\n" +
            $"Ассеты в: {OutputPath}\n" +
            $"Не забудь назначить иконки в Inspector.",
            "OK"
        );
    }

    // ── Вспомогательные ───────────────────────────────────────────────────

    private static void EnsureOutputFolder()
    {
        if (AssetDatabase.IsValidFolder(OutputPath)) return;

        // Создаём папки по одной — CreateFolder принимает родителя и имя дочерней
        if (!AssetDatabase.IsValidFolder("Assets/ScriptableObjects"))
            AssetDatabase.CreateFolder("Assets", "ScriptableObjects");

        AssetDatabase.CreateFolder("Assets/ScriptableObjects", "HandCards");
    }
}
#endif