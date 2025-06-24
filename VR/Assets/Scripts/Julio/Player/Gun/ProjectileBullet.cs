using UnityEngine;
[RequireComponent(typeof(Rigidbody))]
[RequireComponent(typeof(Collider))]

public class ProjectileBullet : MonoBehaviour
{
    private void OnCollisionEnter(Collision col)
    {
        if(col.gameObject.TryGetComponent(out IShootable other))
        {
            other.Hit();
        }
        
        Destroy(gameObject);
    }
}
