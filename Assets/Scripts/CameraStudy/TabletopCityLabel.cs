using UnityEngine;

/// <summary>Держит подпись над изображением города при любом ракурсе камеры.</summary>
[ExecuteAlways]
[DefaultExecutionOrder(100)]
public sealed class TabletopCityLabel : MonoBehaviour
{
    [SerializeField] private SpriteRenderer _standee;
    [SerializeField] private float _offset = .25f;

    public SpriteRenderer Standee => _standee;

    public void Configure(SpriteRenderer standee)
    {
        _standee = standee;
        ApplyPose();
    }

    private void OnEnable() => ApplyPose();
    private void LateUpdate() => ApplyPose();

    public void ApplyPose()
    {
        if (_standee == null || _standee.sprite == null) return;
        var image = _standee.transform;
        var top = image.TransformPoint(Vector3.up * _standee.sprite.bounds.max.y);
        transform.SetPositionAndRotation(top + image.up * _offset, image.rotation);
    }
}
