using System.Collections;
using UnityEngine;

public class FollowGun : MonoBehaviour
{
    [SerializeField] GameObject gun;
    [SerializeField] GameObject follow;

    [SerializeField] bool isActive;

    [SerializeField] float offset;
    private void Start()
    {
        StartCoroutine(Follow());
    }

    IEnumerator Follow()
    {
        yield return new WaitForSeconds(.1f);
        if (isActive)
        {
            follow.transform.position = gun.transform.position;
            follow.transform.rotation = new Quaternion(gun.transform.rotation.x, gun.transform.rotation.y, gun.transform.rotation.z, gun.transform.rotation.w);
        }
        StartCoroutine(Follow());
    }
}
