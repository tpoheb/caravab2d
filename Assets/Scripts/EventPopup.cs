using TMPro;
using UnityEngine;

public class EventPopup : MonoBehaviour
{
    [SerializeField] private GameObject popup;
    [SerializeField] private TextMeshProUGUI eventText;

    public void ShowEvent(string message)
    {
        popup.SetActive(true);
        eventText.text = message;
    }

    public void HideEvent()
    {
        popup.SetActive(false);
    }
}