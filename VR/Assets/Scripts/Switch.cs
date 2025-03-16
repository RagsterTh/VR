using Photon.Pun;
using UnityEngine;
using UnityEngine.Events;
[RequireComponent(typeof(PhotonView))]
public class Switch : MonoBehaviour, IShootable
{
    PhotonView _phView;
    public UnityEvent OnSwitchActivate;

    public void Hit()
    {
        _phView.RPC("RPC_SwitchActivate", RpcTarget.AllBuffered);
    }

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        _phView = GetComponent<PhotonView>();
    }

    // Update is called once per frame
    void Update()
    {
        
    }
    [PunRPC]
    public void RPC_SwitchActivate()
    {
        OnSwitchActivate.Invoke();
    }
}
