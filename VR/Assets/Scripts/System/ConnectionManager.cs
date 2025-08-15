using Photon.Pun;
using UnityEngine.SceneManagement;
using Photon.Realtime;
using ExitGames.Client.Photon;
using System.Collections.Generic;
public class ConnectionManager : MonoBehaviourPunCallbacks
{
     public static ConnectionManager instance;
    public static bool isVR;
    public static int localVRNumber;
    private List<int> _vrsNumber = new List<int>();
    private PhotonView _phView;


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
        _phView = GetComponent<PhotonView>();
            Connection();
    }
    private void Start()
    {
        Hashtable hash = new Hashtable();
        hash.Add("IsVR", isVR);
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
        if (isVR)
        {
            _phView.RPC("RPC_RegisterVRNumber", RpcTarget.AllBuffered, _phView.ControllerActorNr);
        }
        /*
#if UNITY_EDITOR 
        if(isVR)
            if(SceneManager.GetActiveScene().name.Equals("LoadingScene"))
                PhotonNetwork.LoadLevel(1);
#endif
        */
        
    }
    public int GetVRNumber(int controller)
    {
        for (int i = 0; i < _vrsNumber.Count; i++)
        {
            if (_vrsNumber[i] == controller)
            {
                return i;
            }
        }
        return -1;
    }
    [PunRPC]
    public void RPC_RegisterVRNumber(int controllerNumber)
    {
        _vrsNumber.Add(controllerNumber); 
    }

    public override void OnDisconnected(DisconnectCause cause)
    {
        SceneManager.LoadScene("LoadingScene");
    }
}
