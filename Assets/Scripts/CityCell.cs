using UnityEngine;

public class CityCell : MonoBehaviour
{
    public string cityName; // �������� ������
    public GameObject cityUI; // ������ �� UI ���� ������

    public void OnPlayerEnter()
    {
        Debug.Log($"����� ����� � �����: {cityName}");
        OpenCityUI();
    }

    private void OpenCityUI()
    {
        if (cityUI != null)
        {
            cityUI.SetActive(true); // ��������� UI ������
        }
    }
}