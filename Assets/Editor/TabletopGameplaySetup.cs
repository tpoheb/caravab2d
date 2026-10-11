using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using Object = UnityEngine.Object;

/// <summary>Переносит игровые связи на настольную доску, сохраняя её камеру и исходную сцену.</summary>
public static class TabletopGameplaySetup
{
    private const string SourcePath = "Assets/Scenes/MapPrototipe.unity";
    private static readonly string[] GameplayRoots = {
        "EventSystem", "City", "Path's", "Player", "AI", "Canvas", "PauseMenu", "ApplicationFlow"
    };
    private static readonly Dictionary<string, string> _englishNames = new() {
        { "Камера — настольный обзор", "TabletopCamera" }, { "Стол и игровая доска", "TabletopBoard" },
        { "Города — только изображения", TabletopCameraStudySetup.CityVisualsName },
        { "Маршруты — декоративные фишки", TabletopCameraStudySetup.RouteVisualsName },
        { "Подсказки камеры", TabletopCameraStudySetup.CameraHintsName },
        { "Фишки настольных караванов", "TabletopTokens" }, { "Стол", "Table" },
        { "Толщина доски", "BoardEdge" }, { "Тёмная окантовка", "BoardBorder" }, { "Карта", "BoardMap" },
        { "Подставка", "CityBase" }, { "Фигурка города", "CityStandee" },
        { "Торец фишки", "DiscEdge" }, { "Круглая клетка", "CellFace" }
    };

    [MenuItem("Tools/Камера/Подключить игру к настольной доске")]
    public static void Run()
    {
        Require(!EditorApplication.isPlayingOrWillChangePlaymode, "Перенос доступен вне Play Mode.");
        var target = SceneManager.GetSceneByPath(TabletopCameraStudySetup.ScenePath);
        if (!target.IsValid()) target = EditorSceneManager.OpenScene(TabletopCameraStudySetup.ScenePath, OpenSceneMode.Additive);
        Require(!target.isDirty, "Сначала сохраните изменения настольной сцены.");
        Require(Components<GameManager>(target).Length == 0, "Игровая логика уже подключена к настольной сцене.");
        Require(!SceneManager.GetSceneByPath(SourcePath).IsValid(), "Закройте исходную сцену перед переносом.");

        var camera = Components<Camera>(target).Single();
        var rig = camera.GetComponent<TabletopCameraRig>();
        string cameraSettings = EditorJsonUtility.ToJson(camera);
        string rigSettings = EditorJsonUtility.ToJson(rig);
        var source = EditorSceneManager.OpenScene(SourcePath, OpenSceneMode.Additive);
        try
        {
            var roots = source.GetRootGameObjects();
            var selected = GameplayRoots.Select(name => roots.Single(root => root.name == name)).ToArray();
            var cityManager = Components<CityManager>(source).Single();
            var cities = cityManager.AllCities.ToArray();
            var paths = Components<PathCellInitializer>(source);
            var sceneIds = cities.Cast<Component>().Concat(paths).ToDictionary(value => value, GameSaveService.SceneId);
            var sourceCamera = Components<Camera>(source).Single();
            var replacements = new Dictionary<Object, Object> { { sourceCamera.gameObject, camera.gameObject } };
            foreach (var component in sourceCamera.GetComponents<Component>())
            {
                var replacement = camera.GetComponent(component.GetType());
                if (replacement != null) replacements[component] = replacement;
            }
            RemapCamera(selected, replacements);
            foreach (var root in selected) SceneManager.MoveGameObjectToScene(root, target);

            var cityArt = Root(target, TabletopCameraStudySetup.CityVisualsName).transform;
            Require(cities.Length == cityArt.childCount, "Число городов на доске отличается от игровых данных.");
            for (int i = 0; i < cities.Length; i++)
            {
                cities[i].transform.position = cityArt.GetChild(i).position;
                HideOldGeometry(cities[i].gameObject);
            }
            ConnectRoads(target, cities);
            var player = Components<PlayerToken>(target).Single();
            player.transform.position = (Read(player, "startCity") as City).transform.position;
            HideOldGeometry(player.gameObject);
            CreateTokens(target, player);
            ConfigureCityLabels(target, cities);
            var hints = Root(target, TabletopCameraStudySetup.CameraHintsName).GetComponentsInChildren<TMP_Text>(true);
            hints.First().text = "1000 ДОРОГ · НАСТОЛЬНАЯ КАРТА";
            foreach (var hint in hints) hint.raycastTarget = false;

            foreach (var entry in sceneIds)
                Require(GameSaveService.SceneId(entry.Key) == entry.Value, "Изменился идентификатор сохранения: " + entry.Value);
            Require(EditorJsonUtility.ToJson(camera) == cameraSettings && EditorJsonUtility.ToJson(rig) == rigSettings,
                "Настройки камеры должны остаться прежними.");
            Validate(target);
            Require(EditorSceneManager.SaveScene(target), "Не удалось сохранить настольную сцену.");
            var buildScenes = EditorBuildSettings.scenes.Where(scene => scene.path != GameMenuSetup.GamePath).ToList();
            int sourceIndex = buildScenes.FindIndex(scene => scene.path == SourcePath);
            if (sourceIndex >= 0)
            {
                buildScenes[sourceIndex].enabled = false;
                buildScenes.Insert(sourceIndex, new EditorBuildSettingsScene(GameMenuSetup.GamePath, true));
            }
            else buildScenes.Add(new EditorBuildSettingsScene(GameMenuSetup.GamePath, true));
            EditorBuildSettings.scenes = buildScenes.ToArray();
            AssetDatabase.SaveAssets();
            Debug.Log($"TABLETOP_GAMEPLAY_TRANSFER_PASSED: {cities.Length} городов, {paths.Length} направленных маршрутов; камера сохранена.");
        }
        finally { EditorSceneManager.CloseScene(source, true); }
        SceneManager.SetActiveScene(target);
    }

    private static void ConnectRoads(Scene scene, City[] cities)
    {
        var visuals = Root(scene, TabletopCameraStudySetup.RouteVisualsName).transform;
        var numbers = cities.Select((city, i) => (city, i)).ToDictionary(pair => pair.city, pair => pair.i);
        foreach (var city in cities)
        foreach (var path in city.Paths)
        {
            int from = numbers[city], to = numbers[path.FinishCity];
            Require(from != to, "Маршрут не должен возвращаться в тот же город.");
            var road = BoardRoad(visuals, from, to, out bool reversed);
            Require(road != null && road.childCount > 1, "На доске отсутствуют клетки маршрута: " + path.name);
            var properties = new SerializedObject(path);
            var cells = properties.FindProperty("_routeCells");
            cells.arraySize = road.childCount;
            for (int i = 0; i < road.childCount; i++)
            {
                int index = reversed ? road.childCount - 1 - i : i;
                var cell = road.GetChild(index).gameObject;
                cell.tag = "Cell";
                var data = cell.GetComponent<Cell>();
                if (data == null) data = cell.AddComponent<Cell>();
                data.cellNumber = index;
                cells.GetArrayElementAtIndex(i).objectReferenceValue = data;
            }
            properties.ApplyModifiedPropertiesWithoutUndo();
            // Старая геометрия клеток больше не нужна: движение идёт по клеткам настольного макета.
            foreach (Transform cell in path.transform.Cast<Transform>().ToArray())
                Object.DestroyImmediate(cell.gameObject);
        }
    }

    private static Transform BoardRoad(Transform visuals, int from, int to, out bool reversed)
    {
        var road = visuals.Find($"Road{from + 1:00}_{to + 1:00}");
        reversed = road == null;
        return road != null ? road : visuals.Find($"Road{to + 1:00}_{from + 1:00}");
    }

    private static void CreateTokens(Scene scene, PlayerToken player)
    {
        var root = new GameObject("TabletopTokens");
        SceneManager.MoveGameObjectToScene(root, scene);
        var visuals = root.AddComponent<TabletopTokenVisuals>();
        var properties = new SerializedObject(visuals);
        properties.FindProperty("_player").objectReferenceValue = player;
        properties.FindProperty("_aiTurns").objectReferenceValue = Components<AITurnManager>(scene).Single();
        properties.FindProperty("_pawnMesh").objectReferenceValue = Resources.GetBuiltinResource<Mesh>("Capsule.fbx");
        properties.FindProperty("_playerMaterial").objectReferenceValue = PawnMaterial("Караван игрока", new Color(.15f, .65f, .95f));
        properties.FindProperty("_aiMaterial").objectReferenceValue = PawnMaterial("Караван ИИ", new Color(.9f, .25f, .15f));
        properties.ApplyModifiedPropertiesWithoutUndo();
    }

    private static Material PawnMaterial(string name, Color color)
    {
        string path = "Assets/Art/CameraStudy/Materials/" + name + ".mat";
        var material = AssetDatabase.LoadAssetAtPath<Material>(path);
        if (material == null)
        {
            material = new Material(Shader.Find("Caravan/CameraStudy/Solid"));
            AssetDatabase.CreateAsset(material, path);
        }
        material.SetColor("_BaseColor", color);
        EditorUtility.SetDirty(material);
        return material;
    }

    private static void HideOldGeometry(GameObject root)
    {
        foreach (var renderer in root.GetComponentsInChildren<Renderer>(true)) renderer.enabled = false;
        foreach (var collider in root.GetComponentsInChildren<Collider>(true)) collider.enabled = false;
    }

    private static void RemapCamera(GameObject[] roots, Dictionary<Object, Object> replacements)
    {
        foreach (var component in roots.SelectMany(root => root.GetComponentsInChildren<Component>(true)))
        {
            Require(component != null, "В исходной сцене отсутствует скрипт.");
            var serialized = new SerializedObject(component);
            var property = serialized.GetIterator();
            while (property.Next(true))
                if (property.propertyType == SerializedPropertyType.ObjectReference &&
                    property.objectReferenceValue != null && replacements.TryGetValue(property.objectReferenceValue, out var replacement))
                    property.objectReferenceValue = replacement;
            serialized.ApplyModifiedPropertiesWithoutUndo();
        }
    }

    [MenuItem("Tools/Камера/Обновить подписи и имена объектов настольной сцены")]
    public static void UpdatePresentation()
    {
        Require(!EditorApplication.isPlayingOrWillChangePlaymode, "Обновляйте сцену вне Play Mode.");
        var scene = SceneManager.GetSceneByPath(TabletopCameraStudySetup.ScenePath);
        if (!scene.IsValid()) scene = EditorSceneManager.OpenScene(TabletopCameraStudySetup.ScenePath, OpenSceneMode.Additive);
        Require(!scene.isDirty, "Сначала сохраните изменения настольной сцены.");
        var cities = Components<CityManager>(scene).Single().AllCities.ToArray();
        var ids = cities.Cast<Component>().Concat(Components<PathCellInitializer>(scene)).ToDictionary(value => value, GameSaveService.SceneId);
        var camera = Components<Camera>(scene).Single();
        string settings = EditorJsonUtility.ToJson(camera);
        string rigSettings = EditorJsonUtility.ToJson(camera.GetComponent<TabletopCameraRig>());
        string cameraTransform = EditorJsonUtility.ToJson(camera.transform);
        foreach (var value in Components<Transform>(scene))
        {
            var parts = Regex.Match(value.name, @"^(.*?)( \(\d+\))?$");
            if (_englishNames.TryGetValue(parts.Groups[1].Value, out string translated))
                value.name = translated + parts.Groups[2].Value;
            value.name = Regex.Replace(value.name, @"^Город (\d+)$", "City$1");
            value.name = Regex.Replace(value.name, @"^Дорога (\d+) — (\d+)$", "Road$1_$2");
            value.name = Regex.Replace(value.name, @"^Фишка (\d+)$", "Cell$1");
        }
        var hints = Root(scene, TabletopCameraStudySetup.CameraHintsName).GetComponentsInChildren<TMP_Text>(true);
        for (int i = 0; i < hints.Length; i++) hints[i].name = i == 0 ? "Title" : "Controls";
        ConfigureCityLabels(scene, cities);
        foreach (var entry in ids) Require(GameSaveService.SceneId(entry.Key) == entry.Value, "Изменился идентификатор сохранения: " + entry.Value);
        Require(settings == EditorJsonUtility.ToJson(camera) && rigSettings == EditorJsonUtility.ToJson(camera.GetComponent<TabletopCameraRig>()) &&
            cameraTransform == EditorJsonUtility.ToJson(camera.transform), "Настройки камеры должны остаться прежними.");
        Validate(scene);
        Require(EditorSceneManager.SaveScene(scene), "Не удалось сохранить настольную сцену.");
        AssetDatabase.SaveAssets();
        Debug.Log("TABLETOP_PRESENTATION_UPDATED: 18 подписей городов, английские имена объектов; камера и маршруты сохранены.");
    }

    private static void ConfigureCityLabels(Scene scene, City[] cities)
    {
        var cityArt = Root(scene, TabletopCameraStudySetup.CityVisualsName).transform;
        const float scale = .09f;
        Material material = null;
        for (int i = 0; i < cities.Length; i++)
        {
            var node = cities[i].GetComponentsInChildren<CityNode>(true).Single();
            var label = Read(node, "label") as TextMeshPro;
            Require(label != null, "У города отсутствует подпись прототипа: " + cities[i].name);
            if (material == null)
            {
                const string path = "Assets/Art/CameraStudy/Materials/CityLabels.mat";
                material = AssetDatabase.LoadAssetAtPath<Material>(path);
                if (material == null)
                {
                    material = new Material(label.fontSharedMaterial) { name = "CityLabels" };
                    material.SetFloat("_OutlineWidth", .18f);
                    material.SetColor("_OutlineColor", new Color(.035f, .025f, .015f, 1));
                    AssetDatabase.CreateAsset(material, path);
                }
            }
            node.cityName = cities[i].CityName;
            node.enabled = true;
            label.gameObject.SetActive(true);
            label.enabled = true;
            label.text = node.cityName;
            label.fontSharedMaterial = material;
            label.color = MenuUIBuilder.Cream;
            label.fontSize = 32;
            label.fontSizeMin = 26;
            label.fontSizeMax = 32;
            label.enableAutoSizing = true;
            label.textWrappingMode = TextWrappingModes.NoWrap;
            label.overflowMode = TextOverflowModes.Overflow;
            label.alignment = TextAlignmentOptions.Center;
            label.raycastTarget = false;
            label.rectTransform.sizeDelta = new Vector2(36, 5);
            var parentScale = label.transform.parent.lossyScale;
            label.transform.localScale = new Vector3(scale / parentScale.x, scale / parentScale.y, scale / parentScale.z);
            label.renderer.enabled = true;
            label.renderer.sortingOrder = 20;
            var anchor = label.GetComponent<TabletopCityLabel>();
            if (anchor == null) anchor = label.gameObject.AddComponent<TabletopCityLabel>();
            anchor.Configure(cityArt.GetChild(i).GetComponentsInChildren<SpriteRenderer>(true).Single());
            label.ForceMeshUpdate();
            EditorUtility.SetDirty(node);
            EditorUtility.SetDirty(label);
            PrefabUtility.RecordPrefabInstancePropertyModifications(node);
            PrefabUtility.RecordPrefabInstancePropertyModifications(label);
            PrefabUtility.RecordPrefabInstancePropertyModifications(label.transform);
            PrefabUtility.RecordPrefabInstancePropertyModifications(label.renderer);
        }
    }

    [MenuItem("Tools/Проверки/Связи настольной игровой сцены")]
    public static void Check()
    {
        var scene = EditorSceneManager.OpenPreviewScene(GameMenuSetup.GamePath);
        try { Validate(scene); Debug.Log("TABLETOP_GAMEPLAY_SCENE_CHECKS_PASSED"); }
        finally { EditorSceneManager.ClosePreviewScene(scene); }
    }

    private static void Validate(Scene scene)
    {
        Require(Components<GameManager>(scene).Length == 1 && Components<PlayerToken>(scene).Length == 1 &&
            Components<CardManager>(scene).Length == 1 && Components<AITurnManager>(scene).Length == 1,
            "На сцене требуется по одному экземпляру основных игровых систем.");
        Require(Components<Camera>(scene).Length == 1 && Components<TabletopCameraRig>(scene).Length == 1 &&
            Components<IsoCameraFollow>(scene).Length == 0, "На сцене должна остаться только настольная камера.");
        var cities = Components<CityManager>(scene).Single().AllCities;
        Require(cities.Count == 18 && cities.All(city => city != null && city.Data != null), "Требуются данные восемнадцати городов.");
        var paths = Components<PathCellInitializer>(scene);
        Require(paths.Length == 36, "Требуются тридцать шесть направленных маршрутов.");
        var visuals = Root(scene, TabletopCameraStudySetup.RouteVisualsName).transform;
        var cityArt = Root(scene, TabletopCameraStudySetup.CityVisualsName).transform;
        for (int i = 0; i < cities.Count; i++)
        {
            var node = cities[i].GetComponentsInChildren<CityNode>(true).Single();
            var label = Read(node, "label") as TextMeshPro;
            var anchor = label != null ? label.GetComponent<TabletopCityLabel>() : null;
            Require(node.enabled && node.cityName == cities[i].CityName && label != null && label.enabled && label.renderer.enabled && label.text == node.cityName &&
                anchor != null && anchor.Standee != null && anchor.Standee.transform.IsChildOf(cityArt.GetChild(i)), "Подпись должна показывать название своего города: " + cities[i].name);
        }
        Require(Components<Transform>(scene).All(value => !Regex.IsMatch(value.name, "[А-Яа-яЁё]")), "Имена объектов сцены должны быть английскими.");
        var numbers = cities.Select((city, i) => (city, i)).ToDictionary(pair => pair.city, pair => pair.i);
        foreach (var city in cities)
        foreach (var path in city.Paths)
        {
            Require(path != null && cities.Contains(path.FinishCity), "Маршрут должен вести в игровой город.");
            var road = BoardRoad(visuals, numbers[city], numbers[path.FinishCity], out bool reversed);
            Require(road != null && path.CellCount == road.childCount && path.transform.childCount == 0,
                "Число ходов должно соответствовать исходным клеткам настольной дороги: " + path.name);
            var reverse = path.FinishCity.Paths.SingleOrDefault(value => value.FinishCity == city);
            Require(reverse != null && reverse.CellCount == path.CellCount, "Обратный маршрут должен иметь столько же клеток.");
            for (int i = 0; i < path.CellCount; i++)
            {
                var cell = path.GetCell(i);
                Require(cell != null && cell.CompareTag("Cell") && cell.transform == road.GetChild(reversed ? road.childCount - 1 - i : i),
                    "Движение должно использовать исходную клетку доски: " + path.name);
                Require(cell == reverse.GetCell(path.CellCount - 1 - i), "Обратный маршрут должен использовать те же клетки в обратном порядке.");
            }
        }
        foreach (var component in Components<Component>(scene))
        {
            Require(component != null, "На сцене отсутствует скрипт.");
            var property = new SerializedObject(component).GetIterator();
            while (property.Next(true))
            {
                if (property.propertyType != SerializedPropertyType.ObjectReference) continue;
                var reference = property.objectReferenceValue;
                var owner = reference as GameObject ?? (reference as Component)?.gameObject;
                Require(owner == null || EditorUtility.IsPersistent(owner) || owner.scene == scene,
                    $"Ссылка ведёт в другую сцену: {component.name}.{property.propertyPath}");
                Require(reference != null || property.objectReferenceInstanceIDValue == 0,
                    $"Потеряна ссылка: {component.name}.{property.propertyPath}");
            }
        }
    }

    private static Object Read(Object value, string property) => new SerializedObject(value).FindProperty(property).objectReferenceValue;
    private static GameObject Root(Scene scene, string name) => scene.GetRootGameObjects().Single(root => root.name == name);
    private static T[] Components<T>(Scene scene) where T : Component => scene.GetRootGameObjects().SelectMany(root => root.GetComponentsInChildren<T>(true)).ToArray();
    private static void Require(bool condition, string message) { if (!condition) throw new InvalidOperationException(message); }
}
