using UnityEngine;
using Photon.Pun;
using UnityEngine.InputSystem;

public class Gun : MonoBehaviour
{
    PhotonView _phView;
    [SerializeField] float _bulletSpeed = 4;
    ObjectPool _playersBullets;
    Transform _gunPoint;
    ISoundable _soundEmitter;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        _phView = GetComponentInParent<PhotonView>();
        if (!OfflineSession.IsOffline && !_phView.IsMine)
        {
            enabled = false;
            return;
        }

        _gunPoint = transform.GetChild(0);
        if (GameController.instance != null)
            _playersBullets = GameController.instance.PlayersBullets;
        _soundEmitter = GetComponent<ISoundable>();
    }

    // Update is called once per frame
    void Update()
    {
        
    }
    public void Shoot(InputAction.CallbackContext value)
    {
        if (value.performed)
        {
            if (_playersBullets == null)
                return;

            _playersBullets.CallObject(_gunPoint.position, _gunPoint.rotation);
            _soundEmitter?.PlaySound();
            //temp.GetComponent<Rigidbody>().linearVelocity = -transform.up * _bulletSpeed;
        }

    }
}
