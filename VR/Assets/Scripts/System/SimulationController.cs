using Photon.Pun;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

public enum SimulationMode
{
    Default,
    Shoot
}

public class SimulationController : MonoBehaviour
{
    public static SimulationController Instance { get; private set; }

    [Header("Scene setup")]
    [SerializeField] private GameObject[] _simulationSectors;
    [SerializeField] private UserCam _user;
    [SerializeField] private GameObject[] _lobbies;
    [SerializeField] private Transform[] _spawnPoints;
    [SerializeField] private SceneResources _sceneResources;

    [Header("Events")]
    [SerializeField] public UnityEvent OnExperienceBegin;
    [SerializeField] public UnityEvent OnShootGameBegins;

    private PhotonView _phView;
    private readonly List<GameObject> _playerAvatar = new();
    private static readonly Dictionary<ResourceTypes, GameObject> ResourcesRegister = new();

    public List<GameObject> PlayerAvatar => _playerAvatar;
    public UserCam User => _user;
    public Transform[] SpawnPoints => _spawnPoints;

    private void Awake()
    {
        Instance = this;
        _phView = GetComponent<PhotonView>();

        if (OfflineSession.IsOffline)
        {
            RPC_SetSimulationSector((int)UserCam.simulationMode);
            return;
        }

        if (PhotonNetwork.IsMasterClient)
            SetSimulationSector((int)UserCam.simulationMode);
    }

    private IEnumerator Start()
    {
        RegisterResources();

        if (OfflineSession.IsOffline)
        {
            SpawnOfflinePlayer();
            yield return new WaitForSeconds(2f);
            RPC_ActiveScene();
            yield break;
        }

        yield return new WaitUntil(() => PhotonNetwork.InRoom);

        if (ConnectionManager.isVR)
        {
            int spawnIndex = (PhotonNetwork.LocalPlayer.ActorNumber - 1) % _spawnPoints.Length;
            int playerID = PhotonNetwork.Instantiate(
                GetResource(ResourceTypes.PlayerVR).name,
                _spawnPoints[spawnIndex].position,
                Quaternion.LookRotation(_spawnPoints[0].up)).GetPhotonView().ViewID;

            if (PhotonNetwork.LocalPlayer.IsLocal)
                _phView.RPC(nameof(RPC_RegisterPlayerAvatar), RpcTarget.AllBuffered, playerID);
        }

        yield return new WaitForSeconds(2f);
        if (PhotonNetwork.IsMasterClient)
            _phView.RPC(nameof(RPC_ActiveScene), RpcTarget.AllBuffered);
    }

    private void RegisterResources()
    {
        ResourcesRegister.Clear();
        if (_sceneResources == null)
            return;

        foreach (var item in _sceneResources.resources)
            ResourcesRegister[item.type] = item.resource;
    }

    private void SpawnOfflinePlayer()
    {
        GameObject playerPrefab = GetResource(ResourceTypes.PlayerVR);
        if (playerPrefab == null)
            return;

        Transform spawnPoint = _spawnPoints != null && _spawnPoints.Length > 0 ? _spawnPoints[0] : transform;
        // Same facing as the online spawn (LookRotation of the first spawn point's up axis).
        Quaternion rotation = Quaternion.LookRotation(spawnPoint.up);
        GameObject player = Instantiate(playerPrefab, spawnPoint.position, rotation);
        OfflinePlayerRig.Attach(player, spawnPoint.position, rotation * Vector3.forward);
        RegisterLocalPlayerAvatar(player);
    }

    public static GameObject GetResource(ResourceTypes resource)
    {
        if (ResourcesRegister.TryGetValue(resource, out GameObject registeredResource))
            return registeredResource;

        Debug.LogError($"[SimulationController] Resource {resource} is not registered.");
        return null;
    }

    public void RegisterLocalPlayerAvatar(GameObject playerRoot)
    {
        Camera playerCamera = playerRoot.GetComponentInChildren<Camera>(true);
        GameObject avatar = playerCamera != null ? playerCamera.gameObject : playerRoot;

        if (!_playerAvatar.Contains(avatar))
            _playerAvatar.Add(avatar);
    }

    public void ActiveShootGame()
    {
        OnShootGameBegins?.Invoke();
    }

    public void ActiveScene()
    {
        if (OfflineSession.IsOffline)
            RPC_ActiveScene();
        else
            _phView.RPC(nameof(RPC_ActiveScene), RpcTarget.AllBuffered);
    }

    [PunRPC]
    public void RPC_ActiveShootGame()
    {
        OnShootGameBegins?.Invoke();
    }

    [PunRPC]
    public void RPC_ActiveScene()
    {
        OnExperienceBegin?.Invoke();
    }

    [PunRPC]
    public void RPC_RegisterPlayerAvatar(int playerID)
    {
        PhotonView playerView = PhotonNetwork.GetPhotonView(playerID);
        if (playerView != null)
            RegisterLocalPlayerAvatar(playerView.gameObject);
    }

    public int GetPlayerNumber(int playerController)
    {
        if (OfflineSession.IsOffline)
            return _playerAvatar.Count > 0 ? 0 : 100;

        foreach (var player in _playerAvatar)
        {
            PhotonView playerView = player.GetComponentInParent<PhotonView>();
            if (playerView != null && playerView.ControllerActorNr == playerController)
                return _playerAvatar.IndexOf(player);
        }

        return 100;
    }

    public void SetSimulationSector(int mode)
    {
        if (OfflineSession.IsOffline)
            RPC_SetSimulationSector(mode);
        else
            _phView.RPC(nameof(RPC_SetSimulationSector), RpcTarget.AllBuffered, mode);
    }

    [PunRPC]
    public void RPC_SetSimulationSector(int mode)
    {
        if (mode.Equals(SimulationMode.Default))
            return;

        for (int i = 0; i < _simulationSectors.Length; i++)
            _simulationSectors[i].SetActive(i == mode);
    }
}
