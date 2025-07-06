using UnityEngine;
using Photon.Pun;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
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
        if (SceneManager.GetActiveScene().name.Equals("LoadingScene"))
        {
            PhotonNetwork.AutomaticallySyncScene = true;
        }
    }

    // Update is called once per frame
    void Update()
    {
        
    }
    public void LoadLevel(string scene)
    {
        PhotonNetwork.LoadLevel(scene);
    }
    public void ResetUsers()
    {
        _phView.RPC("RPC_ResetUsers", RpcTarget.Others);
        PhotonNetwork.LoadLevel("LoadingScene");
    }

    public void OnActive()
    {
        if (!_phView.IsMine)
            return;


        _controllerPanel.SetActive(!_controllerPanel.activeSelf);
        //GameController.instance.ActiveBattle();
    }
    [PunRPC]
    public void RPC_ResetUsers()
    {
        PhotonNetwork.Disconnect();
    }
}
