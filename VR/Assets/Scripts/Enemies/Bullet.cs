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
            ServiceLocator.Get<PlayersLifeBar>()?.TakeDamage(damage);
        }
        Collision();
    }
    
    // The player's damage collider is a trigger, so OnCollisionEnter never fires against it.
    void OnTriggerEnter(Collider other)
    {
        if (hasCollided || !other.CompareTag("Player"))
            return;

        hasCollided = true;
        ServiceLocator.Get<PlayersLifeBar>()?.TakeDamage(damage);
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
