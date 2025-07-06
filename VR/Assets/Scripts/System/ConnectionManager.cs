using Photon.Pun;
using UnityEngine.SceneManagement;
using Photon.Realtime;
using ExitGames.Client.Photon;
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
        PhotonNetwork.SetPlayerCustomProperties(hash);
    }
    public void Connection()
    {
        PhotonNetwork.AutomaticallySyncScene = true;
        PhotonNetwork.ConnectUsingSettings();
    }
    public override void OnConnectedToMaster()
    {
        PhotonNetwork.JoinLobby();
    }
    public override void OnJoinedLobby()
    {
        if (!SceneManager.GetActiveScene().name.Equals("MedicalQuestions"))
            PhotonNetwork.JoinRandomOrCreateRoom();
    }
    public override void OnJoinedRoom()
    {
        /*
#if UNITY_EDITOR 
        if(isVR)
            if(SceneManager.GetActiveScene().name.Equals("LoadingScene"))
                PhotonNetwork.LoadLevel(1);
#endif
        */
        
    }
    public override void OnDisconnected(DisconnectCause cause)
    {
        SceneManager.LoadScene("LoadingScene");
    }
}
