using UnityEngine;

public class IsoCameraFollow : MonoBehaviour
{
    [Header("Target")]
    public Transform target; // перетащи сюда фишку игрока

    [Header("Follow Settings")]
    public float smoothSpeed = 5f;        // плавность следования
    public Vector3 offset = new Vector3(0f, 15f, -10f); // отступ от фишки

    [Header("Tabletop Angle")]
    public float pitch = 55f;  // наклон вниз
    public float yaw = 20f;    // поворот для объёма

    void Start()
    {
        transform.rotation = Quaternion.Euler(pitch, yaw, 0f);
    }

    void LateUpdate()
    {
        if (target == null) return;

        Vector3 desiredPosition = target.position + offset;

        // плавное смещение
        transform.position = Vector3.Lerp(
            transform.position,
            desiredPosition,
            smoothSpeed * Time.deltaTime
        );
    }
}
