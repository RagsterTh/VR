using UnityEngine;
using TMPro;
using Photon.Pun;
using System.Collections;

public class LocalVRNumberDisplay : MonoBehaviour
{
    private TMP_Text _VRnumber;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    IEnumerator Start()
    {
        _VRnumber = GetComponent<TMP_Text>();
        yield return new WaitUntil(() => PhotonNetwork.InRoom);
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
