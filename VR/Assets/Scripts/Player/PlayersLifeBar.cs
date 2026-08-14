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
        UpdateVisual();

        var gameOverManager = ServiceLocator.Get<GameOverManager>();
        if (gameOverManager != null)
            gameOverManager.RegisterLifeBar(this);
    }

    void OnDestroy()
    {
        var gameOverManager = ServiceLocator.Get<GameOverManager>();
        if (gameOverManager != null)
            gameOverManager.UnregisterLifeBar(this);
    }

    public void TakeDamage(float amount)
    {
        photonView.RPC(nameof(RPC_TakeDamage), RpcTarget.All, amount);
    }

    [PunRPC]
    void RPC_TakeDamage(float amount)
    {
        var gameOverManager = ServiceLocator.Get<GameOverManager>();
        if (gameOverManager == null)
            return;

        foreach (var lifeBar in gameOverManager.PlayersLifeBars)
        {
            lifeBar.CurrentLife = Mathf.Max(0, lifeBar.CurrentLife - amount);
            lifeBar.UpdateVisual();
        }

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