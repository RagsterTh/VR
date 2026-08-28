using System.Collections;
using UnityEngine;
using Photon.Pun;

public class TurretEnemy : Enemy
{
    [SerializeField] Transform muzzle;
    [SerializeField] EnemyData data;
    [SerializeField] float fireRate;
    [SerializeField] float range;
    [SerializeField] float projectileForce;

    protected override void OnEnable()
    {
        base.OnEnable();
        StartCoroutine(Fire());
    }

    IEnumerator Fire()
    {
        yield return new WaitForSeconds(fireRate);
        if (followingPlayer && Vector3.Distance(transform.position, followingPlayer.position) <= range)
        {
            Vector3 direction = (followingPlayer.position - muzzle.position).normalized;
            GameObject projectile = PhotonNetwork.Instantiate(data.bullet.name, muzzle.position, Quaternion.identity);
            projectile.GetComponent<Rigidbody>().linearVelocity = direction * projectileForce;
        }
        StartCoroutine(Fire());
    }
}
