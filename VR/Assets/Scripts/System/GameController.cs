using Photon.Pun;
using Photon.Realtime;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

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
    }
    // Start is called before the first frame update
    void Start()
    {
        foreach (var item in _sceneResources.resources)
        {
            _resourcesRegister.Add(item.type, item.resource);
        }
        int playerID = PhotonNetwork.Instantiate(GetResource(ResourceTypes.Player).name, _spawnPoints[Random.Range(1, _spawnPoints.Length)].position, transform.rotation).GetPhotonView().ViewID;
        if (PhotonNetwork.LocalPlayer.IsLocal)
        {
            _phView.RPC("RPC_RegisterPlayerAvatar", RpcTarget.AllBuffered, playerID);
        }
        if (!PhotonNetwork.IsMasterClient)
            return;

    }
    public static GameObject GetResource(ResourceTypes resource)
    {
        return _resourcesRegister[resource];
    }
    public void BattleBegin()
    {
        OnBattleBegin.Invoke();
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
    
}
