using UnityEngine;
using Photon.Pun;

public class Gun : MonoBehaviour
{
    PhotonView _phView;
    [SerializeField] float _bulletSpeed = 4;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        _phView = GetComponentInParent<PhotonView>();
        if(!_phView.IsMine)
            enabled = false;
    }

    // Update is called once per frame
    void Update()
    {
        
    }
    public void OnActivate()
    {
        GameObject temp = PhotonNetwork.Instantiate("PlayerBullet", transform.GetChild(0).position, transform.rotation);
        temp.GetComponent<Rigidbody>().linearVelocity = -transform.up * _bulletSpeed;
    }
}
