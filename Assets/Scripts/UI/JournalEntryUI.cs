using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// UI-отображение одной записи журнала квестов.
///
/// Структура префаба:
///   Icon (Image)        — необязательно
///   MessageText (TMP)
///   TurnText (TMP)
/// </summary>
public class JournalEntryUI : MonoBehaviour
{
    [Header("Элементы записи")]
    [SerializeField] private Image iconImage;
    [SerializeField] private TextMeshProUGUI messageText;
    [SerializeField] private TextMeshProUGUI turnText;

    [Header("Иконка")]
    [SerializeField] private Sprite questIcon;

    public void Setup(JournalEntryData data)
    {
        if (messageText != null)
            messageText.text = data.message;

        if (turnText != null)
            turnText.text = $"Ход {data.turnNumber}";

        if (iconImage != null)
            iconImage.sprite = questIcon;
    }

    private void OnValidate()
    {
        if (messageText == null)
            Debug.LogWarning("[JournalEntryUI] MessageText не назначен", this);
        if (turnText == null)
            Debug.LogWarning("[JournalEntryUI] TurnText не назначен", this);
    }
}