using UnityEngine;
using TMPro;
using Photon.Pun;
using System.Collections;

public class LocalVRNumberDisplay : MonoBehaviour
{
    private TMP_Text _VRnumber;
    [SerializeField] string _onConectedText = "Informe que está pronto";
    IEnumerator Start()
    {
        _VRnumber = GetComponent<TMP_Text>();
        yield return new WaitUntil(() => PhotonNetwork.InRoom);
        if (ConnectionManager.isVR)
        {
            _VRnumber.text = _onConectedText;
        }
        else
        {
            gameObject.SetActive(false);
        }

    }

}
