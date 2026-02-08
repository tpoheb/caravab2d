using UnityEngine;
using UnityEngine.UI;

public class BattleWindow : MonoBehaviour
{
    [SerializeField] private Button closeButton;
    private System.Action onComplete;



    public void Initialize(System.Action completeCallback)
    {
        onComplete = completeCallback;
        closeButton.onClick.AddListener(CloseWindow);
        gameObject.SetActive(true); // Аналог OpenWindow
    }

    private void CloseWindow()
    {
        onComplete?.Invoke();
        Destroy(gameObject);
    }
}