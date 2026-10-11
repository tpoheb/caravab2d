using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>Камера макета настольной карты. Не связана с игровыми системами.</summary>
[ExecuteAlways]
[RequireComponent(typeof(Camera))]
public sealed class TabletopCameraRig : MonoBehaviour
{
    [Header("Ракурс стола")]
    [SerializeField] private Vector3 _target = new Vector3(0, .25f, 0);
    [SerializeField, Range(30, 89)] private float _pitch = 52;
    [SerializeField] private float _yaw;
    [SerializeField, Range(8, 42)] private float _distance = 28;
    [SerializeField, Range(25, 60)] private float _fieldOfView = 36;
    [SerializeField] private bool _orthographic;

    [Header("Управление обзором")]
    [SerializeField] private float _rotationSpeed = .18f;
    [SerializeField] private float _panSpeed = .018f;
    [SerializeField] private float _keyboardSpeed = 7;
    private Camera _camera;

    private void OnEnable() { _camera = GetComponent<Camera>(); ApplyView(); }
    private void LateUpdate()
    {
        if (Application.isPlaying) ReadInput();
        ApplyView();
    }
    public void ResetView()
    {
        _target = new Vector3(0, .25f, 0); _pitch = 52; _yaw = 0;
        _distance = 28; _orthographic = false; ApplyView();
    }
    public void TopView()
    {
        ResetView(); _pitch = 89; _orthographic = true; ApplyView();
    }
    private void ReadInput()
    {
        var keyboard = Keyboard.current;
        if (keyboard != null)
        {
            if (keyboard.homeKey.wasPressedThisFrame || keyboard.digit1Key.wasPressedThisFrame) ResetView();
            if (keyboard.digit2Key.wasPressedThisFrame) TopView();
            float x = (keyboard.dKey.isPressed || keyboard.rightArrowKey.isPressed ? 1 : 0) -
                (keyboard.aKey.isPressed || keyboard.leftArrowKey.isPressed ? 1 : 0);
            float z = (keyboard.wKey.isPressed || keyboard.upArrowKey.isPressed ? 1 : 0) -
                (keyboard.sKey.isPressed || keyboard.downArrowKey.isPressed ? 1 : 0);
            Pan(new Vector2(x, z) * (_keyboardSpeed * Time.unscaledDeltaTime));
        }
        var mouse = Mouse.current;
        if (mouse == null) return;
        var delta = mouse.delta.ReadValue();
        if (mouse.rightButton.isPressed)
        {
            _yaw += delta.x * _rotationSpeed;
            _pitch -= delta.y * _rotationSpeed;
        }
        if (mouse.middleButton.isPressed) Pan(-delta * (_panSpeed * _distance / 28));
        // Экспоненциальный масштаб одинаково работает с мышью и плавным скроллом.
        _distance *= Mathf.Exp(-mouse.scroll.ReadValue().y * .0012f);
    }
    private void Pan(Vector2 delta)
    {
        var orientation = Quaternion.Euler(0, _yaw, 0);
        _target += orientation * new Vector3(delta.x, 0, delta.y);
        _target.x = Mathf.Clamp(_target.x, -12, 12);
        _target.z = Mathf.Clamp(_target.z, -7, 7);
    }
    public void ApplyView()
    {
        if (_camera == null) _camera = GetComponent<Camera>();
        _pitch = Mathf.Clamp(_pitch, 30, 89); _distance = Mathf.Clamp(_distance, 8, 42);
        var rotation = Quaternion.Euler(_pitch, _yaw, 0);
        float aspectScale = Mathf.Max(1, (16f / 9) / Mathf.Max(.1f, _camera.aspect));
        transform.SetPositionAndRotation(_target - rotation * Vector3.forward * (_distance * aspectScale), rotation);
        _camera.fieldOfView = _fieldOfView;
        _camera.orthographic = _orthographic;
        _camera.orthographicSize = _distance * .32f * aspectScale;
    }
}
