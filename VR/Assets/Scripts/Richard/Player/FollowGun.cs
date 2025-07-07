using System.Collections;
using UnityEngine;

public class FollowGun : MonoBehaviour
{
    [SerializeField] GameObject gun;
    [SerializeField] GameObject hand;
    [SerializeField] GameObject follow;
    Transform target;
    [SerializeField] bool isActive;

    [SerializeField] float offset;
    private void Start()
    {
        if (gun.activeSelf)
            target = gun.transform;
        if(hand.activeSelf)
            target = hand.transform;
    }
    private void LateUpdate()
    {
        if (target == null)
            return;

        follow.transform.position = target.position;
        follow.transform.rotation = new Quaternion(target.rotation.x, target.rotation.y, target.rotation.z, target.rotation.w);
    }
}
