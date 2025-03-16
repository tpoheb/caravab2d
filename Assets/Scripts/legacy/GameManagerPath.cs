using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
using System.Collections;

public class GameManagerPath : MonoBehaviour
{
    public List<GameObject> pathCells; // ������ ��������-������ ����
    public GameObject token;           // �����
    public Button endTurnButton;       // ������ "����� ����"

    private int currentPathIndex = 0;  // ������� ������ � ������ ����
    private TokenStats tokenStats;     // ������ �� ��������������

    void Start()
    {
        // �������� ��������
        if (pathCells.Count == 0 || token == null || endTurnButton == null)
        {
            Debug.LogError("�� ��� ������� ��������� ��� ������ ������ ����!");
            return;
        }

        tokenStats = token.GetComponent<TokenStats>();
        if (tokenStats == null)
        {
            Debug.LogError("��������� TokenStats ����������� �� ������� Token!");
            return;
        }

        // ������������� ����� �� ������ ������ ����
        token.transform.position = pathCells[0].transform.position;
        endTurnButton.onClick.AddListener(EndTurn);
    }

    void EndTurn()
    {
        // ���������, ����� �� ��������� ������
        if (currentPathIndex + tokenStats.moveDistance < pathCells.Count)
        {
            currentPathIndex += tokenStats.moveDistance; // ��������� � ��������� ������
            MoveToken();
        }
        else
        {
            Debug.Log("����� �������� ����� ����!");
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

    // �������: ����� ������ ������ � �������
    void OnValidate()
    {
        if (pathCells != null && pathCells.Count > 0)
        {
            Debug.Log("������ ������ ����:");
            for (int i = 0; i < pathCells.Count; i++)
            {
                Debug.Log($"������ {i}: {pathCells[i].transform.position}");
            }
        }
    }
}