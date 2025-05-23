using Photon.Pun;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
public class Gun : MonoBehaviour
{
    [SerializeField] float fireSpeed = 20;
    PhotonView _phView;
    [SerializeField] Transform _gunDirection;
    IShootable _target;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Awake()
    {
        _phView = GetComponentInParent<PhotonView>();
        if (!_phView.IsMine)
        {
            this.enabled = false;
        }
    }


    void FixedUpdate()
    {
        if(!ConnectionManager.isVR)
            Aim();
    }
    void Aim()
    {
        if (Physics.Raycast(_gunDirection.position, _gunDirection.forward, out RaycastHit hit))
        {
            if(hit.collider.TryGetComponent(out IShootable target))
            {
                this._target = target;

            } else
            {
                this._target = null;
            }
        }
        else
        {
            _target = null;
        }
    }
    public void Shoot()
    {
        print("atirei"+_target);
        _target?.Hit();
    }
    public void SetTarget(HoverEnterEventArgs value)
    {
        _target = value.interactableObject.transform.GetComponent<IShootable>();
    }
    public void NullTarget()
    {
        _target = null;
    }
}
