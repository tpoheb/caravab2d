using UnityEngine;

public class CardManager : MonoBehaviour
{
    [Header("Зависимости")]
    [SerializeField] private GameManager gameManager;
    
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
        Debug.Log("CardManager: ВЗЯТЬ КАРТУ. Запуск анимации...");
        
        // --- Здесь будет логика выбора и применения эффекта карты ---

        // Временная заглушка: применяем эффект и завершаем фазу
        Invoke(nameof(ApplyCardEffectAndComplete), 0.5f);
    }

    private void ApplyCardEffectAndComplete()
    {
        Debug.Log("CardManager: Эффект карты применен. Фаза события завершена.");

        // Оповещаем GameManager, что фаза события завершена, чтобы он мог продолжить ход.
        gameManager.CompleteEventPhase();
    }
}