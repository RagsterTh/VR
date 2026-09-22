using UnityEngine;
using Photon.Pun;
using System.Collections;
public class DisableOnlineisMine : MonoBehaviour
{
    PhotonView _phView;
    [SerializeField] PhotonView myPhView;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
{
    _phView = myPhView != null
        ? myPhView
        : GetComponentInParent<PhotonView>();

    if (_phView == null)
    {
        Debug.LogError("PhotonView não encontrado.", this);
        return;
    }

    if (OfflineSession.IsOffline || _phView.IsMine)
    {
        gameObject.SetActive(false);
    }
}
}
