using UnityEngine;
using UnityEngine.UI;
using System.Linq;

public class TradeSystem : MonoBehaviour
{
    public TradeDataSO tradeDataSO; // ScriptableObject с данными
    public GameObject tradePanel; // Панель торговли
    public GameObject tradeItemRowPrefab; // Префаб строки товара
    public Transform tradeContent; // Родительский объект для строк товаров

    private TradeData tradeData; // Данные о товарах

    void Start()
    {
        tradePanel.SetActive(false);

        // Получаем данные из ScriptableObject
        if (tradeDataSO != null)
        {
            tradeData = tradeDataSO.tradeData;
        }
        else
        {
            Debug.LogError("TradeDataSO не назначен!");
        }
    }

    // Открытие панели торговли
    public void OpenTradePanel()
    {
        tradePanel.SetActive(true);
        CreateTradeRows();
    }

    // Закрытие панели торговли
    public void CloseTradePanel()
    {
        tradePanel.SetActive(false);
    }

    // Создание строк товаров
    private void CreateTradeRows()
    {
        foreach (Transform child in tradeContent)
        {
            Destroy(child.gameObject);
        }

        foreach (var item in tradeData.items)
        {
            // Создаем экземпляр префаба
            GameObject row = Instantiate(tradeItemRowPrefab, tradeContent);

            // Получаем компонент TradeItemRow и инициализируем его
            TradeItemRow rowScript = row.GetComponent<TradeItemRow>();
            if (rowScript != null)
            {
                rowScript.Initialize(item);
            }
            else
            {
                Debug.LogError("Компонент TradeItemRow не найден в префабе!");
            }
        }
    }
}
