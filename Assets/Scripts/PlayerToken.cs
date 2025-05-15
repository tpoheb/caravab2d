using UnityEngine;
using UnityEngine.UI;

public class PlayerToken : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private Button endTurnButton;
    [SerializeField] private GameObject tokenObject;
    [SerializeField] private TeamSystem teamSystem;

    [Header("Window Prefabs")]
    [SerializeField] private GameObject battleWindowPrefab;
    [SerializeField] private GameObject eventPopupPrefab;

    [Header("Game References")]
    [SerializeField] private CityManager cityManager;
    [SerializeField] private DiceSystem diceSystem;
    [SerializeField] private PlayerInventory playerInventory;
    [SerializeField] private Transform uiCanvas; // Ссылка на Canvas для создания окон

    private PathCellInitializer currentPath;
    private int currentCellIndex = -1;
    private Cell[] pathCells;
    private GameObject currentBattleWindow;
    private GameObject currentEventPopup;

    private void Start()
    {
        endTurnButton.onClick.AddListener(OnEndTurn);
        tokenObject.SetActive(false);
        diceSystem.OnDiceRolled += ApplyDiceEffects;
        ValidateReferences();
    }

    public void SetPath(PathCellInitializer path)
    {
        if (path == null) return;

        currentPath = path;
        currentCellIndex = 0;
        InitializePathCells(path);
        MoveToCell(currentCellIndex);
        tokenObject.SetActive(true);
    }

    private void OnEndTurn()
    {
        if (!HasActivePath())
        {
            diceSystem.RollDice();
            return;
        }

        currentCellIndex++;

        if (IsPathCompleted())
        {
            ArriveAtDestination();
        }
        else
        {
            ContinueMoving();
            HandleCurrentCellEffect();
        }
    }

    private void ApplyDiceEffects(int diceResult)
    {
        playerInventory.Money += diceSystem.LastMoneyModifier;
        LogDiceEffects();
    }

    private void ArriveAtDestination()
    {
        if (currentPath.FinishCity == null)
        {
            Debug.LogWarning("Город финиша не задан для этого пути!");
            return;
        }

        OpenCityPanel();
        Debug.Log($"Игрок достиг города {currentPath.FinishCity.CityName}");
        ResetToken();
    }

    private void ResetToken()
    {
        currentPath = null;
        currentCellIndex = -1;
        tokenObject.SetActive(false);
    }

    private void MoveToCell(int cellIndex)
    {
        if (IsInvalidCellIndex(cellIndex))
        {
            Debug.LogError($"Невозможно переместиться на клетку {cellIndex}");
            return;
        }

        tokenObject.transform.position = pathCells[cellIndex].Position;
        Debug.Log($"Фишка перемещена на клетку {cellIndex}");
    }

    private void HandleCurrentCellEffect()
    {
        Cell currentCell = pathCells[currentCellIndex];

        switch (currentCell.Type)
        {
            case CellType.Battle:
                ShowBattleWindow();
                break;

            case CellType.Event:
                ShowEventPopup();
                break;
        }
    }

    private void ShowBattleWindow()
    {
        if (battleWindowPrefab == null)
        {
            Debug.LogError("Префаб окна битвы не назначен!");
            return;
        }

        // Удаляем предыдущее окно если есть
        if (currentBattleWindow != null)
        {
            Destroy(currentBattleWindow);
        }

        // Создаем новое окно
        currentBattleWindow = Instantiate(battleWindowPrefab, uiCanvas);
        Debug.Log("Окно битвы создано");

        // Получаем компонент окна
        var battleWindow = currentBattleWindow.GetComponent<BattleWindow>();
        if (battleWindow != null)
        {
            battleWindow.Initialize(OnBattleComplete); // Используем Initialize вместо OpenWindow
        }
        else
        {
            Debug.LogError("Компонент BattleWindow не найден на префабе!");
        }
    }

    private void OnBattleComplete()
    {
        Debug.Log("Битва завершена");
        if (currentBattleWindow != null)
        {
            Destroy(currentBattleWindow);
        }
    }

    private void ShowEventPopup()
    {
        if (eventPopupPrefab == null)
        {
            Debug.LogError("Префаб попапа событий не назначен!");
            return;
        }

        // Удаляем предыдущий попап если есть
        if (currentEventPopup != null)
        {
            Destroy(currentEventPopup);
        }

        // Создаем новый попап
        currentEventPopup = Instantiate(eventPopupPrefab, uiCanvas);

        // Получаем компонент EventPopup
        EventPopup eventPopupComponent = currentEventPopup.GetComponent<EventPopup>();
        if (eventPopupComponent == null)
        {
            Debug.LogError("Не найден компонент EventPopup на префабе!");
            return;
        }

        // Генерируем событие
        int moneyChange = Random.Range(-50, 100);
        playerInventory.Money += moneyChange;
        string message = $"Вы {(moneyChange >= 0 ? "получили" : "потеряли")} {Mathf.Abs(moneyChange)} монет";

        // Вызываем метод Initialize у компонента
        eventPopupComponent.Initialize(message, OnEventComplete);
        Debug.Log("Событие активировано: " + message);
    }

    private void OnEventComplete()
    {
        Debug.Log("Событие завершено");
        if (currentEventPopup != null)
        {
            Destroy(currentEventPopup);
        }
    }

    private void TriggerRandomEvent()
    {
        int moneyChange = Random.Range(-50, 100);
        playerInventory.Money += moneyChange;
        string message = $"Вы {(moneyChange >= 0 ? "получили" : "потеряли")} {Mathf.Abs(moneyChange)} монет";

        if (currentEventPopup != null)
        {
            EventPopup eventPopupComponent = currentEventPopup.GetComponent<EventPopup>();
            if (eventPopupComponent != null)
            {
                eventPopupComponent.Initialize(message, OnEventComplete);
            }
            else
            {
                Debug.LogError("Компонент EventPopup не найден на объекте окна!");
            }
        }
        else
        {
            Debug.LogWarning("Окно событий не создано!");
        }
    }

    private void OnDestroy()
    {
        diceSystem.OnDiceRolled -= ApplyDiceEffects;

        // Удаляем окна при уничтожении объекта
        if (currentBattleWindow != null) Destroy(currentBattleWindow);
        if (currentEventPopup != null) Destroy(currentEventPopup);
    }

    private void ValidateReferences()
    {
        if (cityManager == null) Debug.LogError("CityManager не назначен!");
        if (currentBattleWindow == null) Debug.LogWarning("BattleWindow не назначен!");
        if (currentEventPopup == null) Debug.LogWarning("EventPopup не назначен!");
    }

    private void InitializePathCells(PathCellInitializer path)
    {
        pathCells = new Cell[path.transform.childCount];
        for (int i = 0; i < path.transform.childCount; i++)
        {
            pathCells[i] = path.transform.GetChild(i).GetComponent<Cell>();
            if (pathCells[i] == null) Debug.LogError($"Клетка {i} на пути не имеет компонента Cell");
        }
    }

    private bool HasActivePath()
    {
        return currentPath != null && currentCellIndex >= 0;
    }

    private bool IsPathCompleted()
    {
        return currentCellIndex >= pathCells.Length;
    }

    private void ContinueMoving()
    {
        MoveToCell(currentCellIndex);
        teamSystem.PaySalaries();
        diceSystem.RollDice();
    }

    private bool IsInvalidCellIndex(int cellIndex)
    {
        return cellIndex < 0 || cellIndex >= pathCells.Length || pathCells[cellIndex] == null;
    }

    private void OpenCityPanel()
    {
        CityPanel destinationPanel = cityManager.GetCityPanel(currentPath.FinishCity);
        destinationPanel.OpenPanel(currentPath.FinishCity);
    }

    private void LogDiceEffects()
    {
        Debug.Log($"Применены эффекты кубика:\nДеньги: {(diceSystem.LastMoneyModifier >= 0 ? "+" : "")}{diceSystem.LastMoneyModifier}\n");
    }


}