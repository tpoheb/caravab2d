using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class EventPopup : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI messageText;
    [SerializeField] private Button closeButton;

    private System.Action onComplete;

    public void Initialize(string message, System.Action completeCallback)
    {
        messageText.text = message;
        this.onComplete = completeCallback;
        closeButton.onClick.AddListener(OnClose);
        gameObject.SetActive(true);
    }

    private void OnClose()
    {
        onComplete?.Invoke();
        Destroy(gameObject);
    }
}