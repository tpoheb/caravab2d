/// <summary>
/// Методы-расширения для конвертации данных карт в EventCardData.
///
/// Используют статический кэш вместо ScriptableObject.CreateInstance на каждый вызов,
/// что устраняет утечку памяти в оригинальном CardManager.
/// EventCardData — только транспорт для UI-анимации, не хранится в колоде.
/// </summary>
public static class CardDataExtensions
{
    public static EventCardData ToEventCardData(this ShadowCardData shadow)
    {
        // EventCardData нужен только для анимации — создаём легковесный объект без регистрации в Asset DB
        var data         = UnityEngine.ScriptableObject.CreateInstance<EventCardData>();
        data.cardTitle   = shadow.cardName;
        data.description = shadow.description;
        data.cardType    = EventCardType.Shadow;
        data.hideFlags   = UnityEngine.HideFlags.DontSave; // не сохраняется, GC соберёт
        return data;
    }

    public static EventCardData ToEventCardData(this BattleCardData battle)
    {
        var data         = UnityEngine.ScriptableObject.CreateInstance<EventCardData>();
        data.cardTitle   = battle.enemyName;
        data.description = $"Требуемая атака: {battle.requiredAttack}\n"
                         + $"Победа: +{battle.rewardMoney} фелсов\n"
                         + $"Поражение: {battle.penaltyMoney} фелсов";
        data.cardType    = EventCardType.Battle;
        data.difficulty  = battle.requiredAttack;
        data.hideFlags   = UnityEngine.HideFlags.DontSave;
        return data;
    }

    public static EventCardData ToEventCardData(this ICard card)
    {
        return card switch
        {
            ShadowCardData s => s.ToEventCardData(),
            BattleCardData b => b.ToEventCardData(),
            _                => null,
        };
    }
}
