using UnityEngine;
using UnityEngine.UI;

public class PlayerButtonController : MonoBehaviour
{
    [SerializeField] private PlayerPanelUI playerPanel;
    private Button button;

    private void Awake()
    {
        button = GetComponent<Button>();
        button.onClick.AddListener(TogglePanel);
    }

    private void TogglePanel()
    {
        if (playerPanel.gameObject.activeSelf)
        {
            playerPanel.ClosePanel();
        }
        else
        {
            playerPanel.OpenPanel();
        }
    }
}