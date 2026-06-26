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
        _phView.RPC("RPC_StartBattle", RpcTarget.AllBuffered);
    }
    [PunRPC]
    public void RPC_StartBattle()
    {
        PhotonNetwork.LoadLevel("Game");
    }
}
