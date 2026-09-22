using Photon.Pun;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.SceneManagement;

public class GameController : MonoBehaviour
{
    private PhotonView _phView;
    public static GameController instance;

    [Header("Scene setup")]
    [SerializeField] private Transform[] _spawnPoints;
    [SerializeField] private ObjectPool[] _enemyPools;
    [SerializeField] private ObjectPool _playersBullets;
    [SerializeField] private SceneResources _sceneResources;
    [SerializeField] private Switch _switch;

    [Header("Difficulty")]
    [Tooltip("How much each extra player speeds up enemy spawns. 0 = spawn rate ignores player count. 1 = spawn interval is halved with 2 players, thirds with 3, etc.")]
    [SerializeField] private float _difficultyFactor = 0.5f;

    [Header("Events")]
    public UnityEvent OnBattleBegin;
    public UnityEvent OnPlayerLeftBattle;
    public UnityEvent OnSceneLoaded;

    private readonly List<GameObject> _playerAvatar = new();
    private static readonly Dictionary<ResourceTypes, GameObject> ResourcesRegister = new();

    public List<GameObject> PlayerAvatar => _playerAvatar;
    public ObjectPool PlayersBullets => _playersBullets;
    public float DifficultyFactor { get => _difficultyFactor; set => _difficultyFactor = value; }
    public Transform[] SpawnPoints => _spawnPoints;

    private void Awake()
    {
        instance = this;
        _phView = GetComponent<PhotonView>();
        ResourcesRegister.Clear();
        ServiceLocator.Register(this);
    }

    private IEnumerator Start()
    {
        RegisterResources();

        if (OfflineSession.IsOffline)
        {
            if (SceneManager.GetActiveScene().name == OfflineSession.CombatScene)
                SpawnOfflinePlayer();

            RPC_ActiveScene();
            yield break;
        }

        yield return new WaitUntil(() => PhotonNetwork.InRoom);

        if (SceneManager.GetActiveScene().name == OfflineSession.CombatScene && ConnectionManager.isVR)
        {
            Transform spawnPoint = GetRandomCombatSpawn();
            int playerID = PhotonNetwork.Instantiate(
                GetResource(ResourceTypes.PlayerVR).name,
                spawnPoint.position,
                spawnPoint.rotation).GetPhotonView().ViewID;

            if (PhotonNetwork.LocalPlayer.IsLocal)
                _phView.RPC(nameof(RPC_RegisterPlayerAvatar), RpcTarget.AllBuffered, playerID);
        }

        if (PhotonNetwork.IsMasterClient)
            _phView.RPC(nameof(RPC_ActiveScene), RpcTarget.AllBuffered);
    }

    private void RegisterResources()
    {
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

        Transform spawnPoint = GetRandomCombatSpawn();
        GameObject player = Instantiate(playerPrefab, spawnPoint.position, spawnPoint.rotation);
        RegisterLocalPlayerAvatar(player);
    }

    private Transform GetRandomCombatSpawn()
    {
        if (_spawnPoints == null || _spawnPoints.Length == 0)
            return transform;

        int firstPlayableIndex = _spawnPoints.Length > 1 ? 1 : 0;
        return _spawnPoints[Random.Range(firstPlayableIndex, _spawnPoints.Length)];
    }

    public static GameObject GetResource(ResourceTypes resource)
    {
        if (ResourcesRegister.TryGetValue(resource, out GameObject registeredResource))
            return registeredResource;

        Debug.LogError($"[GameController] Resource {resource} is not registered.");
        return null;
    }

    public void RegisterLocalPlayerAvatar(GameObject playerRoot)
    {
        Camera playerCamera = playerRoot.GetComponentInChildren<Camera>(true);
        GameObject avatar = playerCamera != null ? playerCamera.gameObject : playerRoot;

        if (!_playerAvatar.Contains(avatar))
            _playerAvatar.Add(avatar);
    }

    public void BattleBegin()
    {
        if (OfflineSession.IsOffline)
            RPC_BattleBegin();
        else
            _phView.RPC(nameof(RPC_BattleBegin), RpcTarget.All);
    }

    public void RemovePlayerAvatar(int playerID)
    {
        if (OfflineSession.IsOffline)
        {
            if (_playerAvatar.Count > 0)
            {
                GameObject avatar = _playerAvatar[0];
                _playerAvatar.RemoveAt(0);
                Destroy(avatar.transform.root.gameObject);
            }

            OnPlayerLeftBattle?.Invoke();
            return;
        }

        _phView.RPC(nameof(RPC_RemovePlayerAvatar), RpcTarget.All, playerID);
    }

    public List<GameObject> GetPlayerList()
    {
        return _playerAvatar;
    }

    public void ActiveBattle()
    {
        if (!AllVRPlayersReady())
        {
            Debug.LogWarning($"[GameController] ActiveBattle blocked: {GetReadyPlayerCount()}/{CountExpectedVRPlayers()} VR players ready.");
            return;
        }

        _switch.Active();
    }

    private bool AllVRPlayersReady()
    {
        int expectedVRPlayers = CountExpectedVRPlayers();
        return expectedVRPlayers > 0 && GetReadyPlayerCount() >= expectedVRPlayers;
    }

    public int GetReadyPlayerCount()
    {
        if (_playerAvatar.Count > 0)
            return _playerAvatar.Count;

        if (SimulationController.Instance != null)
            return SimulationController.Instance.PlayerAvatar.Count;

        return 0;
    }

    private int CountExpectedVRPlayers()
    {
        if (OfflineSession.IsOffline)
            return 1;

        int expectedVRPlayers = 0;
        foreach (var player in PhotonNetwork.PlayerList)
        {
            if (player.CustomProperties.TryGetValue("IsVR", out object isVR) && (bool)isVR)
                expectedVRPlayers++;
        }

        return expectedVRPlayers;
    }

    [PunRPC]
    public void RPC_RegisterPlayerAvatar(int playerID)
    {
        PhotonView playerView = PhotonNetwork.GetPhotonView(playerID);
        if (playerView != null)
            RegisterLocalPlayerAvatar(playerView.gameObject);
    }

    [PunRPC]
    public void RPC_RemovePlayerAvatar(int playerID)
    {
        for (int i = _playerAvatar.Count - 1; i >= 0; i--)
        {
            PhotonView view = _playerAvatar[i].GetComponentInParent<PhotonView>();
            if (view == null || view.ViewID != playerID)
                continue;

            _playerAvatar.RemoveAt(i);
            PhotonNetwork.Destroy(view.gameObject);
            break;
        }

        OnPlayerLeftBattle?.Invoke();
    }

    [PunRPC]
    public void RPC_BattleBegin()
    {
        OnBattleBegin?.Invoke();
    }

    [PunRPC]
    public void RPC_ActiveScene()
    {
        OnSceneLoaded?.Invoke();
    }

    public void BattleEnd()
    {
        if (OfflineSession.IsOffline)
        {
            OfflineSession.LoadAfterBattle();
            return;
        }

        if (PhotonNetwork.IsMasterClient && SceneManager.GetActiveScene().name == OfflineSession.FullExperienceScene)
            PhotonNetwork.LoadLevel(OfflineSession.MedicalScene);
        else
            PhotonNetwork.LoadLevel(OfflineSession.CreditsScene);
    }
}
