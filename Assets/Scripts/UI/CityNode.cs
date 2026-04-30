using UnityEngine;
using TMPro;
#if UNITY_EDITOR
using UnityEditor;
#endif

[ExecuteAlways]
public class CityNode : MonoBehaviour
{
    [Header("Данные города")]
    public string cityName = "Новый город";

    [Header("Ссылки")]
    [SerializeField] private TextMeshPro label;

    void OnValidate()
    {
        UpdateLabel();
    }

    void Awake()
    {
        UpdateLabel();
    }

    void UpdateLabel()
    {
        if (label != null)
            label.text = cityName;
    }

#if UNITY_EDITOR
    void OnDrawGizmos()
    {
        // Дублируем в Scene View через Handles для надёжности
        GUIStyle style = new GUIStyle();
        style.normal.textColor = Color.yellow;
        style.fontSize = 12;
        style.fontStyle = FontStyle.Bold;

        Handles.Label(
            transform.position + Vector3.up * 1f,
            cityName,
            style
        );
    }
#endif
}