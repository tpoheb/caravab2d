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

    /// <summary>Актуальные данные города (runtime-копия, если есть, иначе исходный ассет).</summary>
    public CityData Data => RuntimeData != null ? RuntimeData : sourceData;

    /// <summary>Информация о городе.</summary>
    public string Info => Data != null ? Data.info : string.Empty;

    /// <summary>Список слухов в городе (никогда не null).</summary>
    public List<string> Rumors => Data != null && Data.rumors != null ? Data.rumors : new List<string>();

    /// <summary>Список юнитов, доступных для найма в этом городе (никогда не null).</summary>
    public List<UnitData> AvailableUnits => Data != null && Data.availableUnits != null ? Data.availableUnits : new List<UnitData>();

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

            // Спрос пересчитывается автоматически на основе цен,
            // ручные значения из ассета не должны влиять на стрелки.
            if (RuntimeData.items != null)
            {
                foreach (var item in RuntimeData.items)
                {
                    item?.UpdateDemand();
                }
            }
        }
        else
        {
            Debug.LogError($"[City] На объекте {gameObject.name} не назначен sourceData (CityData)!");
        }
    }
}