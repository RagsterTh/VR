using Photon.Pun;
using UnityEngine;
using UnityEngine.SceneManagement;

public enum PlayerTool
{
    Hand,
    Gun
}

public class PlayerPrefabNetwork : MonoBehaviour
{
    [Header("Local player objects")]
    [SerializeField] private GameObject _playerLifebar;
    [SerializeField] private GameObject[] _elements;

    [Header("Tools")]
    [SerializeField] private GameObject _leftGun;
    [SerializeField] private GameObject _rightGun;
    [SerializeField] private GameObject _leftHandMecanic;
    [SerializeField] private GameObject _leftHandVisual;
    [SerializeField] private GameObject _rightHandMecanic;
    [SerializeField] private GameObject _rightHandVisual;

    private PhotonView _phView;

    private bool IsLocalPlayer => OfflineSession.IsOffline || (_phView != null && _phView.IsMine);

    private void Awake()
    {
        _phView = GetComponent<PhotonView>();
        if (!IsLocalPlayer)
            return;

        foreach (var item in _elements)
        {
            if (item != null)
                item.SetActive(true);
        }

        PlayerTool startingTool = SceneManager.GetActiveScene().name == OfflineSession.CombatScene
            ? PlayerTool.Gun
            : PlayerTool.Hand;
        SetHands(startingTool);

        if (SimulationController.Instance != null)
        {
            SimulationController.Instance.OnShootGameBegins.AddListener(
                () => SetHands(PlayerTool.Gun));
        }
    }

    private void Start()
    {
        var lifeBarService = ServiceLocator.Get<PlayersLifeBar>();
        if (lifeBarService == null || _playerLifebar == null)
            return;

        var lifeBars = new System.Collections.Generic.List<GameObject>(lifeBarService.LifeBar);
        if (!lifeBars.Contains(_playerLifebar))
            lifeBars.Add(_playerLifebar);
        lifeBarService.LifeBar = lifeBars.ToArray();
    }

    public void TakeDamage(float amount)
    {
        if (OfflineSession.IsOffline)
            RPC_TakeDamage(amount);
        else
            _phView.RPC(nameof(RPC_TakeDamage), RpcTarget.All, amount);
    }

    [PunRPC]
    public void RPC_TakeDamage(float amount)
    {
        PlayersLifeBar lifeBar = GetComponentInChildren<PlayersLifeBar>();
        if (lifeBar != null)
            lifeBar.ApplyDamage(amount);
        else
            Debug.LogWarning($"[PlayerPrefabNetwork] {name}: no PlayersLifeBar found in children.");
    }

    private void SetHands(PlayerTool tool)
    {
        if (OfflineSession.IsOffline)
            RPC_Hands((int)tool);
        else
            _phView.RPC(nameof(RPC_Hands), RpcTarget.AllBuffered, (int)tool);
    }

    [PunRPC]
    private void RPC_Hands(int tool)
    {
        bool useHands = tool == (int)PlayerTool.Hand;
        _leftGun.SetActive(!useHands);
        _rightGun.SetActive(!useHands);
        _leftHandMecanic.SetActive(useHands);
        _leftHandVisual.SetActive(useHands);
        _rightHandMecanic.SetActive(useHands);
        _rightHandVisual.SetActive(useHands);
    }

    public void RecenterPlayer()
    {
        if (OfflineSession.IsOffline)
        {
            Transform[] offlineSpawns = SimulationController.Instance != null
                ? SimulationController.Instance.SpawnPoints
                : GameController.instance != null ? GameController.instance.SpawnPoints : null;

            if (offlineSpawns != null && offlineSpawns.Length > 0)
                transform.SetPositionAndRotation(offlineSpawns[0].position, offlineSpawns[0].rotation);
            return;
        }

        Transform[] spawnPoints = SimulationController.Instance.SpawnPoints;
        int spawnIndex = (PhotonNetwork.LocalPlayer.ActorNumber - 1) % spawnPoints.Length;
        transform.position = spawnPoints[spawnIndex].position;
    }
}
