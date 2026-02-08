using UnityEngine;

public class FollowTarget : MonoBehaviour
{
    public Transform target;          // Фишка игрока
    public float smoothSpeed = 5f;    // Скорость плавности
    public Vector3 offset;            // Смещение камеры (изометрическое)

    private void LateUpdate()
    {
        if (target == null) return;

        // Желаемая позиция камеры
        Vector3 desiredPosition = target.position + offset;

        // Плавное движение
        transform.position = Vector3.Lerp(
            transform.position,
            desiredPosition,
            smoothSpeed * Time.deltaTime
        );
    }
}