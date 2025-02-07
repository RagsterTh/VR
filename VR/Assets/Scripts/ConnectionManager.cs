using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Photon.Pun;
public class ConnectionManager : MonoBehaviourPunCallbacks
{
     public static ConnectionManager instance;
     GameController gameController;
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
    private void Start()
    {
        gameController= GetComponent<GameController>();
    }
    public override void OnConnectedToMaster()
    {
        PhotonNetwork.JoinLobby();
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
    private void Update()
    {
        if (Input.GetButtonDown("Fire1"))
        {
            EnterRandomOrCreateRoom();
        }
    }
}
