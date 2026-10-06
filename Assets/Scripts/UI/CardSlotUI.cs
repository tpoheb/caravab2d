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

    // Назначается HandManager-ом при отрисовке
    public void Setup(HandCardData data, int index)
    {
        slotIndex = index;

        if (iconImage      != null) iconImage.sprite   = data.icon;
        if (titleText      != null) titleText.text      = data.cardName;
        if (descriptionText != null)
            descriptionText.text = data.description + $"\n<size=80%><color=#D9B66F>{data.TimingLabel}</color></size>";

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
