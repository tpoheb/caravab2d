using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// UI-отображение одной записи журнала. Используется на префабе записи
/// для обеих вкладок (торговля и квесты).
///
/// Структура префаба:
///   Icon (Image)
///   MessageText (TMP_Text)
///   TurnText (TMP_Text)
/// </summary>
public class JournalEntryUI : MonoBehaviour
{
    [Header("Элементы записи")]
    [SerializeField] private Image iconImage;
    [SerializeField] private TextMeshProUGUI messageText;
    [SerializeField] private TextMeshProUGUI turnText;

    // Иконки по типу (назначаются в инспекторе, раздельно для торговли и квестов)
    [Header("Иконки типов")]
    [SerializeField] private Sprite buyIcon;
    [SerializeField] private Sprite sellIcon;
    [SerializeField] private Sprite questIcon;

    /// <summary>Заполняет запись данными из журнала.</summary>
    public void Setup(JournalEntryData data)
    {
        if (messageText != null)
            messageText.text = data.message;

        if (turnText != null)
            turnText.text = $"Ход {data.turnNumber}";

        if (iconImage != null)
            iconImage.sprite = ResolveIcon(data.type);
    }

    private Sprite ResolveIcon(JournalEntryType type)
    {
        switch (type)
        {
            case JournalEntryType.TradeBuy:  return buyIcon;
            case JournalEntryType.TradeSell: return sellIcon;
            case JournalEntryType.Quest:     return questIcon;
            default:                         return null;
        }
    }

    private void OnValidate()
    {
        // Лёгкая проверка в редакторе
        if (messageText == null)
            Debug.LogWarning("[JournalEntryUI] MessageText не назначен", this);
        if (turnText == null)
            Debug.LogWarning("[JournalEntryUI] TurnText не назначен", this);
    }
}