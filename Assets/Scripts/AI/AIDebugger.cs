using System.Collections;
using UnityEngine;

/// <summary>
/// Временный диагностический скрипт.
/// Повесь на тот же GameObject что AITurnManager.
/// Удали после отладки.
/// </summary>
public class AIDebugger : MonoBehaviour
{
    [SerializeField] private PlayerTraderAdapter playerAdapter;
    [SerializeField] private AITurnManager aiTurnManager;

    private void Start()
    {
        StartCoroutine(DiagnosticLoop());
    }

    private IEnumerator DiagnosticLoop()
    {
        // Ждём пока всё инициализируется
        yield return new WaitForSeconds(1f);

        while (true)
        {
            yield return new WaitForSeconds(3f);

            // --- 1. Проверяем PlayerTraderAdapter ---
            if (playerAdapter == null)
            {
                Debug.LogError("[AIDebug] PlayerTraderAdapter не назначен в инспекторе!");
                continue;
            }

            Debug.Log($"[AIDebug] PlayerAdapter.IsReady = {playerAdapter.IsReady} | " +
                      $"CurrentCity = {playerAdapter.CurrentCity?.CityName ?? "NULL"}");

            // --- 2. Проверяем ИИ торговцев ---
            if (aiTurnManager == null)
            {
                Debug.LogError("[AIDebug] AITurnManager не назначен!");
                continue;
            }

            foreach (var ai in aiTurnManager.AiTraders)
            {
                Debug.Log($"[AIDebug] ИИ '{ai.DisplayName}': " +
                          $"City={ai.CurrentCity?.CityName ?? "NULL"} | " +
                          $"Gold={ai.Gold} | " +
                          $"HasPathController={ai.GetComponent<PathController>() != null}");
            }

            // --- 3. Проверяем WorldEconomy snapshot ---
            var economy = FindAnyObjectByType<WorldEconomy>();
            if (economy == null)
            {
                Debug.LogError("[AIDebug] WorldEconomy не найден в сцене!");
                continue;
            }

            var snapshot = economy.TakeSnapshot();
            Debug.Log($"[AIDebug] Snapshot: {snapshot.Cities.Count} городов, " +
                      $"{snapshot.AllGoods.Count} товаров, ход {snapshot.TurnNumber}");

            if (snapshot.Cities.Count == 0)
                Debug.LogError("[AIDebug] Snapshot пустой! Проверь CityBindings в WorldEconomy.");

            if (snapshot.AllGoods.Count == 0)
                Debug.LogError("[AIDebug] Нет товаров! Проверь CityData.items.");
        }
    }
}