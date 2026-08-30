using UnityEngine;

/// <summary>
/// Вешается на UI-метку города на карте.
/// Связывает визуальную метку с объектом City в сцене.
/// </summary>
public class CityMapMarker : MonoBehaviour
{
    [SerializeField] private City city;
    public City City => city;
}