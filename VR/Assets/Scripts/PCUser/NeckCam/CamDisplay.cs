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

        _phView.RPC("RPC_SetCamera", RpcTarget.MasterClient, SimulationController.Instance.GetPlayerNumber(_phView.ControllerActorNr));
    }
    [PunRPC]
    void RPC_SetCamera(int playerIdentification)
    {
        SimulationController.Instance.User.AddCamera(_camera);
    }

}
