using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class CardUI : MonoBehaviour
{
    public Image iconImage;
    public TMP_Text nameText;
    public TMP_Text descriptionText;
    public Button playButton;

    public void Initialize(CardData data, System.Action onPlay)
    {
        iconImage.sprite = data.icon;
        nameText.text = data.cardName;
        descriptionText.text = data.description;

        playButton.onClick.AddListener(() => onPlay?.Invoke());
    }
}