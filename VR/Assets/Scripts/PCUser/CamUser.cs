using UnityEngine;
using Photon.Pun;
using UnityEngine.UI;
public class CamUser : MonoBehaviour
{
    PhotonView _phView;
    GameObject _cam;
    GameObject _controllerPanel;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        _cam = transform.GetChild(0).gameObject;
        _phView = GetComponent<PhotonView>();
        if (ConnectionManager.isVR)
            return;

        _controllerPanel = GetComponentInChildren<Image>(true).gameObject;
        _cam.SetActive(true);
        Cursor.lockState = CursorLockMode.None;
    }

    // Update is called once per frame
    void Update()
    {
        
    }
    public void LoadLevel(string scene)
    {
        _phView.RPC("RPC_LoadLevel", RpcTarget.All, scene);
    }

    public void OnActive()
    {
        if (!_phView.IsMine)
            return;


        _controllerPanel.SetActive(!_controllerPanel.activeSelf);
        //GameController.instance.ActiveBattle();
    }

    [PunRPC]
    public void RPC_LoadLevel(string scene)
    {
        PhotonNetwork.LoadLevel(scene);
    }
}
