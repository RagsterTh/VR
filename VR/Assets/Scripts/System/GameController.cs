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
    [SerializeField]SceneResources _sceneResources;
    List<GameObject> _playerAvatar = new List<GameObject>();
    static Dictionary<ResourceTypes, GameObject> _resourcesRegister = new Dictionary<ResourceTypes, GameObject>();

    public List<GameObject> PlayerAvatar { get => _playerAvatar;}

    //[Header("Events")]

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
        int playerID = PhotonNetwork.Instantiate(GetResource(ResourceTypes.Player).name, _spawnPoints[Random.Range(0, _spawnPoints.Length)].position, transform.rotation).GetPhotonView().ViewID;
        if (PhotonNetwork.LocalPlayer.IsLocal)
        {
            _phView.RPC("RPC_RegistePlayerAvatar", RpcTarget.AllBuffered, playerID);
        }
        if (!PhotonNetwork.IsMasterClient)
            return;
    }
    public static GameObject GetResource(ResourceTypes resource)
    {
        return _resourcesRegister[resource];
    }
    [PunRPC]
    public void RPC_RegistePlayerAvatar(int playerID)
    {
        _playerAvatar.Add(PhotonNetwork.GetPhotonView(playerID).gameObject);
    }
    
}
