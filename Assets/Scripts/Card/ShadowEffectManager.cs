using System.Collections.Generic;
using UnityEngine;

public class ShadowEffectManager : MonoBehaviour
{
    [SerializeField] private PlayerStats playerStats;
    [SerializeField] private PlayerInventory playerInventory;

    private List<ActiveEffect> activeEffects = new List<ActiveEffect>();

    [System.Serializable]
    public class ActiveEffect
    {
        public ShadowCardData data;
        public int remainingTurns;
    }

    public void ApplyCard(ShadowCardData card)
    {
        if (card.effectType == ShadowEffectType.Money)
        {
            // Золото применяется мгновенно и не длится 10 ходов
            if (card.value > 0) playerInventory.AddMoney(card.value);
            else playerInventory.TrySpendMoney(Mathf.Abs(card.value));
            return;
        }

        // Для характеристик (Атака, Грузоп., Торг)
        ActiveEffect newEffect = new ActiveEffect { data = card, remainingTurns = card.duration };
        activeEffects.Add(newEffect);
        
        ApplyStatsChange(card.effectType, card.value);
        Debug.Log($"Эффект {card.cardName} применен на {card.duration} ходов.");
    }

    // Вызывать этот метод каждый раз при завершении хода (EndTurn)
    public void ProcessTurn()
    {
        for (int i = activeEffects.Count - 1; i >= 0; i--)
        {
            activeEffects[i].remainingTurns--;
            if (activeEffects[i].remainingTurns <= 0)
            {
                // Время вышло — откатываем изменения
                RemoveStatsChange(activeEffects[i].data.effectType, activeEffects[i].data.value);
                activeEffects.RemoveAt(i);
            }
        }
    }

    private void ApplyStatsChange(ShadowEffectType type, int value)
    {
        if (playerStats == null) return;

        switch (type)
        {
            case ShadowEffectType.Attack:
                playerStats.ModifyAttack(value); // Используем твой новый метод
                break;
            case ShadowEffectType.Capacity:
                playerStats.ModifyCapacity(value);
                break;
            case ShadowEffectType.Bargain:
                playerStats.ModifyBargain(value);
                break;
        }
    }

    private void RemoveStatsChange(ShadowEffectType type, int value)
    {
        ApplyStatsChange(type, -value); // Инвертируем значение для отката
    }
}