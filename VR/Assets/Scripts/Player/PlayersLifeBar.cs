using UnityEngine;
using UnityEngine.UI;
using Photon.Pun;

public class PlayersLifeBar : MonoBehaviourPun
{
    [SerializeField] Image _lifeBar;
    [SerializeField] float _maxLife;

    float currentLife;

    void Awake()
    {
        ServiceLocator.Register(this);
    }

    void Start()
    {
        currentLife = _maxLife;
        UpdateVisual();
    }

    public void TakeDamage(float amount)
    {
        photonView.RPC(nameof(RPC_TakeDamage), RpcTarget.All, amount);
    }

    [PunRPC]
    void RPC_TakeDamage(float amount)
    {
        currentLife = Mathf.Max(0, currentLife - amount);
        UpdateVisual();
    }

    void UpdateVisual()
    { 
        _lifeBar.fillAmount = currentLife / _maxLife;
    }
}