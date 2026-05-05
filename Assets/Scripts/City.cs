using UnityEngine;
using System.Collections.Generic; // Обязательно для использования List

public class City : MonoBehaviour
{
    [Header("Оригинальные данные (Ассет ScriptableObject)")]
    [Tooltip("Перетащите сюда ассет CityData из окна Project")]
    [SerializeField] private CityData sourceData;

    [Header("Маршруты")]
    [Tooltip("Список путей, исходящих из этого города")]
    // Добавляем список путей, который ищет CityPanel
    public List<PathCellInitializer> Paths = new List<PathCellInitializer>();

    [Header("Данные для текущей сессии (Runtime)")]
    [Tooltip("Эта переменная будет хранить копию данных во время игры")]
    [HideInInspector] public CityData RuntimeData;

    // Свойство для получения имени
    public string CityName => RuntimeData != null ? RuntimeData.cityName : (sourceData != null ? sourceData.cityName : "Unknown City");

    /// <summary>
    /// Создает независимую копию CityData для использования в PlayMode.
    /// Вызывается из CityManager при старте игры.
    /// </summary>
    public void InitRuntimeData()
    {
        if (sourceData != null)
        {
            RuntimeData = Instantiate(sourceData);
            RuntimeData.name = sourceData.name + "_RuntimeCopy"; 
        }
        else
        {
            Debug.LogError($"[City] На объекте {gameObject.name} не назначен sourceData (CityData)!");
        }
    }
}