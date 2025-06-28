using UnityEngine;
using Photon.Pun;
using UnityEngine.InputSystem;

public class Gun : MonoBehaviour
{
    PhotonView _phView;
    [SerializeField] float _bulletSpeed = 4;
    ObjectPool _playersBullets;
    Transform _gunPoint;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        _phView = GetComponentInParent<PhotonView>();
        if(!_phView.IsMine)
            enabled = false;

        _gunPoint = transform.GetChild(0);
        _playersBullets = GameController.instance.PlayersBullets;
    }

    // Update is called once per frame
    void Update()
    {
        
    }
    public void Shoot(InputAction.CallbackContext value)
    {
        if (value.performed)
        {
            _playersBullets.CallObject(_gunPoint.position, _gunPoint.rotation);
            //temp.GetComponent<Rigidbody>().linearVelocity = -transform.up * _bulletSpeed;
        }

    }
}
