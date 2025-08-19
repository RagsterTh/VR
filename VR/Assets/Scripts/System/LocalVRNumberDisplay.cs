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
            int number = ConnectionManager.instance.GetVRNumber(PhotonNetwork.LocalPlayer) + 1;
            _VRnumber.text = "Informe o número: "+ number;
        } else
        {
            gameObject.SetActive(false);
        }
            
    }

}
