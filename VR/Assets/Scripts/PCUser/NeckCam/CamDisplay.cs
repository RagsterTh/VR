using Photon.Pun;
using UnityEngine;

public class CamDisplay : MonoBehaviour
{
    private PhotonView _phView;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        _phView = GetComponentInParent<PhotonView>();
        if (!_phView.IsMine)
            return;
        GetComponent<Camera>().targetDisplay = PhotonNetwork.LocalPlayer.ActorNumber + 2;
    }

}
