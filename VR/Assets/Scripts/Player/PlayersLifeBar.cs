using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Photon.Pun;

public class PlayersLifeBar : MonoBehaviourPun
{
    [SerializeField] private GameObject[] _lifeBar;
    [SerializeField] float _maxLife;

    float currentLife;

    public float CurrentLife { get => currentLife; set => currentLife = value; }
    public GameObject[] LifeBar { get => _lifeBar; set => _lifeBar = value; }

    void Awake()
    {
        ServiceLocator.Register(this);
    }

    void Start()
    {
        CurrentLife = _maxLife;
        Debug.Log($"[PlayersLifeBar] {name} Start(): maxLife={_maxLife}, currentLife={currentLife}");
        UpdateVisual();

        var gameOverManager = ServiceLocator.Get<GameOverManager>();
        if (gameOverManager != null)
        {
            gameOverManager.RegisterLifeBar(this);
            Debug.Log($"[PlayersLifeBar] {name} registered in GameOverManager (total registered: {gameOverManager.PlayersLifeBars.Count})");
        }
        else
        {
            Debug.LogWarning($"[PlayersLifeBar] {name} Start(): no GameOverManager found in ServiceLocator yet.");
        }
    }

    void OnDestroy()
    {
        var gameOverManager = ServiceLocator.Get<GameOverManager>();
        if (gameOverManager != null)
            gameOverManager.UnregisterLifeBar(this);
    }

    public void TakeDamage(float amount)
    {
        Debug.Log($"[PlayersLifeBar] {name} TakeDamage({amount}) called, dispatching RPC_TakeDamage to all clients.");
        photonView.RPC(nameof(RPC_TakeDamage), RpcTarget.All, amount);
    }

    [PunRPC]
    void RPC_TakeDamage(float amount)
    {
        var gameOverManager = ServiceLocator.Get<GameOverManager>();
        IReadOnlyList<PlayersLifeBar> targets;
        if (gameOverManager != null && gameOverManager.PlayersLifeBars.Count > 0)
        {
            targets = gameOverManager.PlayersLifeBars;
        }
        else
        {
            Debug.LogWarning($"[PlayersLifeBar] {name} RPC_TakeDamage: no GameOverManager/registered life bars found, applying damage only to self.");
            targets = new List<PlayersLifeBar> { this };
        }

        foreach (var lifeBar in targets)
        {
            float before = lifeBar.CurrentLife;
            lifeBar.CurrentLife = Mathf.Max(0, lifeBar.CurrentLife - amount);
            Debug.Log($"[PlayersLifeBar] {lifeBar.name} life {before} -> {lifeBar.CurrentLife} (damage={amount})");
            lifeBar.UpdateVisual();
        }

        if (gameOverManager != null)
            gameOverManager.VerifyLose();
    }

    void UpdateVisual()
    {
        if (LifeBar == null || LifeBar.Length == 0)
        {
            Debug.LogWarning($"{nameof(PlayersLifeBar)}: LifeBar is not assigned on {name}.", this);
            return;
        }

        foreach (GameObject lifeBar in LifeBar)
        {
            Image lifeBarImg = lifeBar.GetComponent<Image>();
            lifeBarImg.fillAmount = CurrentLife / _maxLife;
        }
    }
}