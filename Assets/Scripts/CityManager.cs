using UnityEngine;

public class CityManager : MonoBehaviour
{
    public void BuyItem(string itemName)
    {
        Debug.Log($"������ �������: {itemName}");
        // ������ �������
    }

    public void HireCrew()
    {
        Debug.Log("������� ������!");
        // ������ ����� �������
    }
}