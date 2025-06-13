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
        print("bateu Collision " + collision.gameObject.name);
        hasCollided = true;
        if (collision.gameObject.TryGetComponent(out PlayerStats damageble))
        {
            damageble.TakeDamage(damage);
            print("deu dano");
        }
        Collision();
    }
    private void OnTriggerEnter(Collider other)
    {
        hasCollided = true;
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
