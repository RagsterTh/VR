using Photon.Pun;
using Photon.Realtime;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.SceneManagement;

public class GameController : MonoBehaviour
{
    PhotonView _phView;
    public static GameController instance;
    [SerializeField]Transform[] _spawnPoints;
    [SerializeField] ObjectPool[] _enemyPools;
    [SerializeField]SceneResources _sceneResources;
    List<GameObject> _playerAvatar = new List<GameObject>();
    static Dictionary<ResourceTypes, GameObject> _resourcesRegister = new Dictionary<ResourceTypes, GameObject>();

    public List<GameObject> PlayerAvatar { get => _playerAvatar;}

    [Header("Events")]
    public UnityEvent OnBattleBegin;

    private void Awake()
    {
        instance = this;
        _phView = GetComponent<PhotonView>();
        _resourcesRegister.Clear();
        
    }
    // Start is called before the first frame update
    IEnumerator Start()
    {
        foreach (var item in _sceneResources.resources)
        {
            _resourcesRegister.Add(item.type, item.resource);
        }
        yield return new WaitUntil(() => PhotonNetwork.InRoom);
        int playerID;
        if (ConnectionManager.isVR)
        {
            playerID = PhotonNetwork.Instantiate(GetResource(ResourceTypes.PlayerVR).name, _spawnPoints[Random.Range(1, _spawnPoints.Length)].position, transform.rotation).GetPhotonView().ViewID;
        }
        else
        {
            playerID = PhotonNetwork.Instantiate(GetResource(ResourceTypes.Player).name, _spawnPoints[Random.Range(1, _spawnPoints.Length)].position, transform.rotation).GetPhotonView().ViewID;
        }

        if (PhotonNetwork.LocalPlayer.IsLocal)
        {
            _phView.RPC("RPC_RegisterPlayerAvatar", RpcTarget.AllBuffered, playerID);
        }
        PhotonNetwork.AutomaticallySyncScene = false;

    }
    public static GameObject GetResource(ResourceTypes resource)
    {
        return _resourcesRegister[resource];
    }
    public void BattleBegin()
    {
        //OnBattleBegin.Invoke();
        PhotonNetwork.CurrentRoom.IsOpen = false;
        _phView.RPC("RPC_BattleBegin", RpcTarget.All);
    }

    //RPC's
    [PunRPC]
    public void RPC_RegisterPlayerAvatar(int playerID)
    {
        _playerAvatar.Add(PhotonNetwork.GetPhotonView(playerID).transform.GetChild(1).gameObject);
    }
    [PunRPC]
    public void RPC_BattleBegin()
    {
        OnBattleBegin.Invoke();
    }

    public List<GameObject> GetPlayerList()
    {
        return _playerAvatar;
    }

    public void BattleEnd()
    {
        if (PhotonNetwork.IsMasterClient)
            _phView.RPC("RPC_LoadMedicalQuestions", RpcTarget.All);

    }
    public void LoadMedicalScene()
    {
        PhotonNetwork.LoadLevel("MedicalQuestions");
    }
    [PunRPC]
    void RPC_LoadMedicalQuestions()
    {
        if (SceneManager.GetActiveScene().name.Equals("MedicalQuestions"))
            return;

        PhotonNetwork.LoadLevel("MedicalQuestions");
    }    
}
