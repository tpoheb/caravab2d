using UnityEngine;

[CreateAssetMenu(fileName = "New Item", menuName = "Trade/Item")]
public class Item : ScriptableObject
{
    public string itemName;
    public Sprite icon;
    public int weight = 1;
}