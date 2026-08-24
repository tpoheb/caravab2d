using UnityEngine;

/// <summary>
/// Управляет гильдией игрока: вступление, выход, смена.
/// Висит на том же GameObject что и PlayerInventory/TeamSystem.
/// Вступить/выйти/сменить гильдию можно только в столице — проверку делает CityPanel.
/// </summary>
public class GuildSystem : MonoBehaviour
{
    [Header("Зависимости")]
    [SerializeField] private PlayerInventory playerInventory;

    /// <summary>
    /// Текущая гильдия игрока. null = не состоит в гильдии.
    /// </summary>
    public GuildData CurrentGuild { get; private set; }

    public bool HasGuild => CurrentGuild != null;

    public event System.Action OnGuildChanged;

    private void Awake() => ValidateReferences();

    /// <summary>
    /// Попытаться вступить в гильдию. Списание денег происходит здесь.
    /// Возвращает false если уже в гильдии, не хватает денег или данные null.
    /// </summary>
    public bool TryJoinGuild(GuildData guild)
    {
        if (guild == null)
        {
            Debug.LogError("[GuildSystem] GuildData == null!");
            return false;
        }

        if (CurrentGuild == guild)
        {
            Debug.Log($"[GuildSystem] Игрок уже состоит в гильдии «{guild.guildName}».");
            return false;
        }

        if (!playerInventory.TrySpendMoney(guild.entryFee))
        {
            Debug.LogWarning($"[GuildSystem] Недостаточно денег для вступления в «{guild.guildName}». Нужно: {guild.entryFee}, есть: {playerInventory.Money}");
            return false;
        }

        // Если уже в другой гильдии — сначала выходим (без возврата денег)
        if (CurrentGuild != null)
        {
            Debug.Log($"[GuildSystem] Игрок покидает гильдию «{CurrentGuild.guildName}» ради «{guild.guildName}».");
        }

        CurrentGuild = guild;
        Debug.Log($"[GuildSystem] Игрок вступил в гильдию «{guild.guildName}». Заплачено: {guild.entryFee}");
        OnGuildChanged?.Invoke();
        return true;
    }

    /// <summary>
    /// Выйти из текущей гильдии. Деньги не возвращаются.
    /// </summary>
    public void LeaveGuild()
    {
        if (CurrentGuild == null)
        {
            Debug.Log("[GuildSystem] Игрок не состоит в гильдии — нечего покидать.");
            return;
        }

        Debug.Log($"[GuildSystem] Игрок покинул гильдию «{CurrentGuild.guildName}».");
        CurrentGuild = null;
        OnGuildChanged?.Invoke();
    }

    /// <summary>
    /// Сменить гильдию — синоним TryJoinGuild, но с явным именем для UI.
    /// </summary>
    public bool TrySwitchGuild(GuildData newGuild)
    {
        return TryJoinGuild(newGuild);
    }

    private void ValidateReferences()
    {
        if (playerInventory == null)
            playerInventory = GetComponent<PlayerInventory>();
        if (playerInventory == null)
            Debug.LogError("[GuildSystem] PlayerInventory не назначен и не найден на объекте!");
    }

    #region Скидки (заготовка — будет проработана позже)

    /// <summary>
    /// Проверяет, даёт ли текущая гильдия скидку на указанный товар.
    /// </summary>
    public bool HasDiscountFor(Item item)
    {
        if (CurrentGuild == null || item == null) return false;
        return CurrentGuild.discountedItems.Contains(item);
    }

    /// <summary>
    /// Возвращает множитель цены с учётом скидки гильдии (1.0 = без скидки).
    /// </summary>
    public float GetPriceMultiplier(Item item)
    {
        if (!HasDiscountFor(item)) return 1.0f;
        return 1.0f - CurrentGuild.discountPercent;
    }

    #endregion
}