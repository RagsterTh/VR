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
    private void LateUpdate()
    {
        if (target == null || !target.gameObject.activeSelf)
        {
            if (gun != null && gun.activeSelf) target = gun.transform;
            else if (hand != null && hand.activeSelf) target = hand.transform;
            if (target == null) return;
        }

        follow.transform.position = target.position;
        follow.transform.rotation = target.rotation;
    }
}
