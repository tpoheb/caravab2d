using UnityEngine;

/// <summary>
/// Singleton. Хранит активные торговые модификаторы от карт руки.
/// Подписывается на TradeCardEvents, выдаёт значения по Consume-паттерну.
/// TradeTransactionHandler вызывает Consume-методы в момент транзакции.
/// </summary>
public class TradeCardModifiers : MonoBehaviour
{
    public static TradeCardModifiers Instance { get; private set; }

    // ── Активные модификаторы ────────────────────────────────────────────

    private int   _pendingSaleBonus       = 0;
    private bool  _ignoreTaxActive        = false;
    private float _pendingSalePriceBoost  = 0f;   // множитель, напр. 0.25 = +25%
    private float _pendingPurchaseDiscount = 0f;  // множитель, напр. 0.20 = −20%

    // ── Lifecycle ────────────────────────────────────────────────────────

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
    }

    private void OnEnable()
    {
        TradeCardEvents.OnSaleBonusActivated      += AddSaleBonus;
        TradeCardEvents.OnIgnoreTaxActivated       += SetIgnoreTax;
        TradeCardEvents.OnSalePriceBoostActivated  += AddSalePriceBoost;
        TradeCardEvents.OnPurchaseDiscountActivated += AddPurchaseDiscount;
    }

    private void OnDisable()
    {
        TradeCardEvents.OnSaleBonusActivated      -= AddSaleBonus;
        TradeCardEvents.OnIgnoreTaxActivated       -= SetIgnoreTax;
        TradeCardEvents.OnSalePriceBoostActivated  -= AddSalePriceBoost;
        TradeCardEvents.OnPurchaseDiscountActivated -= AddPurchaseDiscount;
    }

    // ── Подписчики ───────────────────────────────────────────────────────

    private void AddSaleBonus(int bonus)
    {
        _pendingSaleBonus += bonus;
        Debug.Log($"[TradeCardModifiers] SaleBonus накоплен: +{bonus} (итого {_pendingSaleBonus})");
    }

    private void SetIgnoreTax()
    {
        _ignoreTaxActive = true;
        Debug.Log("[TradeCardModifiers] IgnoreTax активирован.");
    }

    private void AddSalePriceBoost(float percent)
    {
        _pendingSalePriceBoost += percent;
        Debug.Log($"[TradeCardModifiers] SalePriceBoost накоплен: +{percent:P0} (итого {_pendingSalePriceBoost:P0})");
    }

    private void AddPurchaseDiscount(float percent)
    {
        _pendingPurchaseDiscount += percent;
        Debug.Log($"[TradeCardModifiers] PurchaseDiscount накоплен: -{percent:P0} (итого {_pendingPurchaseDiscount:P0})");
    }

    // ── Consume-методы (вызываются из TradeTransactionHandler) ───────────

    /// <summary>Забирает фиксированный бонус к продаже и сбрасывает его.</summary>
    public int ConsumeSaleBonus()
    {
        int v = _pendingSaleBonus;
        _pendingSaleBonus = 0;
        if (v != 0) Debug.Log($"[TradeCardModifiers] ConsumeSaleBonus: применено +{v}");
        return v;
    }

    /// <summary>Проверяет и сбрасывает флаг игнорирования пошлины.</summary>
    public bool ConsumeIgnoreTax()
    {
        if (!_ignoreTaxActive) return false;
        _ignoreTaxActive = false;
        Debug.Log("[TradeCardModifiers] ConsumeIgnoreTax: пошлина проигнорирована.");
        return true;
    }

    /// <summary>Забирает процентный буст к цене продажи и сбрасывает его.</summary>
    public float ConsumeSalePriceBoost()
    {
        float v = _pendingSalePriceBoost;
        _pendingSalePriceBoost = 0f;
        if (v > 0f) Debug.Log($"[TradeCardModifiers] ConsumeSalePriceBoost: применено +{v:P0}");
        return v;
    }

    /// <summary>Забирает процентную скидку на покупку и сбрасывает её.</summary>
    public float ConsumePurchaseDiscount()
    {
        float v = _pendingPurchaseDiscount;
        _pendingPurchaseDiscount = 0f;
        if (v > 0f) Debug.Log($"[TradeCardModifiers] ConsumePurchaseDiscount: применено -{v:P0}");
        return v;
    }
}