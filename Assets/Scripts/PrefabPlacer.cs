using UnityEngine;
using UnityEditor;

public class PrefabPlacer : MonoBehaviour
{
    [Header("Настройки размещения")]
    public GameObject prefabToPlace;
    public int count = 10;

    [Header("Трансформ")]
    public Vector3 localScale = Vector3.one;
    public Vector3 rotation = Vector3.zero;

    [Header("Объекты-точки")]
    public Transform startPointObj;
    public Transform endPointObj;

    [Header("Объект-поверхность (опционально)")]
    public Transform placementSurface;

    [ContextMenu("Разместить префабы между объектами")]
    public void PlacePrefabs()
    {
        if (prefabToPlace == null)
        {
            Debug.LogError("Префаб не назначен!");
            return;
        }

        if (startPointObj == null || endPointObj == null)
        {
            Debug.LogError("Необходимо указать обе точки (start и end)!");
            return;
        }

        ClearChildren();

        Vector3 startPos = startPointObj.position;
        Vector3 endPos = endPointObj.position;

        for (int i = 0; i < count; i++)
        {
            float t = count <= 1 ? 0 : (float)i / (count - 1);

            Vector3 pos = Vector3.Lerp(startPos, endPos, t);

            if (placementSurface != null)
            {
                pos.y = placementSurface.position.y;
            }

            var instance = PrefabUtility.InstantiatePrefab(prefabToPlace) as GameObject;
            if (instance != null)
            {
                instance.transform.position = pos;
                instance.transform.localRotation = Quaternion.Euler(rotation);
                instance.transform.localScale = localScale;

                Undo.RegisterCreatedObjectUndo(instance, "Instantiate Prefab");
            }
        }
    }

    private void ClearChildren()
    {
        for (int i = transform.childCount - 1; i >= 0; i--)
        {
            Object child = transform.GetChild(i).gameObject;
            if (child != null)
                Undo.DestroyObjectImmediate(child);
        }
    }
}