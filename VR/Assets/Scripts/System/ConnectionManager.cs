using ExitGames.Client.Photon;
using Photon.Pun;
using Photon.Realtime;
using UnityEngine.SceneManagement;

public class ConnectionManager : MonoBehaviourPunCallbacks
{
    public static ConnectionManager instance;
    public static bool isVR;

    private void Awake()
    {
        if (OfflineSession.IsOffline)
        {
            Destroy(gameObject);
            return;
        }

        if (instance)
        {
            Destroy(gameObject);
        }
        else
        {
            instance = this;
            DontDestroyOnLoad(gameObject);
        }
        Connection();
    }

    private void Start()
    {
        if (OfflineSession.IsOffline)
            return;

        Hashtable properties = new Hashtable
        {
            { "IsVR", isVR }
        };

        if (!PhotonNetwork.IsMasterClient)
            properties.Add("VRNumber", -1);

        PhotonNetwork.SetPlayerCustomProperties(properties);
    }

    public void Connection()
    {
        if (OfflineSession.IsOffline)
            return;

        PhotonNetwork.AutomaticallySyncScene = true;
        PhotonNetwork.ConnectUsingSettings();
    }

    public override void OnConnectedToMaster()
    {
        if (!OfflineSession.IsOffline)
            PhotonNetwork.JoinRandomOrCreateRoom();
    }

    public override void OnJoinedRoom()
    {
    }

    public override void OnPlayerEnteredRoom(Player newPlayer)
    {
    }

    public override void OnDisconnected(DisconnectCause cause)
    {
        if (OfflineSession.IsOffline)
            return;

        SceneManager.LoadScene("LoadingScene");
        Connection();
    }
}
