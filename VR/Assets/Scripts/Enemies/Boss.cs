using Photon.Pun;
using UnityEngine;
using UnityEngine.Events;

public class Boss : MonoBehaviour, IShootable
{
    [SerializeField] float damage;
    [SerializeField] int maxHits = 5;
    [Header("Death Effect")]
    [SerializeField] GameObject deathEffectPrefab;
    [SerializeField] float deathEffectLifetime = 10f;
    int currentHits;
    PhotonView _phView;

    public UnityEvent OnDeath;

    void Awake()
    {
        _phView = GetComponent<PhotonView>();
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            ServiceLocator.Get<PlayersLifeBar>()?.TakeDamage(damage);
        }
    }

    public void Hit()
    {
        _phView.RPC(nameof(RPC_Hit), RpcTarget.All);
    }

    [PunRPC]
    void RPC_Hit()
    {
        currentHits++;
        if (currentHits >= maxHits)
        {
            if (deathEffectPrefab != null)
            {
                GameObject deathEffect = Instantiate(deathEffectPrefab, transform.position, Quaternion.identity);
                Destroy(deathEffect, deathEffectLifetime);
            }
            OnDeath.Invoke();
            gameObject.SetActive(false);
        }
    }
}
