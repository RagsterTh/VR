using UnityEngine;

public class GameOverManager : MonoBehaviour
{
    private int enemiesKilled;
    private int enemiesKilledCap = 15;

    public int EnemiesKilled { get => enemiesKilled; set => enemiesKilled = value; }

    void Awake()
    {
        ServiceLocator.Register(this);
    }
    public void VerifyWin()
    {
        Debug.Log("Enemies Killed: " + enemiesKilled + "Lasts: " + (enemiesKilledCap - enemiesKilled));
        if (enemiesKilled >= enemiesKilledCap)
        {
            Win();
        }
    }
    public void VerifyLose()
    {
        Debug.Log("Current Life: " + ServiceLocator.Get<PlayersLifeBar>().CurrentLife);
        if (ServiceLocator.Get<PlayersLifeBar>().CurrentLife <= 0)
        {
            Lose();
        }
    }
    private void Win()
    {
        ServiceLocator.Get<GameController>().BattleEnd();
    }
    private void Lose()
    {
        ServiceLocator.Get<GameController>().RPC_BattleBegin();
    }
}
