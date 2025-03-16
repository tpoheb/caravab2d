using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
using System.Collections;

public class GameManagerPath : MonoBehaviour
{
    public List<GameObject> pathCells; // Список объектов-клеток пути
    public GameObject token;           // Фишка
    public Button endTurnButton;       // Кнопка "Конец хода"

    private int currentPathIndex = 0;  // Текущий индекс в списке пути
    private TokenStats tokenStats;     // Ссылка на характеристики

    void Start()
    {
        // Проверка настроек
        if (pathCells.Count == 0 || token == null || endTurnButton == null)
        {
            Debug.LogError("Не все объекты настроены или список клеток пуст!");
            return;
        }

        tokenStats = token.GetComponent<TokenStats>();
        if (tokenStats == null)
        {
            Debug.LogError("Компонент TokenStats отсутствует на объекте Token!");
            return;
        }

        // Устанавливаем фишку на первую клетку пути
        token.transform.position = pathCells[0].transform.position;
        endTurnButton.onClick.AddListener(EndTurn);
    }

    void EndTurn()
    {
        // Проверяем, можем ли двигаться дальше
        if (currentPathIndex + tokenStats.moveDistance < pathCells.Count)
        {
            currentPathIndex += tokenStats.moveDistance; // Переходим к следующей клетке
            MoveToken();
        }
        else
        {
            Debug.Log("Фишка достигла конца пути!");
        }
    }

    void MoveToken()
    {
        StartCoroutine(MoveTokenCoroutine());
    }

    IEnumerator MoveTokenCoroutine()
    {
        Vector3 startPosition = token.transform.position;
        Vector3 targetPosition = pathCells[currentPathIndex].transform.position;
        float duration = Vector3.Distance(startPosition, targetPosition) / tokenStats.moveSpeed;
        float elapsedTime = 0f;

        while (elapsedTime < duration)
        {
            elapsedTime += Time.deltaTime;
            float t = elapsedTime / duration;
            token.transform.position = Vector3.Lerp(startPosition, targetPosition, t);
            yield return null;
        }

        token.transform.position = targetPosition;
    }

    // Отладка: вывод списка клеток в консоль
    void OnValidate()
    {
        if (pathCells != null && pathCells.Count > 0)
        {
            Debug.Log("Список клеток пути:");
            for (int i = 0; i < pathCells.Count; i++)
            {
                Debug.Log($"Клетка {i}: {pathCells[i].transform.position}");
            }
        }
    }
}