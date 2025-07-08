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

        Vector3 currentRotation = transform.eulerAngles;


        float targetY = _target.eulerAngles.y;

        transform.rotation = Quaternion.Euler(currentRotation.x, targetY, currentRotation.z);
    }
}
