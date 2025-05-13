using UnityEngine;
using UnityEngine.UI;

public class PlayerToken : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private Button endTurnButton;
    [SerializeField] private GameObject tokenObject;
    [SerializeField] private TeamSystem teamSystem;
    [SerializeField] private BattleWindow battleWindow;
    [SerializeField] private EventPopup eventPopup;

    [Header("Game References")]
    [SerializeField] private CityManager cityManager;
    [SerializeField] private DiceSystem diceSystem;
    [SerializeField] private PlayerInventory playerInventory;


    private PathCellInitializer currentPath;
    private int currentCellIndex = -1;
    private Cell[] pathCells;

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
        if (currentCellIndex < 0 || currentCellIndex >= pathCells.Length)
        {
            Debug.LogError($"Индекс клетки {currentCellIndex} вне границ массива!");
            return;
        }

        Cell currentCell = pathCells[currentCellIndex];
        if (currentCell == null)
        {
            Debug.LogError($"Клетка {currentCellIndex} равна null!");
            return;
        }

        Debug.Log($"[ЯЧЕЙКА] Фишка на клетке {currentCellIndex} ({currentCell.Type})");

        switch (currentCell.Type)
        {
            case CellType.Battle:
                Debug.Log("[БИТВА] Начинаем битву...");
                if (battleWindow != null)
                {
                    battleWindow.OpenWindow();
                    Debug.Log("[БИТВА] Окно битвы открыто");
                }
                else
                {
                    Debug.LogError("[БИТВА] Окно битвы не назначено!");
                }
                break;

            case CellType.Event:
                Debug.Log("[СОБЫТИЕ] Активируем случайное событие...");
                TriggerRandomEvent();
                break;

            default:
                Debug.Log($"[ЯЧЕЙКА] Обычная клетка, ничего не происходит");
                break;
        }
    }

    private void TriggerRandomEvent()
    {
        int moneyChange = Random.Range(-50, 100);
        playerInventory.Money += moneyChange;

        if (eventPopup != null)
            eventPopup.ShowEvent($"Вы {(moneyChange >= 0 ? "получили" : "потеряли")} {Mathf.Abs(moneyChange)} монет");
        else
            Debug.LogWarning("Окно событий не назначено!");
    }

    private void OnDestroy()
    {
        diceSystem.OnDiceRolled -= ApplyDiceEffects;
    }

    private void ValidateReferences()
    {
        if (cityManager == null) Debug.LogError("CityManager не назначен!");
        if (battleWindow == null) Debug.LogWarning("BattleWindow не назначен!");
        if (eventPopup == null) Debug.LogWarning("EventPopup не назначен!");
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