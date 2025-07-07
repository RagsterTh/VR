using Photon.Pun;
using UnityEngine;

public class WaitingPlayers : MonoBehaviour
{
    PhotonView _phView;
    int _playersFinish;
    int _vrPlayersAmount;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        _phView = GetComponent<PhotonView>();
        foreach (var item in PhotonNetwork.PlayerList)
        {
            if ((bool)item.CustomProperties["IsVR"])
            {
                _vrPlayersAmount++;
            }
        }
    }

    // Update is called once per frame
    void Update()
    {
        
    }
    public void Finish(string scene)
    {
        _phView.RPC("RPC_Finish", RpcTarget.MasterClient, scene);
    }
    [PunRPC]
    public void RPC_Finish(string scene)
    {
        _playersFinish++;
        if(_playersFinish >= _vrPlayersAmount)
        {
            PhotonNetwork.LoadLevel(scene);
        }
    }
}
