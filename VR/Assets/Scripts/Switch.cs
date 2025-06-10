using Photon.Pun;
using UnityEngine;
using UnityEngine.Events;
[RequireComponent(typeof(PhotonView))]
public class Switch : MonoBehaviour
{
    PhotonView _phView;
    public UnityEvent OnSwitchActivate;

    public void Active(string rpc)
    {
        _phView.RPC(rpc, RpcTarget.AllBuffered);
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
