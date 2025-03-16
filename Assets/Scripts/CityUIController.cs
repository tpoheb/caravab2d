using UnityEngine;

public class CityUIController : MonoBehaviour
{
    public CityManager cityManager;

    public void OnBuyItemButtonClicked(string itemName)
    {
        cityManager.BuyItem(itemName);
    }

    public void OnHireCrewButtonClicked()
    {
        cityManager.HireCrew();
    }

    public void OnCloseButtonClicked()
    {
        gameObject.SetActive(false);
    }
}