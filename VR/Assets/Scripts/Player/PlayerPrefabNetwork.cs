using UnityEngine;
using Photon.Pun;
using UnityEngine.SceneManagement;

public enum PlayerTool
{
    Hand, Gun
}
public class PlayerPrefabNetwork : MonoBehaviour
{
    PhotonView _phView;
    [SerializeField] GameObject[] _elements;
    [SerializeField] GameObject _leftGun;
    [SerializeField] GameObject _rightGun;
    [SerializeField] GameObject _leftHand;
    [SerializeField] GameObject _rightHand;
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
        
    }
    [PunRPC]
    void RPC_Hands(int tool)
    {
        _leftGun.SetActive(tool != (int)PlayerTool.Hand);
        _rightGun.SetActive(tool != (int)PlayerTool.Hand);
        _leftHand.SetActive(tool == (int)PlayerTool.Hand);
        _rightHand.SetActive(tool == (int)PlayerTool.Hand);
    }
}
