using System.Collections;
using UnityEngine;
using Photon.Pun;

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
        Collision();
    }
    private void OnTriggerEnter(Collider other)
    {
        hasCollided = true;
        if (other.TryGetComponent(out IDamageable damageble))
        {
            damageble.TakeDamage(damage);
            print("deu dano");
        }
        Collision();
    }
    
    bool Collision()    
    {
        return hasCollided;
    }

    IEnumerator DestroyBullet()
    {
        print("if Collide");
        yield return new WaitUntil(Collision);
        print("Collide true");
        PhotonNetwork.Destroy(gameObject.GetComponent<PhotonView>());
    }
}
