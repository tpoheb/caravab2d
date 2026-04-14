using UnityEngine;

[RequireComponent(typeof(MeshFilter))]
public class HexUVFixer : MonoBehaviour
{
    void Start()
    {
        Mesh mesh = GetComponent<MeshFilter>().mesh;
        Vector3[] vertices = mesh.vertices;
        Vector2[] uvs = new Vector2[vertices.Length];

        // Находим bounds меша чтобы нормализовать UV
        Bounds bounds = mesh.bounds;

        for (int i = 0; i < vertices.Length; i++)
        {
            // Переводим XZ координаты вершин в UV (0..1)
            uvs[i] = new Vector2(
                (vertices[i].x - bounds.min.x) / bounds.size.x,
                (vertices[i].z - bounds.min.z) / bounds.size.z
            );
        }

        mesh.uv = uvs;
    }
}