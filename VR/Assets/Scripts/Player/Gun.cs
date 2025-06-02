using Photon.Pun;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
public class Gun : MonoBehaviour
{
    [SerializeField] float fireSpeed = 20;
    PhotonView _phView;
    [SerializeField] Transform _gunDirection;
    IShootable _target;
    ISoundable _sound;

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
            if (hit.collider.TryGetComponent(out ISoundable sound))
            {
                this._sound = sound;

            }
            else
            {
                this._sound = null;
            }
        }
        else
        {
            _sound = null;
        }
    }
    public void Shoot()
    {
        _sound?.PlaySound();
        _target?.Hit();
    }
    public void SetTarget(HoverEnterEventArgs value)
    {
        _target = value.interactableObject.transform.GetComponent<IShootable>();
        _sound = value.interactableObject.transform.GetComponent<ISoundable>();
    }
    public void NullTarget()
    {
        _target = null;
        _sound = null;
    }
}
