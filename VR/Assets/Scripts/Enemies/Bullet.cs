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
    
    bool Collision()    
    {
        return hasCollided;
    }

    IEnumerator DestroyBullet()
    {
        yield return new WaitUntil(Collision);
        PhotonNetwork.Destroy(gameObject.GetComponent<PhotonView>());
    }
}
