using UnityEngine;

public class PlayerBullet : MonoBehaviour
{
    [SerializeField] float _speed;
    Rigidbody _rb;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Awake()
    {
        _rb = GetComponent<Rigidbody>();
        gameObject.SetActive(false);
    }
    private void OnEnable()
    {
        _rb.linearVelocity = -transform.up * _speed;
    }
    // Update is called once per frame
    void Update()
    {
        
    }
    private void OnTriggerEnter(Collider other)
    {
        if(other.TryGetComponent(out IShootable target))
        {
            target.Hit();
            gameObject.SetActive(false);
        }
    }
    private void OnTriggerExit(Collider other)
    {
        if(other.name.Equals("End"))
            gameObject.SetActive(false);
    }
}
