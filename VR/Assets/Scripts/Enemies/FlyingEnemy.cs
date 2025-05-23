using System.Collections;
using UnityEngine;
using Photon.Pun;


public class FlyingEnemy : MovingEnemy
{
    [SerializeField] Transform muzzle;
    [SerializeField] EnemyData data;

    [SerializeField] float stopingDistance;

    void Start()
    {
        agent.stoppingDistance = stopingDistance;
        agent.speed = data.movimentVelocity;
        float value = Random.Range(2, 5);
        agent.height = value;
        agent.baseOffset = value;
        StartCoroutine(IsInRange());
    }

    IEnumerator IsInRange()
    {
        yield return new WaitForSeconds(1f);
        if (agent.pathPending)
        {
            print("Paro");
            Fire();
        }
        StartCoroutine(IsInRange());
    }


    void Fire()
    {
        GameObject a = Instantiate(data.bullet, muzzle.position, Quaternion.identity);
        a.GetComponent<Rigidbody>().AddForce(muzzle.forward);
    }
}
