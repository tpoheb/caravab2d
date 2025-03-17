using UnityEngine;

public class PlayerToken : MonoBehaviour
{
    public City currentCity;

    private void Start()
    {
        currentCity.OpenPanel();
    }
}