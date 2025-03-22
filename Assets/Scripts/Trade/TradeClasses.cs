using System;

[Serializable]
public class Item
{
    public string itemName;
    public int price;
    public int quantityInCity;
    public int quantityInPlayerInventory;
}

[Serializable]
public class TradeData
{
    public Item[] items;
}