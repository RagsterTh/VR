using Photon.Pun;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;
using UnityEngine.XR.Interaction.Toolkit.Interactors;
using UnityEngine.XR.Interaction.Toolkit.Samples.StarterAssets;
public class Gun : MonoBehaviour
{
    GameObject _bullet;
    [SerializeField] float fireSpeed = 20;
    PhotonView _phView;
    [SerializeField] Transform _gunDirection;
    IShootable _target;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        _bullet = GameController.GetResource(ResourceTypes.Bullet);
        _phView = GetComponentInParent<PhotonView>();
        if (!_phView.IsMine)
        {
            this.enabled = false;
        }
    }


    void FixedUpdate()
    {
        //Aim();
    }
    void Aim()
    {
        if (Physics.Raycast(_gunDirection.position, _gunDirection.forward, out RaycastHit hit))
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
    public void OnShoot()
    {
        _target?.Hit();
    }
    public void SetTarget(SelectEnterEventArgs value)
    {
        _target = value.interactableObject.transform.GetComponent<IShootable>();
        _target?.Hit();
    }
}
