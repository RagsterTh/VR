using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Photon.Pun;

public class ConnectionManager : MonoBehaviourPunCallbacks
{
     public static ConnectionManager instance;
    private void Awake()
    {
        if (instance)
        {
            Destroy(gameObject);
        } else
        {
            instance = this;
            DontDestroyOnLoad(gameObject);
        }
        PhotonNetwork.ConnectUsingSettings();
    }
    public override void OnConnectedToMaster()
    {
        PhotonNetwork.JoinLobby();
    }
    public override void OnJoinedLobby()
    {

    }
    public override void OnJoinedRoom()
    {
        PhotonNetwork.LoadLevel(1);
    }



    //Funções para incorporar no menu
    public void EnterRandomOrCreateRoom()
    {
        PhotonNetwork.JoinRandomOrCreateRoom();
    }
}
