using Photon.Pun;
using System.Collections;
using TMPro;
using UnityEngine;

public class LocalVRNumberDisplay : MonoBehaviour
{
    [SerializeField] private string _onConectedText = "Informe que está pronto";
    private TMP_Text _vrNumber;

    private IEnumerator Start()
    {
        _vrNumber = GetComponent<TMP_Text>();

        if (OfflineSession.IsOffline)
        {
            _vrNumber.text = _onConectedText;
            yield break;
        }

        yield return new WaitUntil(() => PhotonNetwork.InRoom);
        if (ConnectionManager.isVR)
            _vrNumber.text = _onConectedText;
        else
            gameObject.SetActive(false);
    }
}
