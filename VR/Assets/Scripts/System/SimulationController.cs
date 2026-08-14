using Photon.Pun;
using System.Collections;
using System.Collections.Generic;
using Unity.XR.CoreUtils;
using UnityEngine;
using UnityEngine.Events;

public enum SimulationMode
{
    Default, Shoot
}
public class SimulationController : MonoBehaviour
{
    public static SimulationController Instance { get; private set; }
    [SerializeField] private GameObject[] _simulationSectors;
    [SerializeField] private UserCam _user;
    PhotonView _phView;
    [SerializeField] GameObject[] _lobbies;
    [SerializeField] Transform[] _spawnPoints;
    [SerializeField] SceneResources _sceneResources;
    static Dictionary<ResourceTypes, GameObject> _resourcesRegister = new Dictionary<ResourceTypes, GameObject>();
    [SerializeField]public UnityEvent OnExperienceBegin;
    [SerializeField]public UnityEvent OnShootGameBegins;
    List<GameObject> _playerAvatar = new List<GameObject>();
    public List<GameObject> PlayerAvatar { get => _playerAvatar; }

    public UserCam User { get => _user; }
    public Transform[] SpawnPoints { get => _spawnPoints;}

    private void Awake()
    {
        Instance = this;
        _phView = GetComponent<PhotonView>();
        if (!PhotonNetwork.IsMasterClient)
            return;

        SetSimulationSector((int)UserCam.simulationMode);
    }
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    IEnumerator Start()
    {
          //  Application.Quit();
        _resourcesRegister.Clear();
        foreach (var item in _sceneResources.resources) 
        {
            _resourcesRegister.Add(item.type, item.resource);
        }
        yield return new WaitUntil(() => PhotonNetwork.InRoom);
       
        if (ConnectionManager.isVR)
        {
            //int vrNumber = (int)PhotonNetwork.LocalPlayer.CustomProperties["VRNumber"];
            //vrNumber = vrNumber == -1 ? 0 : vrNumber;
            int playerID = PhotonNetwork.Instantiate(GetResource(ResourceTypes.PlayerVR).name, _spawnPoints[1].position, Quaternion.LookRotation(_spawnPoints[0].up)).GetPhotonView().ViewID;
            if (PhotonNetwork.LocalPlayer.IsLocal)
            {
                _phView.RPC("RPC_RegisterPlayerAvatar", RpcTarget.AllBuffered, playerID);
            }
        }
        yield return new WaitForSeconds(2);
        /*
        if (PhotonNetwork.IsMasterClient)
            _phView.RPC("RPC_ActiveScene", RpcTarget.AllBuffered);
        */
    }
    public static GameObject GetResource(ResourceTypes resource)
    {
        return _resourcesRegister[resource];
    }

    // Update is called once per frame
    void Update()
    {
        
    }
    public void ActiveShootGame()
    {
        OnShootGameBegins.Invoke();
        //_phView.RPC("RPC_ActiveShootGame", RpcTarget.AllBuffered);
    }
    public void ActiveScene()
    {
        _phView.RPC("RPC_ActiveScene", RpcTarget.AllBuffered);
    }
    [PunRPC]
    public void RPC_ActiveShootGame()
    {
        OnShootGameBegins.Invoke();
    }
    [PunRPC]
    public void RPC_ActiveScene()
    {
        OnExperienceBegin?.Invoke();
    }
    [PunRPC]
    public void RPC_RegisterPlayerAvatar(int playerID)
    {

        GameObject player = PhotonNetwork.GetPhotonView(playerID).GetComponentInChildren<Camera>(true).gameObject;
        _playerAvatar.Add(player);
    }
    public int GetPlayerNumber(int playerController)
    {
        foreach (var player in _playerAvatar)
        {
            if(player.GetPhotonView().ControllerActorNr == playerController)
            {
                return _playerAvatar.IndexOf(player);
            }
        }
        return 100;
    }
    public void SetSimulationSector(int mode)
    {
        _phView.RPC("RPC_SetSimulationSector", RpcTarget.AllBuffered, mode);
    }
    [PunRPC]
    public void RPC_SetSimulationSector(int mode)
    {
        if (mode.Equals(SimulationMode.Default))
            return;

        for (int i = 0; i < _simulationSectors.Length; i++)
        {
            _simulationSectors[i].SetActive(i == mode);
        }
    }
}
