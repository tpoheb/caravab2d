using UnityEngine;

public class SimpleMapCam : MonoBehaviour
{
    [Header("Movement Settings")]
    public float moveSpeed = 10f;
    public float rotationSpeed = 100f;
    public float edgeScrollThreshold = 0.05f; // Для движения при подходе к краю экрана

    [Header("Zoom Settings")]
    public float zoomSpeed = 10f;
    public float minZoom = 5f;
    public float maxZoom = 50f;
    public float zoomDampening = 5f;

    private Vector3 targetPosition;
    private float targetZoom;
    private Camera cam;

    void Start()
    {
        cam = GetComponent<Camera>();
        targetPosition = transform.position;
        targetZoom = cam.orthographicSize;
    }

    void Update()
    {
        HandleKeyboardMovement();
        HandleEdgeScrolling();
        HandleMouseZoom();
        
        // Плавное перемещение и зум
        transform.position = Vector3.Lerp(transform.position, targetPosition, Time.deltaTime * moveSpeed);
        cam.orthographicSize = Mathf.Lerp(cam.orthographicSize, targetZoom, Time.deltaTime * zoomDampening);
    }

    void HandleKeyboardMovement()
    {
        Vector3 moveDirection = Vector3.zero;
        
        // Стандартное управление WASD
        if (Input.GetKey(KeyCode.W) || Input.GetKey(KeyCode.UpArrow))
            moveDirection += transform.forward;
        if (Input.GetKey(KeyCode.S) || Input.GetKey(KeyCode.DownArrow))
            moveDirection -= transform.forward;
        if (Input.GetKey(KeyCode.A) || Input.GetKey(KeyCode.LeftArrow))
            moveDirection -= transform.right;
        if (Input.GetKey(KeyCode.D) || Input.GetKey(KeyCode.RightArrow))
            moveDirection += transform.right;

        // Нормализуем вектор, если есть ввод с клавиатуры
        if (moveDirection != Vector3.zero)
        {
            moveDirection.Normalize();
            targetPosition += moveDirection * moveSpeed * Time.deltaTime;
        }
    }

    void HandleEdgeScrolling()
    {
        Vector3 moveDirection = Vector3.zero;
        Vector2 mousePosition = Input.mousePosition;
        
        // Движение при подходе к краю экрана
        if (mousePosition.x < Screen.width * edgeScrollThreshold)
            moveDirection -= transform.right;
        if (mousePosition.x > Screen.width * (1 - edgeScrollThreshold))
            moveDirection += transform.right;
        if (mousePosition.y < Screen.height * edgeScrollThreshold)
            moveDirection -= transform.forward;
        if (mousePosition.y > Screen.height * (1 - edgeScrollThreshold))
            moveDirection += transform.forward;

        if (moveDirection != Vector3.zero)
        {
            moveDirection.Normalize();
            targetPosition += moveDirection * moveSpeed * Time.deltaTime;
        }
    }
    
    void HandleMouseZoom()
    {
        float scroll = Input.GetAxis("Mouse ScrollWheel");
        if (scroll != 0f)
        {
            targetZoom -= scroll * zoomSpeed;
            targetZoom = Mathf.Clamp(targetZoom, minZoom, maxZoom);
        }
    }
}