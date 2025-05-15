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
    [SerializeField] private Transform uiCanvas; // ������ �� Canvas ��� �������� ����

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
            Debug.LogWarning("����� ������ �� ����� ��� ����� ����!");
            return;
        }

        OpenCityPanel();
        Debug.Log($"����� ������ ������ {currentPath.FinishCity.CityName}");
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
            Debug.LogError($"���������� ������������� �� ������ {cellIndex}");
            return;
        }

        tokenObject.transform.position = pathCells[cellIndex].Position;
        Debug.Log($"����� ���������� �� ������ {cellIndex}");
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
            Debug.LogError("������ ���� ����� �� ��������!");
            return;
        }

        // ������� ���������� ���� ���� ����
        if (currentBattleWindow != null)
        {
            Destroy(currentBattleWindow);
        }

        // ������� ����� ����
        currentBattleWindow = Instantiate(battleWindowPrefab, uiCanvas);
        Debug.Log("���� ����� �������");

        // �������� ��������� ����
        var battleWindow = currentBattleWindow.GetComponent<BattleWindow>();
        if (battleWindow != null)
        {
            battleWindow.Initialize(OnBattleComplete); // ���������� Initialize ������ OpenWindow
        }
        else
        {
            Debug.LogError("��������� BattleWindow �� ������ �� �������!");
        }
    }

    private void OnBattleComplete()
    {
        Debug.Log("����� ���������");
        if (currentBattleWindow != null)
        {
            Destroy(currentBattleWindow);
        }
    }

    private void ShowEventPopup()
    {
        if (eventPopupPrefab == null)
        {
            Debug.LogError("������ ������ ������� �� ��������!");
            return;
        }

        // ������� ���������� ����� ���� ����
        if (currentEventPopup != null)
        {
            Destroy(currentEventPopup);
        }

        // ������� ����� �����
        currentEventPopup = Instantiate(eventPopupPrefab, uiCanvas);

        // �������� ��������� EventPopup
        EventPopup eventPopupComponent = currentEventPopup.GetComponent<EventPopup>();
        if (eventPopupComponent == null)
        {
            Debug.LogError("�� ������ ��������� EventPopup �� �������!");
            return;
        }

        // ���������� �������
        int moneyChange = Random.Range(-50, 100);
        playerInventory.Money += moneyChange;
        string message = $"�� {(moneyChange >= 0 ? "��������" : "��������")} {Mathf.Abs(moneyChange)} �����";

        // �������� ����� Initialize � ����������
        eventPopupComponent.Initialize(message, OnEventComplete);
        Debug.Log("������� ������������: " + message);
    }

    private void OnEventComplete()
    {
        Debug.Log("������� ���������");
        if (currentEventPopup != null)
        {
            Destroy(currentEventPopup);
        }
    }

    private void TriggerRandomEvent()
    {
        int moneyChange = Random.Range(-50, 100);
        playerInventory.Money += moneyChange;
        string message = $"�� {(moneyChange >= 0 ? "��������" : "��������")} {Mathf.Abs(moneyChange)} �����";

        if (currentEventPopup != null)
        {
            EventPopup eventPopupComponent = currentEventPopup.GetComponent<EventPopup>();
            if (eventPopupComponent != null)
            {
                eventPopupComponent.Initialize(message, OnEventComplete);
            }
            else
            {
                Debug.LogError("��������� EventPopup �� ������ �� ������� ����!");
            }
        }
        else
        {
            Debug.LogWarning("���� ������� �� �������!");
        }
    }

    private void OnDestroy()
    {
        diceSystem.OnDiceRolled -= ApplyDiceEffects;

        // ������� ���� ��� ����������� �������
        if (currentBattleWindow != null) Destroy(currentBattleWindow);
        if (currentEventPopup != null) Destroy(currentEventPopup);
    }

    private void ValidateReferences()
    {
        if (cityManager == null) Debug.LogError("CityManager �� ��������!");
        if (currentBattleWindow == null) Debug.LogWarning("BattleWindow �� ��������!");
        if (currentEventPopup == null) Debug.LogWarning("EventPopup �� ��������!");
    }

    private void InitializePathCells(PathCellInitializer path)
    {
        pathCells = new Cell[path.transform.childCount];
        for (int i = 0; i < path.transform.childCount; i++)
        {
            pathCells[i] = path.transform.GetChild(i).GetComponent<Cell>();
            if (pathCells[i] == null) Debug.LogError($"������ {i} �� ���� �� ����� ���������� Cell");
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
        Debug.Log($"��������� ������� ������:\n������: {(diceSystem.LastMoneyModifier >= 0 ? "+" : "")}{diceSystem.LastMoneyModifier}\n");
    }


}