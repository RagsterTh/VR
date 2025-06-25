using Photon.Pun;
using UnityEngine;

public class Table_Interactable : MonoBehaviour, IShootable
{
    [SerializeField] GameObject _menuOBJ;
    [SerializeField] bool isOpened;

    PhotonView _phView;

    private void Start()
    {
        _phView = GetComponent<PhotonView>();
    }

    private void OnMouseDown()
    {
        print("triggo");
        Hit();
    }

    public void Hit()
    {
        print("ativo");
        _phView.RPC("RPC_Hit", RpcTarget.AllBuffered);
    }

    [PunRPC]
    public void RPC_Hit()
    {
        print("Funfo");
        if (!_menuOBJ.activeInHierarchy)
        {
            _menuOBJ.SetActive(true);
        }
        else
        {
            _menuOBJ.SetActive(false);
        }
    }
}
