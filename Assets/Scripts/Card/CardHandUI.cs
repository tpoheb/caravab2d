using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;

public class CardHandUI : MonoBehaviour
{
    public GameObject cardPrefab;
    public Transform handContainer;
    public float cardSpacing = 100f;

    private List<GameObject> cardInstances = new List<GameObject>();

    public void UpdateHand(List<CardData> hand, CardDeckManager deckManager, PlayerInventory player)
    {
        ClearHand();

        for (int i = 0; i < hand.Count; i++)
        {
            GameObject cardObj = Instantiate(cardPrefab, handContainer);
            cardObj.transform.localPosition = new Vector3(i * cardSpacing, 0, 0);

            CardUI cardUI = cardObj.GetComponent<CardUI>();
            cardUI.Initialize(hand[i], () =>
            {
                deckManager.PlayCard(hand[i], player);
                UpdateHand(deckManager.currentHand, deckManager, player);
            });

            cardInstances.Add(cardObj);
        }
    }

    private void ClearHand()
    {
        foreach (GameObject card in cardInstances)
        {
            Destroy(card);
        }
        cardInstances.Clear();
    }
}