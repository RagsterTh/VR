using Photon.Pun;
using UnityEngine.SceneManagement;
using Photon.Realtime;
using ExitGames.Client.Photon;
using System.Collections.Generic;
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
            Connection();
    }
    private void Start()
    {
        Hashtable hash = new Hashtable();
        hash.Add("IsVR", isVR);

        if (!PhotonNetwork.IsMasterClient)
            hash.Add("VRNumber", -1);

        PhotonNetwork.SetPlayerCustomProperties(hash);
    }
    public void Connection()
    {
        PhotonNetwork.AutomaticallySyncScene = true;
        PhotonNetwork.ConnectUsingSettings();
    }
    public override void OnConnectedToMaster()
    {
        PhotonNetwork.JoinRandomOrCreateRoom();
    }
    public override void OnJoinedRoom()
    {
        /*
        if (isVR)
        {
            _phView.RPC("RPC_RegisterVRNumber", RpcTarget.AllBuffered, PhotonNetwork.LocalPlayer);
        }
                /*
#if UNITY_EDITOR 
        if(isVR)
            if(SceneManager.GetActiveScene().name.Equals("LoadingScene"))
                PhotonNetwork.LoadLevel(1);
#endif
        */
        
    }
    public override void OnPlayerEnteredRoom(Player newPlayer)
    {//Não funcionou, preciso testar outro meio de identificação
        //_phView.RPC("RPC_RegisterVRNumber", RpcTarget.AllBuffered, newPlayer.ActorNumber);
    }

    public override void OnDisconnected(DisconnectCause cause)
    {
        SceneManager.LoadScene("LoadingScene");
        Connection();
    }
}
