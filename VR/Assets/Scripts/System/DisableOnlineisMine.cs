using UnityEngine;
using Photon.Pun;
public class DisableOnlineisMine : MonoBehaviour
{
    PhotonView _phView;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        _phView = GetComponentInParent<PhotonView>();
        if (_phView.IsMine)
        {
            gameObject.SetActive(false);
        }
    }
}
