using Photon.Pun;
using UnityEngine;

public class Gun : MonoBehaviour
{
    PhotonView _phView;
    [SerializeField] Transform _gunDirection;
    IShootable _target;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        _phView = GetComponentInParent<PhotonView>();
        if (!_phView.IsMine)
        {
            this.enabled = false;
        }
    }


    void FixedUpdate()
    {
        Aim();
    }
    void Aim()
    {
        if (Physics.Raycast(transform.position, _gunDirection.forward, out RaycastHit hit))
        {
            if(hit.collider.TryGetComponent(out IShootable target))
            {
                this._target = target;
            }
        }
        else
        {
            _target = null;
        }
    }
    public void Shoot()
    {
        _target.Hit();
    }
}
