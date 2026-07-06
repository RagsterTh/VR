using UnityEngine;
using UnityEngine.UI;
using Photon.Pun;

public class PlayersLifeBar : MonoBehaviourPun
{
    [SerializeField] GameObject _lifeBar;
    [SerializeField] float _maxLife;

    float currentLife;

    public float CurrentLife { get => currentLife; set => currentLife = value; }

    void Awake()
    {
        ServiceLocator.Register(this);
        _lifeBar = GameObject.FindWithTag("Lifebar");
    }

    void Start()
    {
        CurrentLife = _maxLife;
        UpdateVisual();
    }

    public void TakeDamage(float amount)
    {
        photonView.RPC(nameof(RPC_TakeDamage), RpcTarget.All, amount);
        ServiceLocator.Get<GameOverManager>().VerifyLose();
    }

    [PunRPC]
    void RPC_TakeDamage(float amount)
    {
        CurrentLife = Mathf.Max(0, CurrentLife - amount);
        UpdateVisual();
    }

    void UpdateVisual()
    {
        Image _lifeBarImg = _lifeBar.GetComponent<Image>();
        _lifeBarImg.fillAmount = CurrentLife / _maxLife;
    }
}