using UnityEngine;
using Photon.Pun;

public class PlayerBullet : MonoBehaviour
{
    [SerializeField] float _speed;
    [SerializeField] float _maxLifetime = 8f;
    [SerializeField] GameObject _hitEffect;
    Rigidbody _rb;
    float _spawnTime;
    Gun _shooter;

    public void SetShooter(Gun shooter) => _shooter = shooter;

    void Awake()
    {
        _rb = GetComponent<Rigidbody>();
        gameObject.SetActive(false);
    }
    private void OnEnable()
    {
        _rb.linearVelocity = -transform.up * _speed;
        _spawnTime = Time.time;
    }

    private void OnDisable()
    {
        _shooter = null;
    }
    void Update()
    {
        if (Time.time - _spawnTime > _maxLifetime)
            gameObject.SetActive(false);
    }
    private void OnTriggerEnter(Collider other)
    {
        if(other.TryGetComponent(out IShootable target))
        {
            target?.Hit();
            if (OfflineSession.IsOffline && _shooter != null)
                _shooter.ConfirmHit();
            if (OfflineSession.IsOffline)
                OfflineCombatFeedback.BulletImpact(transform.position, _rb.linearVelocity,
                    other.GetComponentInParent<Enemy>(true) != null || other.GetComponentInParent<Boss>(true) != null);

            if (_hitEffect != null)
            {
                if (OfflineSession.IsOffline)
                    Instantiate(_hitEffect, other.ClosestPoint(transform.position), transform.rotation);
                else if (PhotonNetwork.IsMasterClient)
                    PhotonNetwork.Instantiate(_hitEffect.name, other.ClosestPoint(transform.position), transform.rotation);
            }

            gameObject.SetActive(false);
        }
    }
    private void OnTriggerExit(Collider other)
    {
        if(other.name.Equals("End"))
            gameObject.SetActive(false);
    }
}
