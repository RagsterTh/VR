using Photon.Pun;
using UnityEngine;

public class FollowTarget : MonoBehaviour
{
    [SerializeField] Transform _target;
    PhotonView _phView;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        _phView = GetComponentInParent<PhotonView>();
    }

    // Update is called once per frame
    void Update()
    {
        
    }
    private void LateUpdate()
    {
        if (!_phView.IsMine)
            return;
        transform.position = new Vector3(_target.position.x, transform.position.y, _target.position.z - 0.05f);
        //transform.rotation = Quaternion.Euler(0, _target.rotation.y, 0);
    }
}
