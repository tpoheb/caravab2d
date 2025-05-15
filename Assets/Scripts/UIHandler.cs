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
            Debug.LogError("Префаб окна битвы не назначен!");
            return;
        }

        currentBattleWindow = Instantiate(battleWindowPrefab);
        var bw = currentBattleWindow.GetComponent<BattleWindow>();
        if (bw != null)
            bw.Initialize(onBattleComplete);
        else
            Debug.LogError("Компонент BattleWindow не найден на префабе!");
    }

    public void ShowEventPopup(PlayerInventory inventory, Action onEventComplete)
    {
        CloseEventPopup();

        if (eventPopupPrefab == null)
        {
            Debug.LogError("Префаб попапа событий не назначен!");
            return;
        }

        currentEventPopup = Instantiate(eventPopupPrefab);
        var ep = currentEventPopup.GetComponent<EventPopup>();
        if (ep == null)
        {
            Debug.LogError("Компонент EventPopup не найден на префабе!");
            return;
        }

        int moneyChange = UnityEngine.Random.Range(-50, 100);
        inventory.Money += moneyChange;
        string msg = $"Вы {(moneyChange >= 0 ? "получили" : "потеряли")} {Mathf.Abs(moneyChange)} монет";

        ep.Initialize(msg, onEventComplete);
        Debug.Log("Событие активировано: " + msg);
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
