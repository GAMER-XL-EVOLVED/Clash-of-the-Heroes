using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }
    
    public enum Team { Blue, Red }
    public Team currentTeam = Team.Blue;
    
    public List<UnitController> blueTeamUnits = new List<UnitController>();
    public List<UnitController> redTeamUnits = new List<UnitController>();
    
    public Button endTurnButton;
    
    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
        }
    }
    
    void Start()
    {
        if (endTurnButton != null)
        {
            endTurnButton.onClick.AddListener(EndTurn);
        }
    }
    
    public void EndTurn()
    {
        currentTeam = currentTeam == Team.Blue ? Team.Red : Team.Blue;
        Debug.Log($"Current Turn: {currentTeam}");
    }
    
    public bool IsUnitsTurn(UnitController unit)
    {
        return (unit.team == currentTeam);
    }
} 