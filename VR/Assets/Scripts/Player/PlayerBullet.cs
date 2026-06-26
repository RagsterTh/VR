using UnityEngine;

public class PlayerBullet : MonoBehaviour
{
    [SerializeField] float _speed;
    [SerializeField] float _maxLifetime = 8f;
    Rigidbody _rb;
    float _spawnTime;

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
            gameObject.SetActive(false);
        }
    }
    private void OnTriggerExit(Collider other)
    {
        if(other.name.Equals("End"))
            gameObject.SetActive(false);
    }
}
