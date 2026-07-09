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

    bool hasDisable;

    void Start()
    {
        StartCoroutine(FollowPlayer(agent));
        agent.stoppingDistance = stopingDistance;
        agent.speed = data.movimentVelocity;
        float value = Random.Range(2, 5);
        agent.height = value;
        agent.baseOffset = value;
        StartCoroutine(IsInRange());
        StartCoroutine(Fire());
    }

    new void OnEnable()
    {
        base.OnEnable();
        if (hasDisable)
        {
            StartCoroutine(FollowPlayer(agent));
            float value = Random.Range(2, 5);
            fireRate = value;
            agent.height = value;
            agent.baseOffset = value;
            StartCoroutine(IsInRange());
            StartCoroutine(Fire());
        }        
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
        if (isInRange && followingPlayer != null)
        {
            Vector3 targetPos = followingPlayer.position + Vector3.up * 1.5f;
            Vector3 direction = (targetPos - muzzle.position).normalized;
            GameObject projectile = PhotonNetwork.Instantiate(data.bullet.name, muzzle.position, Quaternion.identity);
            projectile.GetComponent<Rigidbody>().linearVelocity = direction * projectileForce;
        }
        StartCoroutine(Fire());
    }

    public override void Hit()
    {
        base.Hit();
        hasDisable = true;
    }
}
