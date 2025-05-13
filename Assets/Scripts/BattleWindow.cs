using TMPro;
using UnityEngine;

public class BattleWindow : MonoBehaviour
{
    [SerializeField] private GameObject window;
    [SerializeField] private TextMeshProUGUI battleText;

    public void OpenWindow()
    {
        window.SetActive(true);
        battleText.text = "Началась битва!";
    }

    public void CloseWindow()
    {
        window.SetActive(false);
    }
}