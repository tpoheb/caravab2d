using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class EventPopup : MonoBehaviour
{
    [Header("Card System")]
    public CardDeckManager deckManager;
    public CardHandUI handUI;
    public PlayerInventory player;

    [SerializeField] private TextMeshProUGUI messageText;
    [SerializeField] private Button closeButton;

    private System.Action onComplete;

    public void Initialize(string message, System.Action completeCallback)
    {
        messageText.text = message;
        this.onComplete = completeCallback;
        closeButton.onClick.AddListener(OnClose);
        gameObject.SetActive(true);
    }
    public void InitializeWithCardDraw(string message, System.Action completeCallback)
    {
        Initialize(message, completeCallback);

        // Игрок получает карту при событии
        CardData newCard = deckManager.DrawCard();
        if (newCard != null)
        {
            deckManager.CurrentHand.Add(newCard);
            handUI.UpdateHand(deckManager.CurrentHand, deckManager, player);
        }
    }

        private void OnClose()
    {
        onComplete?.Invoke();
        Destroy(gameObject);
    }
}