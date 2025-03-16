using UnityEngine;

public class CityManager : MonoBehaviour
{
    public void BuyItem(string itemName)
    {
        Debug.Log($"Куплен предмет: {itemName}");
        // Логика покупки
    }

    public void HireCrew()
    {
        Debug.Log("Команда нанята!");
        // Логика найма команды
    }
}