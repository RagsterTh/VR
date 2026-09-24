using Photon.Pun;
using UnityEngine;

public class LocationConfirm : MonoBehaviour
{
    [SerializeField] GameObject _confirmPanel;
    PhotonView _phView;

    private void Start()
    {
        _phView = GetComponent<PhotonView>();
    }
    public void Back()
    {
        _confirmPanel.SetActive(false);
    }
    public void Confirm()
    {
        if (OfflineSession.IsOffline)
            RPC_StartBattle();
        else
            _phView.RPC(nameof(RPC_StartBattle), RpcTarget.AllBuffered);
    }
    [PunRPC]
    public void RPC_StartBattle()
    {
        if (OfflineSession.IsOffline)
            OfflineSession.LoadMappedScene("Game");
        else
            PhotonNetwork.LoadLevel("Game");
    }
}
