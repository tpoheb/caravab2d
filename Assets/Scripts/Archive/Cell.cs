using UnityEngine;

public class Cell : MonoBehaviour
{
    public int cellNumber;
    public Vector3 Position => transform.position;
    public CellType Type = CellType.Normal;
    public bool IsActive => gameObject.activeSelf;
}

public enum CellType
{
    Normal,
    Battle,
    Event
}