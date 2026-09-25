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
    [Tooltip("Life bar + heal HUD, shown only while the guns are out (combat).")]
    [SerializeField] private GameObject _combatHud;

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

        bool isCombatScene = OfflineSession.IsOffline
            ? OfflineSession.IsCombatScene
            : SceneManager.GetActiveScene().name == "Game";
        PlayerTool startingTool = isCombatScene ? PlayerTool.Gun : PlayerTool.Hand;
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

        var lifeBars = lifeBarService.LifeBar != null
            ? new System.Collections.Generic.List<GameObject>(lifeBarService.LifeBar)
            : new System.Collections.Generic.List<GameObject>();
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

    public void Heal(float amount)
    {
        if (OfflineSession.IsOffline)
            RPC_Heal(amount);
        else
            _phView.RPC(nameof(RPC_Heal), RpcTarget.All, amount);
    }

    [PunRPC]
    public void RPC_Heal(float amount)
    {
        PlayersLifeBar lifeBar = GetComponentInChildren<PlayersLifeBar>();
        if (lifeBar != null)
            lifeBar.ApplyHeal(amount);
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
        SetActiveIfAssigned(_leftGun, !useHands);
        SetActiveIfAssigned(_rightGun, !useHands);
        SetActiveIfAssigned(_leftHandMecanic, useHands);
        SetActiveIfAssigned(_leftHandVisual, useHands);
        SetActiveIfAssigned(_rightHandMecanic, useHands);
        SetActiveIfAssigned(_rightHandVisual, useHands);
        SetActiveIfAssigned(_combatHud, !useHands);
    }

    // Rigs without tools (e.g. PlayerMedicalScene) leave these references empty.
    private static void SetActiveIfAssigned(GameObject target, bool active)
    {
        if (target != null)
            target.SetActive(active);
    }

    public void RecenterPlayer()
    {
        if (OfflineSession.IsOffline)
        {
            OfflinePlayerRig offlineRig = GetComponent<OfflinePlayerRig>();
            if (offlineRig != null)
            {
                offlineRig.Place();
                return;
            }

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
