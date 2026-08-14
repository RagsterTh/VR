using System.Collections;
using UnityEngine;

public class FollowGun : MonoBehaviour
{
    [SerializeField] GameObject gun;
    [SerializeField] GameObject hand;
    [SerializeField] GameObject follow;
    Transform target;
    [SerializeField] bool isActive;
    [SerializeField] Vector3 handRotationOffset;
    Vector3 currentOffset = Vector3.zero;
    [SerializeField] float offset;
    private void LateUpdate()
    {
        if (target == null || !target.gameObject.activeSelf)
        {
            if (gun != null && gun.activeSelf) 
            {
                
                target = gun.transform;
                currentOffset = new Vector3(0, 0, 0);            
            }
            else if (hand != null && hand.activeSelf)
            { 
                
                target = hand.transform;
                currentOffset = handRotationOffset;
                
            }
            if (target == null) return;
        }

        follow.transform.position = target.position;
        follow.transform.rotation = target.rotation * Quaternion.Euler(currentOffset);
    }
}
