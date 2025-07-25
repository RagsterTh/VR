using ExitGames.Client.Photon;
using Photon.Pun;
using Photon.Realtime;
using System.Collections;
using TMPro;
using UnityEngine;

public class PlayerList : MonoBehaviour
{
    TMP_Text[] _txts;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Awake()
    {
        _txts = transform.GetComponentsInChildren<TMP_Text>();
    }
    private void Start()
    {
        StartCoroutine(PlayersVerification());
    }
    // Update is called once per frame
    void Update()
    {
        
    }
    IEnumerator PlayersVerification()
    {
        if (!PhotonNetwork.LocalPlayer.IsMasterClient)
        {
            PhotonNetwork.SetMasterClient(PhotonNetwork.LocalPlayer);
        }
        int playersVR = 0;
        foreach (var item in PhotonNetwork.PlayerList)
        {
            if ((bool)item.CustomProperties["IsVR"])
            {
                _txts[playersVR].text = $"Player{playersVR + 1}: On";
                playersVR++;
            }
        }
        if(playersVR < _txts.Length)
        {
            for (int i = playersVR; i < _txts.Length; i++)
            {
                _txts[i].text = $"Player{i + 1}: Off";
            }
        }
        yield return new WaitForSeconds(2);
        StartCoroutine(PlayersVerification());
    }


}
