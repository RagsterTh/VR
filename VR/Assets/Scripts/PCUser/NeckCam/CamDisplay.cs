using Photon.Pun;
using UnityEngine;

public class CamDisplay : MonoBehaviour
{
    private PhotonView _phView;
    private Camera _camera;
    private int _playerNumber;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        _phView = GetComponentInParent<PhotonView>();
        _camera = GetComponentInChildren<Camera>(true);
        if (!_phView.IsMine)
            return;

        _phView.RPC("RPC_SetCamera", RpcTarget.MasterClient, _phView.ViewID);
    }
    [PunRPC]
    void RPC_SetCamera(int ID)
    {
        SimulationController.Instance.User.AddCamera(PhotonNetwork.GetPhotonView(ID).GetComponentInChildren<Camera>());
    }

}
