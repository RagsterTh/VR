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
    [SerializeField] GameObject _gun;
    [SerializeField] GameObject _hand;
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
        /*
        switch (SceneManager.GetActiveScene().name)
        {
            default:
                _phView
                break;

        }
        */
    }
    [PunRPC]
    void RPC_Hands(int tool)
    {

    }
}
