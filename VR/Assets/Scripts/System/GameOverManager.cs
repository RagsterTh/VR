using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class GameOverManager : MonoBehaviour
{
    [Header("UI")]
    [SerializeField] TextMeshProUGUI[] _killsText;

    private int enemiesKilled;
    private int enemiesKilledCap = 15;
    private readonly List<PlayersLifeBar> _playersLifeBars = new();

    public int EnemiesKilled
    {
        get => enemiesKilled;
        set
        {
            enemiesKilled = value;
            UpdateKillsText();
        }
    }
    public IReadOnlyList<PlayersLifeBar> PlayersLifeBars => _playersLifeBars;

    void Awake()
    {
        Debug.Log($"[GameOverManager] Awake() on {name}, registering in ServiceLocator.");
        ServiceLocator.Register(this);
        UpdateKillsText();
    }

    void UpdateKillsText()
    {
        if (_killsText == null)
            return;

        string text = $"{enemiesKilled}/{enemiesKilledCap}";
        foreach (var killsText in _killsText)
        {
            if (killsText != null)
                killsText.text = text;
        }
    }

    public void RegisterLifeBar(PlayersLifeBar lifeBar)
    {
        if (!_playersLifeBars.Contains(lifeBar))
        {
            _playersLifeBars.Add(lifeBar);
            Debug.Log($"[GameOverManager] RegisterLifeBar: {lifeBar.name} added (total: {_playersLifeBars.Count})");
        }
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
