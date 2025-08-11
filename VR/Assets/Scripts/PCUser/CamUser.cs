using UnityEngine;
using Photon.Pun;
using UnityEngine.UI;
using System.Collections.Generic;
using UnityEngine.InputSystem;
public class CamUser : MonoBehaviour
{
    private PhotonView _phView;
    private GameObject _cam;
    private GameObject _controllerPanel;
    private List<Camera> _camsManager = new List<Camera>();
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        _cam = transform.GetChild(0).gameObject;
        if (!ConnectionManager.isVR)
            _camsManager.Add(_cam.GetComponentInChildren<Camera>());

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
        PhotonNetwork.LoadLevel(scene);
    }
    public void Quit()
    {
        _phView.RPC("RPC_QuitAll", RpcTarget.AllBuffered);
    }

    public void ResetUsers()
    {
        _phView.RPC("RPC_ResetUsers", RpcTarget.Others);
        PhotonNetwork.LoadLevel("LoadingScene");
    }
    public void ResetVRs()
    {
        _phView.RPC("RPC_ResetVRs", RpcTarget.All);
    }
    [PunRPC]
    public void RPC_ResetVRs()
    {
        List<UnityEngine.XR.InputDevice> devices = new List<UnityEngine.XR.InputDevice>();
        UnityEngine.XR.InputDevices.GetDevices(devices);

        foreach (var device in devices)
        {
            Debug.Log("Device detected: " + device.name);
        }
    }

    public void OnActive()
    {
        if (!_phView.IsMine)
            return;


        _controllerPanel.SetActive(!_controllerPanel.activeSelf);
        //GameController.instance.ActiveBattle();
    }
    [PunRPC]
    public void RPC_QuitAll()
    {
        Application.Quit();
    }
    [PunRPC]
    public void RPC_ResetUsers()
    {
        PhotonNetwork.Disconnect();
    }
    public void AddCamera(Camera cam)
    {
        _camsManager.Add(cam);
        cam.enabled = false;
    }
    public void ChangeCam(InputAction.CallbackContext value)
    {
        if (!value.phase.Equals(InputActionPhase.Performed))
            return;
        if(value.control.name.Equals("backquote") || value.control.name.Equals("5"))
        {
            CamsManager(0);
            return;
        }
        int camNumber = int.Parse(value.control.name);
        CamsManager(camNumber);
    }
    void CamsManager(int activeCamID)
    {
        if (_camsManager.Count <= 1 || activeCamID >= _camsManager.Count)
            return;
        for (int i = 0; i < _camsManager.Count; i++)
        {
            _camsManager[i].enabled = i == activeCamID;
        }
    }
}
