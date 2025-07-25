using Photon.Pun;
using UnityEngine;

public class CameraNetwork : MonoBehaviour
{
    PhotonView _phView;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Awake()
    {
        _phView = GetComponentInParent<PhotonView>();
        if (!_phView.IsMine)
        {
            gameObject.SetActive(false);
            return;
        }
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
