using System;

/// <summary>
/// Шина событий торговых карт руки.
/// HandManager публикует → TradeCardModifiers подписывается и запоминает модификатор.
/// TradeTransactionHandler читает модификаторы в момент транзакции.
/// </summary>
public static class TradeCardEvents
{
    /// <summary>Фиксированный бонус к сумме продажи (Слово Менялы).</summary>
    public static event Action<int> OnSaleBonusActivated;

    /// <summary>Игнорировать пошлину при въезде в город (Второе дно каравана).</summary>
    public static event Action OnIgnoreTaxActivated;

    /// <summary>Процентный бонус к цене продажи одного товара (Слух из первых уст).</summary>
    public static event Action<float> OnSalePriceBoostActivated;

    /// <summary>Процентная скидка на покупку (Купец купцу друг).</summary>
    public static event Action<float> OnPurchaseDiscountActivated;

    // ── Безопасные вызовы ────────────────────────────────────────────────

    public static void SaleBonusActivated(int bonus)
        => OnSaleBonusActivated?.Invoke(bonus);

    public static void IgnoreTaxActivated()
        => OnIgnoreTaxActivated?.Invoke();

    public static void SalePriceBoostActivated(float percent)
        => OnSalePriceBoostActivated?.Invoke(percent);

    public static void PurchaseDiscountActivated(float percent)
        => OnPurchaseDiscountActivated?.Invoke(percent);
}