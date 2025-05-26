using UnityEngine;

[CreateAssetMenu(fileName = "NewCard", menuName = "Cards/Card Data")]
public class CardData : ScriptableObject
{
    public string cardName;
    public Sprite icon;
    [TextArea] public string description;

    // Настраиваемые эффекты
    public int moneyChange;
    public int attackBonus;
    // Добавьте другие параметры по необходимости

    public void ApplyEffects(PlayerInventory player)
    {
        player.Money += moneyChange;
        Debug.Log($"Применена карта: {cardName}");
    }

    /*public void ApplyEffects(PlayerStats player)
    {
       
        player.Attack += attackBonus;
        Debug.Log($"Применена карта: {cardName}");
    }*/
}