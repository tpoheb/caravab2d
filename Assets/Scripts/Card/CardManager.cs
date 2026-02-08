using System.Collections.Generic;
using UnityEngine;

public class CardManager : MonoBehaviour
{
    
    [SerializeField] private GameManager gameManager;
    [SerializeField] private List<ShadowCardData> allShadowCards;
    [SerializeField] private BattleUIManager uiManager;
    [SerializeField] private ShadowEffectManager effectManager;
    [SerializeField] private List<BattleCardData> allBattleCards;
    
    // --- Одиночка (опционально) ---
    public static CardManager Instance { get; private set; }

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }
    
    /// <summary>
    /// Вызывается из GameManager при выпадении события "Тень Влияния" (DiceEventType.ShadowInfluence).
    /// </summary>
   
    public void DrawCard()
    {
        if (allShadowCards.Count == 0) return;

        // Случайный выбор
        int index = Random.Range(0, allShadowCards.Count);
        ShadowCardData selectedCard = allShadowCards[index];

        // Применяем эффект
        effectManager.ApplyCard(selectedCard);

        // Показываем в UI
        uiManager.DisplayShadowCard(selectedCard);
    }
    public BattleCardData GetRandomBattleCard()
    {
        if (allBattleCards.Count == 0) return null;
        return allBattleCards[Random.Range(0, allBattleCards.Count)];
    }
    private void ApplyCardEffectAndComplete()
    {
        Debug.Log("CardManager: Эффект карты применен. Фаза события завершена.");

        // Оповещаем GameManager, что фаза события завершена, чтобы он мог продолжить ход.
        gameManager.CompleteEventPhase();
    }
}