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
        if (!SceneManager.GetActiveScene().name.Equals("Title"))
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
        PhotonNetwork.ConnectUsingSettings();
    }
    public override void OnConnectedToMaster()
    {
        PhotonNetwork.JoinRandomOrCreateRoom();
    }
    public override void OnJoinedRoom()
    {
        
#if UNITY_EDITOR 
        if(isVR)
            if(SceneManager.GetActiveScene().name.Equals("LoadingScene"))
                PhotonNetwork.LoadLevel(1);
#endif
        
    }
    public override void OnPlayerLeftRoom(Player otherPlayer)
    {
        if(SceneManager.GetActiveScene().name.Equals("Game"))
            foreach (var item in GameController.instance.GetPlayerList())
            {
                if (item.GetPhotonView().ControllerActorNr == otherPlayer.ActorNumber)
                    GameController.instance.RemovePlayerAvatar(item.GetPhotonView().ViewID);
            }
    }
}
