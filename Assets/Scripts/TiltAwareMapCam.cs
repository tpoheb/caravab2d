using UnityEngine;

[RequireComponent(typeof(Camera))]
public class TiltAwareMapCam : MonoBehaviour
{
    [Header("Настройки")]
    public float moveSpeed   = 20f;   // юнитов в секунду
    public float zoomSpeed   = 10f;   // юнитов за щелчок
    public float zoomMin     = 5f;
    public float zoomMax     = 50f;

    private Camera cam;

    void Awake()
    {
        cam = GetComponent<Camera>();
        cam.orthographic = true;        // на всякий случай
    }

    void Update()
    {
        HandleMovement();
        HandleZoom();
    }

    // Движение относительно собственных осей камеры
    void HandleMovement()
    {
        Vector2 input = new Vector2(Input.GetAxis("Horizontal"),
            Input.GetAxis("Vertical")); // Raw можно

        Vector3 right   = transform.right;      // локальный вектор "вправо"
        Vector3 forward = transform.forward;    // локальный вектор "вперёд"

        // проецируем на плоскость XZ мира, чтобы не влиять Y
        right.y   = 0f;
        forward.y = 0f;
        right.Normalize();
        forward.Normalize();

        Vector3 dir = (right * input.x + forward * input.y).normalized;
        transform.position += dir * moveSpeed * Time.deltaTime;
    }

    // Зум без сдвига центра
    void HandleZoom()
    {
        float scroll = -Input.GetAxis("Mouse ScrollWheel");
        if (Mathf.Abs(scroll) > 0f)
        {
            cam.orthographicSize = Mathf.Clamp(cam.orthographicSize + scroll * zoomSpeed,
                zoomMin, zoomMax);
        }
    }
}