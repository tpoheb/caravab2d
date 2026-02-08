using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class CardSlotUI : MonoBehaviour
{
    public int slotIndex;
    [SerializeField] private Image iconImage;
    [SerializeField] private TextMeshProUGUI titleText;
    
    // Назначается HandManager-ом при отрисовке
    public void Setup(HandCardData data, int index)
    {
        slotIndex = index;
        titleText.text = data.cardName;
        iconImage.sprite = data.icon;
    }

    public void OnUseClick()
    {
        HandManager.Instance.UseCard(slotIndex);
    }

    public void OnDiscardClick()
    {
        HandManager.Instance.DiscardCard(slotIndex);
    }
}