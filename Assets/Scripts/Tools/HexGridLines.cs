using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.Rendering;

[RequireComponent(typeof(MeshFilter), typeof(MeshRenderer))]
public class HexGridLines : MonoBehaviour
{
    [Header("Настройки")]
    public float hexSize = 1f;
    public float lineWidth = 0.04f;
    public float heightOffset = 0.02f;
    public Color lineColor = new Color(1f, 1f, 1f, 0.35f);
    public Transform hexParent;

    private Mesh mesh;
    private MeshRenderer mr;

    void Start()
    {
        mr = GetComponent<MeshRenderer>();
        StartCoroutine(BuildMeshAsync());
    }

    IEnumerator BuildMeshAsync()
    {
        // Собираем позиции
        var hexPositions = new List<Vector3>(hexParent.childCount);
        foreach (Transform hex in hexParent)
            hexPositions.Add(hex.position);

        yield return null;

        // Собираем рёбра
        var edges = CollectEdges(hexPositions);

        yield return null;

        // Строим меш
        BuildLineMesh(edges);

        // Материал
        if (mr.sharedMaterial == null)
        {
            var mat = new Material(Shader.Find("Unlit/Color"));
            mat.color = lineColor;
            mr.sharedMaterial = mat;
        }
    }

    List<(Vector3 a, Vector3 b)> CollectEdges(List<Vector3> centers)
    {
        var edgeSet = new HashSet<EdgeKey>();
        var edges = new List<(Vector3, Vector3)>(centers.Count * 3);

        foreach (var center in centers)
        {
            Vector3[] corners = GetCorners(center);
            for (int i = 0; i < 6; i++)
            {
                Vector3 a = corners[i];
                Vector3 b = corners[(i + 1) % 6];
                if (edgeSet.Add(new EdgeKey(a, b)))
                    edges.Add((a, b));
            }
        }
        return edges;
    }

    void BuildLineMesh(List<(Vector3 a, Vector3 b)> edges)
    {
        var vertices = new List<Vector3>(edges.Count * 4);
        var triangles = new List<int>(edges.Count * 6);

        foreach (var (a, b) in edges)
        {
            Vector3 dir = (b - a).normalized;
            Vector3 perp = new Vector3(-dir.z, 0f, dir.x) * (lineWidth * 0.5f);
            Vector3 lift = Vector3.up * heightOffset;

            int idx = vertices.Count;
            vertices.Add(a - perp + lift);
            vertices.Add(a + perp + lift);
            vertices.Add(b + perp + lift);
            vertices.Add(b - perp + lift);

            triangles.Add(idx);     triangles.Add(idx + 1); triangles.Add(idx + 2);
            triangles.Add(idx);     triangles.Add(idx + 2); triangles.Add(idx + 3);
        }

        mesh = new Mesh();
        mesh.indexFormat = IndexFormat.UInt32;
        mesh.name = "HexGridLines";
        mesh.SetVertices(vertices);
        mesh.SetTriangles(triangles, 0);
        mesh.RecalculateNormals();
        mesh.RecalculateBounds();

        GetComponent<MeshFilter>().mesh = mesh;
    }

    Vector3[] GetCorners(Vector3 center)
    {
        var corners = new Vector3[6];
        for (int i = 0; i < 6; i++)
        {
            float angle = Mathf.Deg2Rad * (60f * i);
            corners[i] = new Vector3(
                center.x + hexSize * Mathf.Cos(angle),
                center.y,
                center.z + hexSize * Mathf.Sin(angle)
            );
        }
        return corners;
    }

    public void SetVisible(bool visible) => mr.enabled = visible;

    // Быстрый числовой ключ для дедупликации рёбер
    private struct EdgeKey : System.IEquatable<EdgeKey>
    {
        int ax, az, bx, bz;

        public EdgeKey(Vector3 a, Vector3 b)
        {
            int ax_ = Mathf.RoundToInt(a.x * 100f);
            int az_ = Mathf.RoundToInt(a.z * 100f);
            int bx_ = Mathf.RoundToInt(b.x * 100f);
            int bz_ = Mathf.RoundToInt(b.z * 100f);

            if (ax_ < bx_ || (ax_ == bx_ && az_ < bz_))
            {
                ax = ax_; az = az_; bx = bx_; bz = bz_;
            }
            else
            {
                ax = bx_; az = bz_; bx = ax_; bz = az_;
            }
        }

        public bool Equals(EdgeKey other) =>
            ax == other.ax && az == other.az &&
            bx == other.bx && bz == other.bz;

        public override int GetHashCode()
        {
            unchecked
            {
                int hash = 17;
                hash = hash * 31 + ax;
                hash = hash * 31 + az;
                hash = hash * 31 + bx;
                hash = hash * 31 + bz;
                return hash;
            }
        }
    }

#if UNITY_EDITOR
    [ContextMenu("Rebuild")]
    void Rebuild() => StartCoroutine(BuildMeshAsync());
#endif
}