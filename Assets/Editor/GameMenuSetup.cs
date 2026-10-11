using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>Обновление каталога ассетов и воспроизводимая сборка стартовой сцены.</summary>
public sealed class GameMenuSetup : IPreprocessBuildWithReport
{
    public int callbackOrder => 0;
    public const string CatalogPath = "Assets/Data/SaveGameCatalog.asset";
    public const string MainMenuPath = "Assets/Scenes/MainMenu.unity";
    public const string GamePath = "Assets/Scenes/" + ApplicationFlow.GameSceneName + ".unity";
    public void OnPreprocessBuild(BuildReport report) => RefreshCatalog();

    [MenuItem("Tools/Меню/Обновить каталог сохранений")]
    public static SaveGameCatalog RefreshCatalog()
    {
        var catalog = AssetDatabase.LoadAssetAtPath<SaveGameCatalog>(CatalogPath);
        if (catalog == null) { catalog = ScriptableObject.CreateInstance<SaveGameCatalog>(); AssetDatabase.CreateAsset(catalog, CatalogPath); }
        var entries = new List<SaveGameCatalog.Entry>();
        foreach (string guid in AssetDatabase.FindAssets("t:ScriptableObject", new[] { "Assets" }).OrderBy(value => value))
        {
            var asset = AssetDatabase.LoadAssetAtPath<ScriptableObject>(AssetDatabase.GUIDToAssetPath(guid));
            if (asset is Item || asset is UnitData || asset is GuildData || asset is ShadowCardData ||
                asset is BattleCardData || asset is HandCardData || asset is TraderProfile || asset is PlayerStats)
                entries.Add(new SaveGameCatalog.Entry { id = guid, asset = asset });
        }
        catalog.Configure(entries.ToArray()); EditorUtility.SetDirty(catalog); AssetDatabase.SaveAssets();
        AssetDatabase.ImportAsset(CatalogPath, ImportAssetOptions.ForceSynchronousImport);
        return AssetDatabase.LoadAssetAtPath<SaveGameCatalog>(CatalogPath);
    }

    [MenuItem("Tools/Меню/Создать стартовую сцену")]
    public static void Run()
    {
        var catalog = RefreshCatalog();
        var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        catalog = AssetDatabase.LoadAssetAtPath<SaveGameCatalog>(CatalogPath);
        var cameraRoot = new GameObject("Камера главного меню", typeof(Camera), typeof(AudioListener));
        var camera = cameraRoot.GetComponent<Camera>();
        camera.clearFlags = CameraClearFlags.SolidColor; camera.backgroundColor = Color.black;
        camera.cullingMask = 0; camera.orthographic = true; cameraRoot.tag = "MainCamera";
        var root = new GameObject("1000 Дорог — Меню и загрузка");
        var flow = root.AddComponent<ApplicationFlow>();
        var properties = new SerializedObject(flow);
        properties.FindProperty("_background").objectReferenceValue = AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/Art/Loading/CaravanLoadingBackground.png");
        properties.FindProperty("_font").objectReferenceValue = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>("Assets/Bricemont SDF.asset");
        properties.FindProperty("_buttonSprite").objectReferenceValue = AssetDatabase.LoadAssetAtPath<Sprite>(AssetDatabase.GUIDToAssetPath("16bfa79c69596934392cd371ffc2ace5"));
        properties.FindProperty("_catalog").objectReferenceValue = catalog;
        properties.ApplyModifiedPropertiesWithoutUndo();
        EditorSceneManager.SaveScene(scene, MainMenuPath);
        var scenes = new List<EditorBuildSettingsScene> {
            new EditorBuildSettingsScene(MainMenuPath, true), new EditorBuildSettingsScene(GamePath, true) };
        scenes.AddRange(EditorBuildSettings.scenes.Where(value => value.path != MainMenuPath && value.path != GamePath)
            .Select(value => new EditorBuildSettingsScene(value.path, value.path == "Assets/Scenes/SampleScene.unity" ? false : value.enabled)));
        EditorBuildSettings.scenes = scenes.ToArray();
        Debug.Log("GAME_MENU_SETUP_PASSED");
    }
}
