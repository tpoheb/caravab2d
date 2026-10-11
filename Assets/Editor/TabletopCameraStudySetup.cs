using System;
using System.IO;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.UI;

/// <summary>Создаёт настольную доску и подключает к ней игровые системы.</summary>
public static class TabletopCameraStudySetup
{
    public const string ScenePath = "Assets/Scenes/TabletopCameraStudy.unity";
    public const string CityVisualsName = "CityVisuals";
    public const string RouteVisualsName = "RouteVisuals";
    public const string CameraHintsName = "CameraHints";
    private const string ArtPath = "Assets/Art/CameraStudy/";
    private const float PieceScale = .5f;
    private const float PieceHeight = .0625f;
    // Положение городов по схеме: X слева направо, Y сверху вниз.
    private static readonly Vector2[] Cities = {
        new Vector2(.453f, .608f), new Vector2(.555f, .621f), new Vector2(.746f, .642f),
        new Vector2(.918f, .602f), new Vector2(.555f, .791f), new Vector2(.758f, .883f),
        new Vector2(.392f, .858f), new Vector2(.212f, .858f), new Vector2(.457f, .440f),
        new Vector2(.623f, .384f), new Vector2(.644f, .143f), new Vector2(.453f, .182f),
        new Vector2(.874f, .118f), new Vector2(.318f, .515f), new Vector2(.335f, .243f),
        new Vector2(.082f, .281f), new Vector2(.084f, .642f), new Vector2(.224f, .727f)
    };
    // Индексы городов на единицу меньше номеров на схеме; один переход на каждую пару.
    private static readonly Vector2Int[] Roads = {
        new Vector2Int(0, 1), new Vector2Int(1, 2), new Vector2Int(2, 3),
        new Vector2Int(1, 4), new Vector2Int(2, 5), new Vector2Int(0, 6),
        new Vector2Int(6, 7), new Vector2Int(0, 8), new Vector2Int(8, 9),
        new Vector2Int(9, 10), new Vector2Int(10, 11), new Vector2Int(11, 8),
        new Vector2Int(10, 12), new Vector2Int(0, 13), new Vector2Int(13, 14),
        new Vector2Int(13, 17), new Vector2Int(17, 15), new Vector2Int(15, 16)
    };

    [MenuItem("Tools/Камера/Создать настольный макет")]
    public static void CreateScene()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode)
            throw new InvalidOperationException("Создавайте макет вне Play Mode.");
        if (!Application.isBatchMode && !EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
        AssetDatabase.Refresh();
        Directory.CreateDirectory(ArtPath + "Materials");
        AssetDatabase.Refresh();
        var map = ImportSprite("BoardMap.png", new Vector2(.5f, .5f));
        var city = ImportSprite("CityStandee.png", new Vector2(.5f, .1022f));
        var disc = ImportSprite("RouteDisc.png", new Vector2(.5f, .5f));
        var wood = Material("Тёмный стол", new Color(.09f, .065f, .045f));
        var bronze = Material("Бронзовый край", new Color(.38f, .24f, .09f));
        var inset = Material("Торец доски", new Color(.035f, .026f, .018f));
        var stand = Material("Подставки городов", new Color(.23f, .14f, .055f));
        var mapMaterial = SpriteMaterial("Карта", map, true);
        var cityMaterial = SpriteMaterial("Города", city, false);
        var discMaterial = SpriteMaterial("Клетки", disc, true);
        var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        RenderSettings.ambientMode = AmbientMode.Flat;
        RenderSettings.ambientLight = new Color(.65f, .60f, .50f);
        RenderSettings.fog = false;

        var cameraRoot = new GameObject("TabletopCamera", typeof(Camera), typeof(AudioListener));
        cameraRoot.tag = "MainCamera";
        var camera = cameraRoot.GetComponent<Camera>();
        camera.clearFlags = CameraClearFlags.SolidColor;
        camera.backgroundColor = new Color(.045f, .033f, .024f);
        camera.nearClipPlane = 1; camera.farClipPlane = 200;
        camera.GetUniversalAdditionalCameraData().renderPostProcessing = false;
        cameraRoot.AddComponent<TabletopCameraRig>().ResetView();
        var board = new GameObject("TabletopBoard").transform;
        Solid("Table", board, PrimitiveType.Cube, new Vector3(0, -.55f, 0), new Vector3(34, .6f, 23), wood);
        Solid("BoardEdge", board, PrimitiveType.Cube, new Vector3(0, -.09f, 0), new Vector3(24.4f, .28f, 14.06f), bronze);
        Solid("BoardBorder", board, PrimitiveType.Cube, new Vector3(0, .06f, 0), new Vector3(24.12f, .1f, 13.82f), inset);
        var surface = Sprite("BoardMap", board, map, mapMaterial, new Vector3(0, .125f, 0), 24);
        surface.sortingOrder = -100;
        surface.transform.rotation = Quaternion.Euler(90, 0, 0);

        var cityRoot = new GameObject(CityVisualsName).transform;
        for (int i = 0; i < Cities.Length; i++)
        {
            var position = Position(Cities[i]);
            var root = new GameObject($"City{i + 1:00}").transform; root.SetParent(cityRoot);
            root.position = position + Vector3.up * PieceHeight;
            root.localScale = Vector3.one * PieceScale;
            Solid("CityBase", root, PrimitiveType.Cylinder, new Vector3(0, .18f, 0), new Vector3(.75f, .025f, .75f), stand);
            var image = Sprite("CityStandee", root, city, cityMaterial, new Vector3(0, .208f, 0), 2.1f);
            image.gameObject.AddComponent<TabletopStandee>().Configure(camera);
        }
        var routeRoot = new GameObject(RouteVisualsName).transform;
        int cells = 0;
        for (int i = 0; i < Roads.Length; i++)
        {
            var road = new GameObject($"Road{Roads[i].x + 1:00}_{Roads[i].y + 1:00}").transform;
            road.SetParent(routeRoot);
            var start = Position(Cities[Roads[i].x]); var end = Position(Cities[Roads[i].y]);
            var direction = (end - start).normalized;
            var from = start + direction * .525f; var to = end - direction * .525f;
            int count = RouteStepCount(start, end);
            for (int j = 0; j < count; j++)
            {
                float t = (float)j / (count - 1);
                var point = Vector3.Lerp(from, to, t);
                var cell = new GameObject($"Cell{j + 1:00}").transform;
                cell.SetParent(road); cell.position = point + Vector3.up * PieceHeight;
                cell.localScale = Vector3.one * PieceScale;
                Solid("DiscEdge", cell, PrimitiveType.Cylinder, new Vector3(0, .165f, 0), new Vector3(.60f, .025f, .60f), bronze);
                var face = Sprite("CellFace", cell, disc, discMaterial, new Vector3(0, .22f, 0), .72f);
                face.sortingOrder = -50;
                face.transform.rotation = Quaternion.Euler(90, 0, 0);
                cells++;
            }
        }
        Controls();
        EditorSceneManager.SaveScene(scene, ScenePath);
        AssetDatabase.SaveAssets();
        Debug.Log($"TABLETOP_CAMERA_STUDY_CREATED: {Cities.Length} городов, {Roads.Length} дорог, {cells} фишек.");
        TabletopGameplaySetup.Run();
    }

    private static Vector3 Position(Vector2 uv) => new Vector3((uv.x - .5f) * 24, 0, (.5f - uv.y) * 24 * 946 / 1662);
    private static int RouteStepCount(Vector3 start, Vector3 end) =>
        Mathf.Clamp(Mathf.RoundToInt(Vector3.Distance(start, end) / .85f), 4, 8);
    private static Sprite ImportSprite(string filename, Vector2 pivot)
    {
        string path = ArtPath + filename;
        var importer = (TextureImporter)AssetImporter.GetAtPath(path);
        importer.textureType = TextureImporterType.Sprite; importer.spriteImportMode = SpriteImportMode.Single;
        importer.spritePixelsPerUnit = 100;
        var settings = new TextureImporterSettings(); importer.ReadTextureSettings(settings);
        settings.spriteAlignment = (int)SpriteAlignment.Custom; settings.spritePivot = pivot;
        importer.SetTextureSettings(settings);
        importer.textureShape = TextureImporterShape.Texture2D;
        importer.alphaIsTransparency = true; importer.mipmapEnabled = true;
        importer.wrapMode = TextureWrapMode.Clamp; importer.maxTextureSize = 2048;
        importer.textureCompression = TextureImporterCompression.Uncompressed;
        importer.SaveAndReimport();
        var sprite = AssetDatabase.LoadAssetAtPath<Sprite>(path);
        if (sprite == null) throw new InvalidOperationException("Не удалось импортировать спрайт: " + path);
        return sprite;
    }
    private static Material Material(string name, Color color)
    {
        string path = ArtPath + "Materials/" + name + ".mat";
        var material = AssetDatabase.LoadAssetAtPath<Material>(path);
        if (material == null) { material = new Material(Shader.Find("Caravan/CameraStudy/Solid")); AssetDatabase.CreateAsset(material, path); }
        material.SetColor("_BaseColor", color);
        EditorUtility.SetDirty(material); return material;
    }
    private static Material SpriteMaterial(string name, Sprite sprite, bool writeDepth)
    {
        string path = ArtPath + "Materials/" + name + ".mat";
        var material = AssetDatabase.LoadAssetAtPath<Material>(path);
        if (material == null)
        {
            material = new Material(Shader.Find("Universal Render Pipeline/2D/Sprite-Unlit-Default"));
            AssetDatabase.CreateAsset(material, path);
        }
        material.SetTexture("_MainTex", sprite.texture);
        material.SetFloat("_ZWrite", writeDepth ? 1 : 0);
        EditorUtility.SetDirty(material);
        return material;
    }
    private static void Solid(string name, Transform parent, PrimitiveType type, Vector3 position, Vector3 scale, Material material)
    {
        var root = GameObject.CreatePrimitive(type); root.name = name;
        root.transform.SetParent(parent, false); root.transform.localPosition = position; root.transform.localScale = scale;
        UnityEngine.Object.DestroyImmediate(root.GetComponent<Collider>());
        root.GetComponent<Renderer>().sharedMaterial = material;
    }
    private static SpriteRenderer Sprite(string name, Transform parent, Sprite sprite, Material material, Vector3 position, float width)
    {
        var root = new GameObject(name, typeof(SpriteRenderer)); root.transform.SetParent(parent, false);
        root.transform.localPosition = position; root.transform.localScale = Vector3.one * (width / sprite.bounds.size.x);
        var renderer = root.GetComponent<SpriteRenderer>(); renderer.sprite = sprite; renderer.sharedMaterial = material;
        return renderer;
    }
    private static void Controls()
    {
        var root = new GameObject(CameraHintsName, typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler));
        root.GetComponent<Canvas>().renderMode = RenderMode.ScreenSpaceOverlay;
        var scaler = root.GetComponent<CanvasScaler>(); scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080); scaler.matchWidthOrHeight = .5f;
        var font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>("Assets/Bricemont SDF.asset");
        var ui = new MenuUIBuilder(font, null);
        var title = ui.Label(root.transform, "НАСТОЛЬНЫЙ МАКЕТ · КАМЕРА", Vector2.zero, new Vector2(600, 45), 24, TextAlignmentOptions.Left, true);
        title.name = "Title";
        title.rectTransform.anchorMin = title.rectTransform.anchorMax = title.rectTransform.pivot = new Vector2(0, 1);
        title.rectTransform.anchoredPosition = new Vector2(28, -22);
        var hint = ui.Label(root.transform, "Колесо — масштаб   ·   ПКМ — поворот   ·   СКМ / WASD — сдвиг   ·   1 / 2 — ракурсы   ·   Home — сброс",
            Vector2.zero, new Vector2(1800, 45), 23);
        hint.name = "Controls";
        hint.rectTransform.anchorMin = hint.rectTransform.anchorMax = hint.rectTransform.pivot = new Vector2(.5f, 0);
        hint.rectTransform.anchoredPosition = new Vector2(0, 22);
    }

    // Рендер настоящей сцены для проверки композиции, без запуска Play Mode.
    public static void CreateAndPreviewBatch()
    {
        CreateScene();
        var camera = UnityEngine.Object.FindFirstObjectByType<Camera>(); camera.aspect = 16f / 9;
        var rig = camera.GetComponent<TabletopCameraRig>();
        rig.ResetView();
        Preview(camera, "Tabletop.png");
        rig.TopView(); Preview(camera, "TopView.png");
        rig.ResetView();
        Require(UnityEngine.Object.FindObjectsByType<TabletopStandee>(FindObjectsSortMode.None).Length == 18, "Должно быть восемнадцать фигурок города.");
        var cities = GameObject.Find(CityVisualsName).transform;
        Require(cities.childCount == Cities.Length, "Количество городов должно совпадать со схемой.");
        for (int i = 0; i < Cities.Length; i++)
        {
            var city = cities.GetChild(i);
            Require(Vector3.Distance(city.position, Position(Cities[i]) + Vector3.up * PieceHeight) < .001f,
                $"Город {i + 1:00} должен стоять по схеме.");
            Require(Vector3.Distance(city.localScale, Vector3.one * PieceScale) < .001f, "Размер города должен быть уменьшен вдвое.");
        }
        var routes = GameObject.Find(RouteVisualsName).transform;
        Require(routes.childCount == Roads.Length, "Количество переходов должно совпадать со схемой.");
        for (int i = 0; i < Roads.Length; i++)
        {
            var route = routes.GetChild(i);
            Require(route.childCount > 1, "Переход должен содержать игровые клетки.");
            foreach (Transform cell in route)
                Require(Vector3.Distance(cell.localScale, Vector3.one * PieceScale) < .001f, "Размер фишки должен быть уменьшен вдвое.");
        }
        TabletopGameplaySetup.Check();
        EditorSceneManager.SaveScene(camera.gameObject.scene, ScenePath);
        Debug.Log("TABLETOP_CAMERA_STUDY_CHECKS_PASSED");
    }
    private static void Preview(Camera camera, string filename)
    {
        foreach (var standee in UnityEngine.Object.FindObjectsByType<TabletopStandee>(FindObjectsSortMode.None))
            standee.Configure(camera);
        var target = new RenderTexture(1920, 1080, 24);
        var texture = new Texture2D(1920, 1080, TextureFormat.RGB24, false);
        var previous = RenderTexture.active;
        try
        {
            camera.targetTexture = target;
            RenderPipeline.SubmitRenderRequest(camera, new UniversalRenderPipeline.SingleCameraRequest { destination = target });
            RenderTexture.active = target; texture.ReadPixels(new Rect(0, 0, 1920, 1080), 0, 0); texture.Apply();
            Directory.CreateDirectory("Library/CameraStudyPreview");
            File.WriteAllBytes("Library/CameraStudyPreview/" + filename, texture.EncodeToPNG());
        }
        finally
        {
            RenderTexture.active = previous; camera.targetTexture = null;
            UnityEngine.Object.DestroyImmediate(texture); target.Release(); UnityEngine.Object.DestroyImmediate(target);
        }
    }
    private static void Require(bool condition, string message) { if (!condition) throw new InvalidOperationException(message); }
}
