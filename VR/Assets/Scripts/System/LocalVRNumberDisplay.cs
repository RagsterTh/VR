using UnityEngine;
using TMPro;
using Photon.Pun;

public class LocalVRNumberDisplay : MonoBehaviour
{
    private TMP_Text _VRnumber;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        if (ConnectionManager.isVR)
        {
            _VRnumber.text = "Informe o número: "+PhotonNetwork.LocalPlayer.ActorNumber;
        } else
        {
            gameObject.SetActive(false);
        }
            
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
