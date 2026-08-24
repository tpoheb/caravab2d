using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace Editor
{
    [CustomEditor(typeof(CityData), true)]
    public class CityDataEditor : UnityEditor.Editor
    {
        private bool _foldoutRumors = true;

        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            SerializedProperty cityName     = serializedObject.FindProperty("cityName");
            SerializedProperty cityGold     = serializedObject.FindProperty("cityGold");
            SerializedProperty items        = serializedObject.FindProperty("items");
            SerializedProperty info         = serializedObject.FindProperty("info");
            SerializedProperty rumors       = serializedObject.FindProperty("rumors");
            SerializedProperty isCapital    = serializedObject.FindProperty("isCapital");
            SerializedProperty availableUnits = serializedObject.FindProperty("availableUnits");

            EditorGUILayout.PropertyField(cityName);
            EditorGUILayout.PropertyField(cityGold);

            EditorGUILayout.Space();
            EditorGUILayout.PropertyField(items, true);

            EditorGUILayout.Space();

            // ─── Информация о городе ───────────────────────────────────────
            EditorGUILayout.LabelField("Информация о городе", EditorStyles.boldLabel);
            float inspectorWidth = EditorGUIUtility.currentViewWidth - 20f;
            EditorGUILayout.PropertyField(
                info, GUIContent.none,
                GUILayout.MaxWidth(inspectorWidth),
                GUILayout.MinHeight(60));
            EditorGUILayout.Space();

            // ─── Слухи ──────────────────────────────────────────────────────
            _foldoutRumors = EditorGUILayout.Foldout(_foldoutRumors, "Слухи в городе", true);
            if (_foldoutRumors)
            {
                EditorGUI.indentLevel++;

                int removeAt = -1;
                for (int i = 0; i < rumors.arraySize; i++)
                {
                    EditorGUILayout.BeginHorizontal();
                    SerializedProperty rumor = rumors.GetArrayElementAtIndex(i);
                    EditorGUILayout.PropertyField(
                        rumor, GUIContent.none,
                        GUILayout.MaxWidth(inspectorWidth - 30f),
                        GUILayout.MinHeight(40));
                    if (GUILayout.Button("✕", GUILayout.Width(22)))
                        removeAt = i;
                    EditorGUILayout.EndHorizontal();
                }

                if (removeAt >= 0)
                    rumors.DeleteArrayElementAtIndex(removeAt);

                if (GUILayout.Button("+ Добавить слух"))
                    rumors.arraySize++;

                EditorGUI.indentLevel--;
            }

            // ─── Столица ────────────────────────────────────────────────────
            EditorGUILayout.Space();
            EditorGUILayout.PropertyField(isCapital);

            // ─── Найм юнитов ────────────────────────────────────────────────
            EditorGUILayout.Space();
            EditorGUILayout.PropertyField(availableUnits, true);

            serializedObject.ApplyModifiedProperties();
        }
    }

    public class CityInfoEditorWindow : EditorWindow
    {
        private const float SidebarWidth   = 200f;
        private const float ContentMaxWidth = 700f;
        private const float InfoHeight      = 140f;
        private const float RumorHeight     = 60f;

        private List<CityData> _cities = new List<CityData>();
        private Vector2 _cityScroll;
        private Vector2 _editorScroll;
        private CityData _selected;

        [MenuItem("Tools/1000 Roads/City Info & Rumors Editor")]
        public static void ShowWindow()
        {
            var window = GetWindow<CityInfoEditorWindow>("Города: инфо и слухи");
            window.minSize = new Vector2(600, 400);
            window.LoadCities();
            window.Show();
        }

        private void LoadCities()
        {
            _cities.Clear();
            string[] guids = AssetDatabase.FindAssets("t:CityData");
            foreach (string guid in guids)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                var city = AssetDatabase.LoadAssetAtPath<CityData>(path);
                if (city != null)
                    _cities.Add(city);
            }
            _cities.Sort((a, b) => string.CompareOrdinal(a.cityName, b.cityName));
        }

        private void OnGUI()
        {
            // Ширина контентной панели — не больше ContentMaxWidth и не больше того,
            // что влезает в окно рядом с сайдбаром
            float availableWidth = Mathf.Min(
                ContentMaxWidth,
                position.width - SidebarWidth - 16f);

            EditorGUILayout.BeginHorizontal();

            // ─── Список городов ────────────────────────────────────────────
            EditorGUILayout.BeginVertical(GUILayout.Width(SidebarWidth));
            if (GUILayout.Button("↻ Обновить список"))
                LoadCities();

            _cityScroll = EditorGUILayout.BeginScrollView(_cityScroll);
            foreach (var city in _cities)
            {
                bool isSelected = _selected == city;
                GUI.backgroundColor = isSelected ? Color.cyan : Color.white;
                if (GUILayout.Button(city.cityName ?? "(unnamed)"))
                {
                    _selected = city;
                    GUI.FocusControl(null);
                }
                GUI.backgroundColor = Color.white;
            }
            EditorGUILayout.EndScrollView();
            EditorGUILayout.EndVertical();

            // ─── Редактор выбранного города ────────────────────────────────
            EditorGUILayout.BeginVertical(GUILayout.Width(availableWidth));
            _editorScroll = EditorGUILayout.BeginScrollView(_editorScroll);

            if (_selected == null)
            {
                EditorGUILayout.HelpBox(
                    "Выберите город слева, чтобы отредактировать его информацию и слухи.",
                    MessageType.Info);
            }
            else
            {
                EditorGUI.BeginChangeCheck();

                // Название
                EditorGUILayout.LabelField("Название", EditorStyles.boldLabel);
                _selected.cityName = EditorGUILayout.TextField(
                    _selected.cityName, GUILayout.MaxWidth(availableWidth));

                // Столица
                EditorGUILayout.Space();
                _selected.isCapital = EditorGUILayout.Toggle("Столица", _selected.isCapital);

                // Информация
                EditorGUILayout.Space();
                EditorGUILayout.LabelField("Информация о городе", EditorStyles.boldLabel);
                _selected.info = EditorGUILayout.TextArea(
                    _selected.info,
                    GUILayout.MaxWidth(availableWidth),
                    GUILayout.Height(InfoHeight));

                // Слухи
                EditorGUILayout.Space();
                EditorGUILayout.LabelField("Слухи в городе", EditorStyles.boldLabel);

                if (_selected.rumors == null)
                    _selected.rumors = new List<string>();

                int removeAt = -1;
                for (int i = 0; i < _selected.rumors.Count; i++)
                {
                    EditorGUILayout.BeginHorizontal();
                    _selected.rumors[i] = EditorGUILayout.TextArea(
                        _selected.rumors[i],
                        GUILayout.MaxWidth(availableWidth - 30f),
                        GUILayout.Height(RumorHeight));
                    if (GUILayout.Button("✕", GUILayout.Width(22), GUILayout.Height(RumorHeight)))
                        removeAt = i;
                    EditorGUILayout.EndHorizontal();
                }

                if (removeAt >= 0)
                    _selected.rumors.RemoveAt(removeAt);

                if (GUILayout.Button("+ Добавить слух", GUILayout.MaxWidth(availableWidth)))
                    _selected.rumors.Add(string.Empty);

                // Юниты
                EditorGUILayout.Space();
                EditorGUILayout.LabelField("Юниты для найма", EditorStyles.boldLabel);

                if (_selected.availableUnits == null)
                    _selected.availableUnits = new List<UnitData>();

                int removeUnitAt = -1;
                for (int i = 0; i < _selected.availableUnits.Count; i++)
                {
                    EditorGUILayout.BeginHorizontal();
                    _selected.availableUnits[i] = (UnitData)EditorGUILayout.ObjectField(
                        _selected.availableUnits[i], typeof(UnitData), false,
                        GUILayout.MaxWidth(availableWidth - 30f));
                    if (GUILayout.Button("✕", GUILayout.Width(22)))
                        removeUnitAt = i;
                    EditorGUILayout.EndHorizontal();
                }

                if (removeUnitAt >= 0)
                    _selected.availableUnits.RemoveAt(removeUnitAt);

                if (GUILayout.Button("+ Добавить юнита", GUILayout.MaxWidth(availableWidth)))
                    _selected.availableUnits.Add(null);

                if (EditorGUI.EndChangeCheck())
                    EditorUtility.SetDirty(_selected);

                EditorGUILayout.Space();
                if (GUILayout.Button("Сохранить", GUILayout.Height(30), GUILayout.MaxWidth(availableWidth)))
                {
                    AssetDatabase.SaveAssets();
                    EditorUtility.DisplayDialog("Готово", "Изменения сохранены.", "OK");
                }
            }

            EditorGUILayout.EndScrollView();
            EditorGUILayout.EndVertical();

            EditorGUILayout.EndHorizontal();
        }
    }
}