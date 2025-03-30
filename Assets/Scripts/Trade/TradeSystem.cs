using UnityEngine;
using UnityEngine.UI;
using System.Linq;

public class TradeSystem : MonoBehaviour
{
    public TradeDataSO tradeDataSO; // ScriptableObject � �������
    public GameObject tradePanel; // ������ ��������
    public GameObject tradeItemRowPrefab; // ������ ������ ������
    public Transform tradeContent; // ������������ ������ ��� ����� �������

    private TradeData tradeData; // ������ � �������

    void Start()
    {
        tradePanel.SetActive(false);

        // �������� ������ �� ScriptableObject
        if (tradeDataSO != null)
        {
            tradeData = tradeDataSO.tradeData;
        }
        else
        {
            Debug.LogError("TradeDataSO �� ��������!");
        }
    }

    // �������� ������ ��������
    public void OpenTradePanel()
    {
        tradePanel.SetActive(true);
        CreateTradeRows();
    }

    // �������� ������ ��������
    public void CloseTradePanel()
    {
        tradePanel.SetActive(false);
    }

    // �������� ����� �������
    private void CreateTradeRows()
    {
        foreach (Transform child in tradeContent)
        {
            Destroy(child.gameObject);
        }

        foreach (var item in tradeData.items)
        {
            // ������� ��������� �������
            GameObject row = Instantiate(tradeItemRowPrefab, tradeContent);

            // �������� ��������� TradeItemRow � �������������� ���
            TradeItemRow rowScript = row.GetComponent<TradeItemRow>();
            if (rowScript != null)
            {
                rowScript.Initialize(item);
            }
            else
            {
                Debug.LogError("��������� TradeItemRow �� ������ � �������!");
            }
        }
    }
}
