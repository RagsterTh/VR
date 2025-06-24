using Photon.Pun;
using UnityEngine;

public class Table_Interactable : MonoBehaviour, IShootable
{
    [SerializeField] GameObject _menuOBJ;
    [SerializeField] GameObject _menuConfirm;
    [SerializeField] bool isOpened;

    PhotonView _phView;

    private void Start()
    {
        _phView = GetComponent<PhotonView>();
    }

    public void OnMouseEnter()
    {
        _phView.RPC("RPC_Select", RpcTarget.AllBuffered);
    }

    public void OnMouseExit()
    {
        _phView.RPC("RPC_Deselect", RpcTarget.AllBuffered);
    }

    private void OnMouseDown()
    {
        Hit();
    }

    public void Hit()
    {
        _phView.RPC("RPC_Hit", RpcTarget.AllBuffered);
    }

    [PunRPC]
    public void RPC_Select()
    {
        isOpened = true;
    }

    [PunRPC]
    public void RPC_Deselect()
    {
        isOpened = false;
    }
    [PunRPC]
    public void RPC_Hit()
    {
        if (!_menuConfirm.activeInHierarchy)
        {
            _menuConfirm.SetActive(true);
        }
        else
        {
            _menuConfirm.SetActive(false);
        }
    }
}
