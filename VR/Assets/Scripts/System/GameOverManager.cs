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
    private bool _battleFinished;

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

    public static GameOverManager EnsureOffline()
    {
        GameOverManager manager = ServiceLocator.Get<GameOverManager>();
        if (manager != null || !OfflineSession.IsOffline)
            return manager;

        manager = FindAnyObjectByType<GameOverManager>();
        if (manager == null)
            manager = new GameObject("[Offline] Game Over Manager").AddComponent<GameOverManager>();
        else
            ServiceLocator.Register(manager);
        return manager;
    }

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

        string text = OfflineSession.IsOffline
            ? $"ALVOS  {enemiesKilled} / {enemiesKilledCap}"
            : $"{enemiesKilled}/{enemiesKilledCap}";
        foreach (var killsText in _killsText)
        {
            if (killsText != null)
                killsText.text = text;
        }
    }

    public void RegisterLifeBar(PlayersLifeBar lifeBar)
    {
        if (lifeBar == null)
            return;
        _playersLifeBars.RemoveAll(item => item == null);
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
        if (_battleFinished)
            return;
        Debug.Log("Enemies Killed: " + enemiesKilled + "Lasts: " + (enemiesKilledCap - enemiesKilled));
        if (enemiesKilled >= enemiesKilledCap)
        {
            Win();
        }
    }
    public void VerifyLose()
    {
        if (_battleFinished)
            return;
        _playersLifeBars.RemoveAll(item => item == null);
        foreach (PlayersLifeBar lifeBar in _playersLifeBars)
        {
            if (lifeBar.CurrentLife > 0f)
                continue;
            Lose();
            break;
        }
    }
    private void Win()
    {
        _battleFinished = true;
        GameController controller = ServiceLocator.Get<GameController>();
        if (controller != null)
            controller.BattleEnd();
    }
    private void Lose()
    {
        _battleFinished = true;
        if (OfflineSession.IsOffline)
        {
            OfflineSession.LoadDefeat();
            return;
        }

        GameController controller = ServiceLocator.Get<GameController>();
        if (controller != null)
            controller.RPC_BattleBegin();
    }
}
