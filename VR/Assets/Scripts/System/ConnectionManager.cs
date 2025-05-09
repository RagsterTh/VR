using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Photon.Pun;
using UnityEngine.SceneManagement;

public class ConnectionManager : MonoBehaviourPunCallbacks
{
     public static ConnectionManager instance;
    public static bool isVR;
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
        if (!SceneManager.GetActiveScene().name.Equals("Title"))
            Connection();
    }
    public void Connection()
    {
        PhotonNetwork.ConnectUsingSettings();
    }
    public override void OnConnectedToMaster()
    {
        PhotonNetwork.JoinLobby();
    }
    public override void OnJoinedLobby()
    {
        PhotonNetwork.JoinRandomOrCreateRoom();
    }
    public override void OnJoinedRoom()
    {
        if(SceneManager.GetActiveScene().name.Equals("Title") || SceneManager.GetActiveScene().name.Equals("LoadingScene"))
            PhotonNetwork.LoadLevel(4);
    }
}
