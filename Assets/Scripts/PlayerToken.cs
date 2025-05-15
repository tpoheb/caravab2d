using UnityEngine;
using UnityEngine.UI;

public class PlayerToken : MonoBehaviour
{
    [Header("����������")]
    [SerializeField] private PathController pathController;
    [SerializeField] private UIHandler uiHandler;
    [SerializeField] private TeamSystem teamSystem;
    [SerializeField] private CityManager cityManager;
    [SerializeField] private DiceSystem diceSystem;
    [SerializeField] private PlayerInventory playerInventory;

    [Header("UI References")]
    [SerializeField] private Button endTurnButton;

    private void Start()
    {
        endTurnButton.onClick.AddListener(OnEndTurn);
        diceSystem.OnDiceRolled += ApplyDiceEffects;
        ValidateReferences();
    }

    public void SetPath(PathCellInitializer path)
    {
        pathController.SetPath(path);
    }

    private void OnEndTurn()
    {
        if (!pathController.HasActivePath())
        {
            diceSystem.RollDice();
            return;
        }

        pathController.Advance();

        if (pathController.IsPathCompleted())
        {
            ArriveAtDestination();
        }
        else
        {
            pathController.MoveCurrent();
            teamSystem.PaySalaries();
            pathController.HandleCurrentCellEffect(uiHandler, playerInventory);
        }
    }

    private void ApplyDiceEffects(int diceResult)
    {
        playerInventory.Money += diceSystem.LastMoneyModifier;
        Debug.Log($"��������� ������� ������: {(diceSystem.LastMoneyModifier >= 0 ? "+" : "")}{diceSystem.LastMoneyModifier}");
    }

    private void ArriveAtDestination()
    {
        var finishCity = pathController.CurrentPath?.FinishCity;
        if (finishCity == null)
        {
            Debug.LogWarning("����� ������ �� ����� ��� ����� ����!");
        }
        else
        {
            OpenCityPanel();
            Debug.Log($"����� ������ ������ {finishCity.CityName}");
        }

        pathController.ResetToken();
        uiHandler.CloseAll();
    }

    private void OpenCityPanel()
    {
        var finishCity = pathController.CurrentPath.FinishCity;
        var panel = cityManager.GetCityPanel(finishCity);
        panel.OpenPanel(finishCity);
    }

    private void ValidateReferences()
    {
        if (pathController == null) Debug.LogError("PathController �� ��������!");
        if (uiHandler == null) Debug.LogError("UIHandler �� ��������!");
        if (teamSystem == null) Debug.LogError("TeamSystem �� ��������!");
        if (cityManager == null) Debug.LogError("CityManager �� ��������!");
        if (diceSystem == null) Debug.LogError("DiceSystem �� ��������!");
        if (playerInventory == null) Debug.LogError("PlayerInventory �� ��������!");
        if (endTurnButton == null) Debug.LogError("EndTurnButton �� ���������!");
    }

    private void OnDestroy()
    {
        diceSystem.OnDiceRolled -= ApplyDiceEffects;
    }
}
