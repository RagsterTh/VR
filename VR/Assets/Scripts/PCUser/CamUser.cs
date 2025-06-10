using UnityEngine;
using Photon.Pun;
public class CamUser : MonoBehaviour
{
    PhotonView _phView;
    GameObject cam;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        cam = transform.GetChild(0).gameObject;
        _phView = GetComponent<PhotonView>();
        cam.SetActive(!ConnectionManager.isVR);
    }

    // Update is called once per frame
    void Update()
    {
        
    }
    public void OnActive()
    {
        GameController.instance.ActiveBattle();
    }
}
