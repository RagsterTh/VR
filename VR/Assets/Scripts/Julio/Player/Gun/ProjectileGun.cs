using UnityEngine;

public class ProjectileGun : MonoBehaviour
{
    [SerializeField] Transform shootPoint;
    [SerializeField] GameObject bullet;
    [SerializeField] float bulletSpeed = 20f;

    private void Update()
    {
        if(Input.GetButton("Fire 1")) 
        {
            Shoot();    
        }
    }

    private void Shoot()
    {
        GameObject bul = Instantiate(bullet, shootPoint.position, Quaternion.identity);
        Rigidbody body = bul.GetComponent<Rigidbody>();

        body.linearVelocity = bulletSpeed * shootPoint.forward;
    }
}
