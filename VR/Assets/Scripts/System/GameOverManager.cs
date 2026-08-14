using System.Collections.Generic;
using UnityEngine;

public class GameOverManager : MonoBehaviour
{
    private int enemiesKilled;
    private int enemiesKilledCap = 15;
    private readonly List<PlayersLifeBar> _playersLifeBars = new();

    public int EnemiesKilled { get => enemiesKilled; set => enemiesKilled = value; }
    public IReadOnlyList<PlayersLifeBar> PlayersLifeBars => _playersLifeBars;

    void Awake()
    {
        ServiceLocator.Register(this);
    }

    public void RegisterLifeBar(PlayersLifeBar lifeBar)
    {
        if (!_playersLifeBars.Contains(lifeBar))
            _playersLifeBars.Add(lifeBar);
    }

    public void UnregisterLifeBar(PlayersLifeBar lifeBar)
    {
        _playersLifeBars.Remove(lifeBar);
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
        if (_playersLifeBars.Count == 0)
            return;

        Debug.Log("Current Life: " + _playersLifeBars[0].CurrentLife);
        if (_playersLifeBars[0].CurrentLife <= 0)
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
