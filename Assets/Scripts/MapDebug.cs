using UnityEngine;

public class MapDebug : MonoBehaviour
{
    [SerializeField] private CityManager cityManager;

    private void Start()
    {
        foreach (var city in cityManager.AllCities)
        {
            if (city == null) continue;
            Debug.Log($"[MapDebug] {city.CityName} | " +
                      $"world: {city.transform.position} | " +
                      $"parent: {city.transform.parent?.name} | " +
                      $"parentPos: {city.transform.parent?.position}");
        }
    }
}