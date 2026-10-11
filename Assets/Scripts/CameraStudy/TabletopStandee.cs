using UnityEngine;

/// <summary>Разворачивает изображение города к камере, как иллюстрированную фигурку на подставке.</summary>
[ExecuteAlways]
public sealed class TabletopStandee : MonoBehaviour
{
    [SerializeField] private Camera _camera;
    public void Configure(Camera camera) { _camera = camera; FaceCamera(); }
    private void LateUpdate() => FaceCamera();
    private void FaceCamera()
    {
        if (_camera != null) transform.rotation = _camera.transform.rotation;
    }
}
