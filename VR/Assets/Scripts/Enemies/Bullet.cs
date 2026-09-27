using System.Collections;
using UnityEngine;
using Photon.Pun;
using Oculus.Interaction;

public class Bullet : MonoBehaviour
{
    [SerializeField] float damage;

    [SerializeField] bool hasCollided;

    private void Start()
    {
        hasCollided = false;
        StartCoroutine(DestroyBullet());
    }

    void OnCollisionEnter(Collision collision)
    {
        hasCollided = true;
        if (collision.collider.CompareTag("Player"))
        {
            DamagePlayer(collision.collider);
        }
        Collision();
    }
    
    // The player's damage collider is a trigger, so OnCollisionEnter never fires against it.
    void OnTriggerEnter(Collider other)
    {
        if (hasCollided || !other.CompareTag("Player"))
            return;

        hasCollided = true;
        DamagePlayer(other);
    }

    private void DamagePlayer(Collider playerCollider)
    {
        PlayerPrefabNetwork player = playerCollider.GetComponentInParent<PlayerPrefabNetwork>();
        PlayersLifeBar lifeBar = player != null
            ? player.GetComponentInChildren<PlayersLifeBar>(true)
            : ServiceLocator.Get<PlayersLifeBar>();
        lifeBar?.TakeDamage(damage);
    }

    bool Collision()    
    {
        return hasCollided;
    }

    IEnumerator DestroyBullet()
    {
        yield return new WaitUntil(Collision);
        if (OfflineSession.IsOffline)
            Destroy(gameObject);
        else
            PhotonNetwork.Destroy(gameObject.GetComponent<PhotonView>());
    }
}
