using Photon.Pun;
using UnityEngine;

public class UserOnly : MonoBehaviour
{

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        if(ConnectionManager.isVR)
            gameObject.SetActive(false);
    }
}
