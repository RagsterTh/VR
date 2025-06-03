using System.Collections;
using UnityEngine;
using Photon.Pun;


public class FlyingEnemy : MovingEnemy
{
    [SerializeField] Transform muzzle;
    [SerializeField] EnemyData data;

    [SerializeField] float stopingDistance;

    [SerializeField] float fireRate;

    [SerializeField] bool isInRange;

    [SerializeField] float projectileForce;

    [SerializeField] float projectileDestroyTime;

    void Start()
    {
        agent.stoppingDistance = stopingDistance;
        agent.speed = data.movimentVelocity;
        float value = Random.Range(2, 5);
        agent.height = value;
        agent.baseOffset = value;
        StartCoroutine(IsInRange());
        StartCoroutine(Fire());
    }

    IEnumerator IsInRange()
    {
        yield return new WaitForSeconds(0.2f);
        if (agent.remainingDistance <= agent.stoppingDistance)
        {
            isInRange = true;            
        }
        else
        {
            isInRange = false;
        }
        StartCoroutine(IsInRange());
    }


    IEnumerator Fire()
    {
        yield return new WaitForSeconds(fireRate);
        if (isInRange)
        {
            Vector3 direction = new Vector3 (agent.destination.x, agent.destination.y + 2.5f, agent.destination.z) - muzzle.transform.position;
            direction = direction.normalized;
            Physics.Linecast(muzzle.position, agent.destination);
            Debug.DrawLine(transform.position, agent.destination);
            GameObject projectile = PhotonNetwork.Instantiate(data.bullet.name, muzzle.position, Quaternion.identity);
            projectile.GetComponent<Rigidbody>().linearVelocity = direction * projectileForce;
            StartCoroutine(DestroyBullet(projectile));
        }
        StartCoroutine(Fire());
    }

    IEnumerator DestroyBullet(GameObject projectile)
    {
        yield return new WaitForSeconds(projectileDestroyTime);
        PhotonNetwork.Destroy(projectile.GetComponent<PhotonView>());
    }
}
