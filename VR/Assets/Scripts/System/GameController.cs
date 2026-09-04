using Photon.Pun;
using Photon.Pun.Demo.PunBasics;
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
    [SerializeField] Transform[] _spawnPoints;
    [SerializeField] ObjectPool[] _enemyPools;
    [SerializeField] ObjectPool _playersBullets;
    [SerializeField] SceneResources _sceneResources;
    [SerializeField] Switch _switch;

    [Header("Difficulty")]
    [Tooltip("How much each extra player speeds up enemy spawns. 0 = spawn rate ignores player count. 1 = spawn interval is halved with 2 players, thirds with 3, etc.")]
    [SerializeField] float _difficultyFactor = 0.5f;

    List<GameObject> _playerAvatar = new List<GameObject>();
    static Dictionary<ResourceTypes, GameObject> _resourcesRegister = new Dictionary<ResourceTypes, GameObject>();

    public List<GameObject> PlayerAvatar { get => _playerAvatar; }
    public ObjectPool PlayersBullets { get => _playersBullets; }
    public float DifficultyFactor { get => _difficultyFactor; set => _difficultyFactor = value; }

    [Header("Events")]
    public UnityEvent OnBattleBegin;
    public UnityEvent OnPlayerLeftBattle;
    public UnityEvent OnSceneLoaded;

    private void Awake()
    {
        instance = this;
        _phView = GetComponent<PhotonView>();
        _resourcesRegister.Clear();
        ServiceLocator.Register(this);
    }
    // Start is called before the first frame update
    IEnumerator Start()
    {
        foreach (var item in _sceneResources.resources)
        {
            _resourcesRegister.Add(item.type, item.resource);
        }
        yield return new WaitUntil(() => PhotonNetwork.InRoom);
        if (SceneManager.GetActiveScene().name.Equals("Game"))
            if (ConnectionManager.isVR)
            {
                int playerID = PhotonNetwork.Instantiate(GetResource(ResourceTypes.PlayerVR).name, _spawnPoints[Random.Range(1, _spawnPoints.Length)].position, transform.rotation).GetPhotonView().ViewID;
                if (PhotonNetwork.LocalPlayer.IsLocal)
                {
                    _phView.RPC("RPC_RegisterPlayerAvatar", RpcTarget.AllBuffered, playerID);
                }
            }

        if (PhotonNetwork.IsMasterClient)
            _phView.RPC("RPC_ActiveScene", RpcTarget.AllBuffered);

    }
    public static GameObject GetResource(ResourceTypes resource)
    {
        return _resourcesRegister[resource];
    }
    public void BattleBegin()
    {
        //OnBattleBegin.Invoke();
        //PhotonNetwork.CurrentRoom.IsOpen = false;
        _phView.RPC("RPC_BattleBegin", RpcTarget.All);
    }
    public void RemovePlayerAvatar(int playerID)
    {
        _phView.RPC("RPC_RemovePlayerAvatar", RpcTarget.All, playerID);
    }
    public List<GameObject> GetPlayerList()
    {
        return _playerAvatar;
    }

    public void ActiveBattle()
    {
        if (!AllVRPlayersReady())
        {
            Debug.LogWarning($"[GameController] ActiveBattle blocked: {GetReadyPlayerCount()}/{CountExpectedVRPlayers()} VR players connected and ready.");
            return;
        }
        _switch.Active();
    }

    private bool AllVRPlayersReady()
    {
        int expectedVRPlayers = CountExpectedVRPlayers();
        return expectedVRPlayers > 0 && GetReadyPlayerCount() >= expectedVRPlayers;
    }

    private int GetReadyPlayerCount()
    {
        if (_playerAvatar.Count > 0)
            return _playerAvatar.Count;

        if (SimulationController.Instance != null)
            return SimulationController.Instance.PlayerAvatar.Count;

        return 0;
    }

    private int CountExpectedVRPlayers()
    {
        int expectedVRPlayers = 0;
        foreach (var player in PhotonNetwork.PlayerList)
        {
            if (player.CustomProperties.TryGetValue("IsVR", out object isVR) && (bool)isVR)
                expectedVRPlayers++;
        }
        return expectedVRPlayers;
    }

    //RPC's
    [PunRPC]
    public void RPC_RegisterPlayerAvatar(int playerID)
    {
        PhotonView playerView = PhotonNetwork.GetPhotonView(playerID);
        Camera playerCamera = playerView.GetComponentInChildren<Camera>(true);
        if (playerCamera == null)
        {
            Debug.LogWarning($"[GameController] RPC_RegisterPlayerAvatar: no Camera found under player {playerID}, falling back to root transform.");
            _playerAvatar.Add(playerView.gameObject);
            return;
        }
        _playerAvatar.Add(playerCamera.gameObject);
    }
    [PunRPC]
    public void RPC_RemovePlayerAvatar(int playerID)
    {
        foreach (var player in _playerAvatar)
        {
            PhotonView view = player.GetComponentInParent<PhotonView>();
            if (view != null && view.ViewID == playerID)
            {
                PhotonNetwork.Destroy(view.gameObject);
                _playerAvatar.Remove(player);
                break;
            }
        }
        OnPlayerLeftBattle.Invoke();
    }
    [PunRPC]
    public void RPC_BattleBegin()
    {
        OnBattleBegin.Invoke();
    }
    [PunRPC]
    public void RPC_ActiveScene()
    {
        OnSceneLoaded.Invoke();
    }

    public void BattleEnd()
    {
        if (PhotonNetwork.IsMasterClient && SceneManager.GetActiveScene().name.Equals("GloboV2"))
            PhotonNetwork.LoadLevel("MedicalQuestions");
        else
            PhotonNetwork.LoadLevel("Credits");

    }
}
