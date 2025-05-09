using UnityEngine;
using Photon.Pun;
public class DesktopMode : MonoBehaviour
{
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
            gameObject.SetActive(ConnectionManager.isVR);
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    //Temporary
    public void OnConnection()
    {
        print("foi");
        PhotonNetwork.ConnectUsingSettings();
    }
}
