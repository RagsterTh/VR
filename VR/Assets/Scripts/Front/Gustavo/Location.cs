using System.Collections;
using UnityEngine;
using Photon.Pun;
public class Location : MonoBehaviour, IShootable
{
    [SerializeField] GameObject _menuOBJ;
    [SerializeField] GameObject _menuConfirm;
    [SerializeField] bool isOpened;
    [SerializeField] bool isConfirmOpened;
    [SerializeField] Vector3 _originalScale;
    [SerializeField] Vector3 _upperScale;
    [SerializeField] float _interpolateTime;
    PhotonView _phView;
    bool isGameAsked;
    private void Start()
    {
        _phView = GetComponent<PhotonView>();
    }

    private void FixedUpdate()
    {
        _menuOBJ.SetActive(isOpened);

        isConfirmOpened = _menuConfirm.activeInHierarchy;
    }

    private void OnMouseDown()
    {
        Hit();
    }

    public void OnMouseEnter()
    {
        //_phView.RPC("RPC_Select", RpcTarget.AllBuffered);
    }

    public void OnMouseExit()
    {
        //_phView.RPC("RPC_Deselect", RpcTarget.AllBuffered);
    }

    IEnumerator InterpolateScale(Vector3 start, Vector3 end, float time)
    {
        float elapsedTime = 0f;
        while (elapsedTime < time)
        {
            transform.localScale = Vector3.Lerp(start, end, elapsedTime / time);
            elapsedTime += Time.deltaTime;
            yield return null;
        }
        transform.localScale = end;
    }
    
    public void Hit()
    {

        _phView.RPC("RPC_AskGoToGame", RpcTarget.MasterClient);

    }
    [PunRPC]
    public void RPC_AskGoToGame()
    {
        if (isGameAsked)
            return;

        PhotonNetwork.LoadLevel("Game");
        isGameAsked = true;


    }
    [PunRPC]
    public void RPC_Select()
    {
        isOpened = true;
        StartCoroutine(InterpolateScale(transform.localScale, _upperScale, _interpolateTime));
    }
    [PunRPC]
    public void RPC_Deselect()
    {
        isOpened = false;
        StartCoroutine(InterpolateScale(transform.localScale, _originalScale, _interpolateTime));
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
