using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class CardSlotUI : MonoBehaviour
{
    public int slotIndex;

    [SerializeField] private Image iconImage;
    [SerializeField] private TextMeshProUGUI titleText;
    [SerializeField] private TextMeshProUGUI descriptionText;

    // Назначается HandManager-ом при отрисовке
    public void Setup(HandCardData data, int index)
    {
        slotIndex = index;

        if (iconImage      != null) iconImage.sprite   = data.icon;
        if (titleText      != null) titleText.text      = data.cardName;
        if (descriptionText != null) descriptionText.text = data.description;
    }

    public void OnUseClick()     => HandManager.Instance.UseCard(slotIndex);
    public void OnDiscardClick() => HandManager.Instance.DiscardCard(slotIndex);
}