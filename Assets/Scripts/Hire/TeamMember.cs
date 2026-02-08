[System.Serializable]
public class TeamMember
{
    public UnitData unitData;

    public TeamMember(UnitData data)
    {
        unitData = data;
    }

    public void ApplyBonuses(PlayerStats stats)
    {
        stats.ModifyAttack(unitData.attackBonus);
        stats.ModifyBargain(unitData.bargainBonus);
        stats.ModifyCapacity(unitData.capacityBonus);
    }

    public void RemoveBonuses(PlayerStats stats)
    {
        stats.ModifyAttack(-unitData.attackBonus);
        stats.ModifyBargain(-unitData.bargainBonus);
        stats.ModifyCapacity(-unitData.capacityBonus);
    }
}