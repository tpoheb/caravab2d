using System.Collections.Generic;

[System.Serializable]
public class TradeData
{
    public List<City> cities = new List<City>();
    public List<Product> products = new List<Product>();

    [System.Serializable]
    public class City
    {
        public string name;
        public List<Price> prices = new List<Price>();
    }

    [System.Serializable]
    public class Product
    {
        public string id;
        public string name;
    }

    [System.Serializable]
    public class Price
    {
        public string productId;
        public int buyPrice;
        public int sellPrice;
    }
}