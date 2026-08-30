using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;

/// <summary>
/// Карта — города расставлены вручную в Editor (CityMapMarker на каждой метке).
/// Скрипт строит пути между городами и отображает маркеры игрока и ИИ.
/// </summary>
public class MapPanelUI : MonoBehaviour
{
    [Header("Панель")]
    [SerializeField] private GameObject mapPanel;
    [SerializeField] private Button closeButton;

    [Header("Контейнер для путей и маркеров")]
    [SerializeField] private RectTransform dynamicContainer;

    [Header("Префабы")]
    [SerializeField] private GameObject pathLinePrefab;
    [SerializeField] private GameObject playerMarkerPrefab;
    [SerializeField] private GameObject aiMarkerPrefab;

    [Header("Настройки пути")]
    [SerializeField] private float pathLineHeight = 4f;

    [Header("Системы")]
    [SerializeField] private CityManager cityManager;
    [SerializeField] private AITurnManager aiTurnManager;

    private readonly Dictionary<City, RectTransform> _cityMarkers = new();
    private readonly List<GameObject> _staticMarkers = new();  // пути — строятся один раз
    private readonly List<GameObject> _dynamicMarkers = new(); // игрок и ИИ
    private bool _pathsBuilt = false;

    // ------------------------------------------------------------------
    // Жизненный цикл
    // ------------------------------------------------------------------

    private void Awake()
    {
        if (mapPanel == null)         Debug.LogError("[MapPanelUI] mapPanel не назначен");
        if (dynamicContainer == null) Debug.LogError("[MapPanelUI] dynamicContainer не назначен");
        if (cityManager == null)      Debug.LogError("[MapPanelUI] cityManager не назначен");
        if (closeButton == null)      Debug.LogWarning("[MapPanelUI] closeButton не назначен");
    }

    private void Start()
    {
        if (closeButton != null)
            closeButton.onClick.AddListener(CloseMap);

        if (mapPanel != null)
            mapPanel.SetActive(false);

        CollectCityMarkers();
    }

    private void OnDestroy()
    {
        if (closeButton != null)
            closeButton.onClick.RemoveListener(CloseMap);
    }

    // ------------------------------------------------------------------
    // Публичный API
    // ------------------------------------------------------------------

    public void OpenMap()
    {
        if (mapPanel == null) return;
        mapPanel.SetActive(true);

        if (!_pathsBuilt)
        {
            BuildPaths();
            _pathsBuilt = true;
        }

        BuildDynamicMarkers();
    }

    public void CloseMap()
    {
        if (mapPanel != null)
            mapPanel.SetActive(false);
        ClearDynamicMarkers();
    }

    // ------------------------------------------------------------------
    // Сбор меток городов
    // ------------------------------------------------------------------

    private void CollectCityMarkers()
    {
        _cityMarkers.Clear();

        var markers = mapPanel.GetComponentsInChildren<CityMapMarker>(true);
        foreach (var marker in markers)
        {
            if (marker.City == null)
            {
                Debug.LogWarning($"[MapPanelUI] Метка {marker.gameObject.name} без ссылки на City");
                continue;
            }
            _cityMarkers[marker.City] = marker.GetComponent<RectTransform>();
        }

        Debug.Log($"[MapPanelUI] Найдено меток: {_cityMarkers.Count}");
    }

    // ------------------------------------------------------------------
    // Пути между городами
    // ------------------------------------------------------------------

    private void BuildPaths()
    {
        if (pathLinePrefab == null || cityManager == null) return;

        var cities = cityManager.AllCities;
        if (cities == null) return;

        var drawnPairs = new HashSet<(int, int)>();

        foreach (var city in cities)
        {
            if (city == null || city.Paths == null) continue;
            if (!_cityMarkers.TryGetValue(city, out var rtA)) continue;

            foreach (var path in city.Paths)
            {
                if (path == null || path.FinishCity == null) continue;

                var other = path.FinishCity;
                if (!_cityMarkers.TryGetValue(other, out var rtB)) continue;

                int idA = city.GetEntityId();
                int idB = other.GetEntityId();
                if (idA == idB) continue;

                var pair = idA < idB ? (idA, idB) : (idB, idA);
                if (!drawnPairs.Add(pair)) continue;

                SpawnPathLine(rtA, rtB);
            }
        }
    }

    private void SpawnPathLine(RectTransform rtA, RectTransform rtB)
    {
        var go = Instantiate(pathLinePrefab, dynamicContainer);
        var rt = go.GetComponent<RectTransform>();

        rt.anchorMin = new Vector2(0.5f, 0.5f);
        rt.anchorMax = new Vector2(0.5f, 0.5f);
        rt.pivot     = new Vector2(0.5f, 0.5f);

        Vector2 posA = rtA.anchoredPosition;
        Vector2 posB = rtB.anchoredPosition;
        Vector2 dir  = posB - posA;

        rt.anchoredPosition = (posA + posB) * 0.5f;
        rt.sizeDelta        = new Vector2(dir.magnitude, pathLineHeight);
        rt.rotation         = Quaternion.Euler(0f, 0f, Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg);

        _staticMarkers.Add(go);
    }

    // ------------------------------------------------------------------
    // Динамические маркеры (игрок и ИИ)
    // ------------------------------------------------------------------

    private void BuildDynamicMarkers()
    {
        ClearDynamicMarkers();
        SpawnPlayerMarker();
        SpawnAiMarkers();
    }

    private void SpawnPlayerMarker()
    {
        if (playerMarkerPrefab == null || cityManager == null) return;

        var city = cityManager.PlayerCurrentCity;
        if (city == null)
        {
            Debug.LogWarning("[MapPanelUI] Текущий город игрока не определён");
            return;
        }

        if (!_cityMarkers.TryGetValue(city, out var markerRt))
        {
            Debug.LogWarning($"[MapPanelUI] Нет метки для {city.CityName}");
            return;
        }

        SpawnMarkerAt(playerMarkerPrefab, markerRt);
    }

    private void SpawnAiMarkers()
    {
        if (aiMarkerPrefab == null || aiTurnManager == null) return;

        foreach (var trader in aiTurnManager.AiTraders)
        {
            if (trader == null || trader.CurrentCity == null) continue;

            if (!_cityMarkers.TryGetValue(trader.CurrentCity, out var markerRt))
            {
                Debug.LogWarning($"[MapPanelUI] Нет метки для {trader.CurrentCity.CityName}");
                continue;
            }

            SpawnMarkerAt(aiMarkerPrefab, markerRt);
        }
    }

    private void SpawnMarkerAt(GameObject prefab, RectTransform targetRt)
    {
        var go = Instantiate(prefab, dynamicContainer);
        var rt = go.GetComponent<RectTransform>();

        rt.anchorMin        = new Vector2(0.5f, 0.5f);
        rt.anchorMax        = new Vector2(0.5f, 0.5f);
        rt.pivot            = new Vector2(0.5f, 0.5f);
        rt.localScale       = Vector3.one;
        rt.anchoredPosition = targetRt.anchoredPosition;

        _dynamicMarkers.Add(go);
    }

    // ------------------------------------------------------------------
    // Очистка
    // ------------------------------------------------------------------

    private void ClearDynamicMarkers()
    {
        foreach (var go in _dynamicMarkers)
            if (go != null) Destroy(go);
        _dynamicMarkers.Clear();
    }

    private void ClearAllMarkers()
    {
        foreach (var go in _staticMarkers)
            if (go != null) Destroy(go);
        _staticMarkers.Clear();
        _pathsBuilt = false;

        ClearDynamicMarkers();
    }
}