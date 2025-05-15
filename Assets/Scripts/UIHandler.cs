using UnityEngine;
using System;

public class UIHandler : MonoBehaviour
{
    [Header("Window Prefabs")]
    [SerializeField] private GameObject battleWindowPrefab;
    [SerializeField] private GameObject eventPopupPrefab;

    private GameObject currentBattleWindow;
    private GameObject currentEventPopup;

    public void ShowBattleWindow(Action onBattleComplete)
    {
        CloseBattleWindow();

        if (battleWindowPrefab == null)
        {
            Debug.LogError("������ ���� ����� �� ��������!");
            return;
        }

        currentBattleWindow = Instantiate(battleWindowPrefab);
        var bw = currentBattleWindow.GetComponent<BattleWindow>();
        if (bw != null)
            bw.Initialize(onBattleComplete);
        else
            Debug.LogError("��������� BattleWindow �� ������ �� �������!");
    }

    public void ShowEventPopup(PlayerInventory inventory, Action onEventComplete)
    {
        CloseEventPopup();

        if (eventPopupPrefab == null)
        {
            Debug.LogError("������ ������ ������� �� ��������!");
            return;
        }

        currentEventPopup = Instantiate(eventPopupPrefab);
        var ep = currentEventPopup.GetComponent<EventPopup>();
        if (ep == null)
        {
            Debug.LogError("��������� EventPopup �� ������ �� �������!");
            return;
        }

        int moneyChange = UnityEngine.Random.Range(-50, 100);
        inventory.Money += moneyChange;
        string msg = $"�� {(moneyChange >= 0 ? "��������" : "��������")} {Mathf.Abs(moneyChange)} �����";

        ep.Initialize(msg, onEventComplete);
        Debug.Log("������� ������������: " + msg);
    }

    public void CloseAll()
    {
        CloseBattleWindow();
        CloseEventPopup();
    }

    private void CloseBattleWindow()
    {
        if (currentBattleWindow != null)
            Destroy(currentBattleWindow);
    }

    private void CloseEventPopup()
    {
        if (currentEventPopup != null)
            Destroy(currentEventPopup);
    }
}
