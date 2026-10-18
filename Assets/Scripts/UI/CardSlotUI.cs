using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class CardSlotUI : MonoBehaviour
{
    public int slotIndex;

    [SerializeField] private Button useButton;
    [SerializeField] private Image iconImage;
    [SerializeField] private TextMeshProUGUI titleText;
    [SerializeField] private TextMeshProUGUI descriptionText;

    private bool _layoutCaptured;
    private Vector2 _descriptionPosition, _descriptionSize;

    // Назначается HandManager-ом при отрисовке
    public void Setup(HandCardData data, int index)
    {
        slotIndex = index;

        if (iconImage != null)
        {
            iconImage.sprite = data.icon;
            iconImage.gameObject.SetActive(data.icon != null);
        }
        if (titleText      != null) titleText.text      = data.cardName;
        if (descriptionText != null)
        {
            var rect = descriptionText.rectTransform;
            if (!_layoutCaptured)
            {
                _descriptionPosition = rect.anchoredPosition; _descriptionSize = rect.sizeDelta;
                _layoutCaptured = true;
            }
            rect.anchoredPosition = _descriptionPosition; rect.sizeDelta = _descriptionSize;
            if (data.icon == null && iconImage != null)
            {
                // У карт без иллюстрации текст занимает её место, не перекрывая кнопки.
                var iconRect = iconImage.rectTransform;
                float bottom = _descriptionPosition.y - _descriptionSize.y * 0.5f;
                float top = iconRect.anchoredPosition.y + iconRect.sizeDelta.y * 0.5f - 8f;
                rect.sizeDelta = new Vector2(_descriptionSize.x, top - bottom);
                rect.anchoredPosition = new Vector2(_descriptionPosition.x, (top + bottom) * 0.5f);
            }
            descriptionText.enableAutoSizing = true;
            descriptionText.fontSizeMin = 8; descriptionText.fontSizeMax = 10;
            descriptionText.text = data.description + $"\n<size=80%><color=#D9B66F>{data.TimingLabel}</color></size>" +
                (string.IsNullOrEmpty(data.unavailableReason) ? "" : $"\n<size=80%><color=#D98575>Недоступна: {data.unavailableReason}</color></size>");
        }

        // Совместимость с префабами, в которых новое поле ещё не назначено.
        if (useButton == null)
        {
            foreach (var button in GetComponentsInChildren<Button>(true))
                for (int i = 0; i < button.onClick.GetPersistentEventCount(); i++)
                    if (button.onClick.GetPersistentMethodName(i) == nameof(OnUseClick))
                        useButton = button;
        }
        if (useButton != null) useButton.interactable = HandManager.Instance != null && HandManager.Instance.CanUseCard(index);
    }

    public void OnUseClick()     => HandManager.Instance?.UseCard(slotIndex);
    public void OnDiscardClick() => HandManager.Instance.DiscardCard(slotIndex);
}
