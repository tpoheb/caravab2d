using UnityEngine;

[CreateAssetMenu(fileName = "New Unit", menuName = "Team/Unit Data")]
public class UnitData : ScriptableObject
{
    public string unitName;
    public Sprite icon;

    [Header("Bonuses")]
    public int attackBonus;
    public int bargainBonus;
    public int capacityBonus;

    [Header("Costs")]
    public int hireCost;
    public int salaryPerTurn;
}