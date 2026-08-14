using UnityEngine;
using Photon.Pun;
using UnityEngine.SceneManagement;
using System.Collections;

public enum PlayerTool
{
    Hand, Gun
}
public class PlayerPrefabNetwork : MonoBehaviour
{
    [SerializeField] GameObject _playerLifebar;
    PhotonView _phView;
    [SerializeField] GameObject[] _elements;
    [SerializeField] GameObject _leftGun;
    [SerializeField] GameObject _rightGun;
    [SerializeField] GameObject _leftHandMecanic;
    [SerializeField] GameObject _leftHandVisual;
    [SerializeField] GameObject _rightHandMecanic;
    [SerializeField] GameObject _rightHandVisual;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Awake()
    {
        _phView = GetComponent<PhotonView>();
        if (!_phView.IsMine)
            return;

        foreach (var item in _elements)
        {
            item.SetActive(true);
        }

        switch (SceneManager.GetActiveScene().name)
        {
            case "Game":
                _phView.RPC("RPC_Hands", RpcTarget.AllBuffered, (int)PlayerTool.Gun);
                break;
            default:
                _phView.RPC("RPC_Hands", RpcTarget.AllBuffered, (int)PlayerTool.Hand);
                break;

        }
        SimulationController.Instance.OnShootGameBegins.AddListener(delegate
        {
            _phView.RPC("RPC_Hands", RpcTarget.AllBuffered, (int)PlayerTool.Gun);
        });

    }
    void Start()
    {
        var lifeBarService = ServiceLocator.Get<PlayersLifeBar>();
        if (lifeBarService != null)
        {
            var list = new System.Collections.Generic.List<GameObject>(lifeBarService.LifeBar) { _playerLifebar };
            lifeBarService.LifeBar = list.ToArray();
        }
    }
    [PunRPC]
    void RPC_Hands(int tool)
    {
        _leftGun.SetActive(tool != (int)PlayerTool.Hand);
        _rightGun.SetActive(tool != (int)PlayerTool.Hand);
        _leftHandMecanic.SetActive(tool == (int)PlayerTool.Hand);
        _leftHandVisual.SetActive(tool == (int)PlayerTool.Hand);
        _rightHandMecanic.SetActive(tool == (int)PlayerTool.Hand);
        _rightHandVisual.SetActive(tool == (int)PlayerTool.Hand);
    }
    public void RecenterPlayer()
    {
        transform.position = SimulationController.Instance.SpawnPoints[(int)PhotonNetwork.LocalPlayer.CustomProperties["VRNumber"]].position;

    }
}
