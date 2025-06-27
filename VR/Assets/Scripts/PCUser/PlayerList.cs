using ExitGames.Client.Photon;
using Photon.Pun;
using Photon.Realtime;
using TMPro;
using UnityEngine;

public class PlayerList : MonoBehaviourPunCallbacks
{
    TMP_Text[] _txts;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Awake()
    {
        _txts = transform.GetComponentsInChildren<TMP_Text>();
    }

    // Update is called once per frame
    void Update()
    {
        
    }
    private void OnEnable()
    {
        int playersVR = 0;
        foreach (var item in PhotonNetwork.PlayerList)
        {
            if ((bool)item.CustomProperties["IsVR"])
            {
                _txts[playersVR].text = $"Player{playersVR + 1}: On";
                playersVR++;
            }
        }
    }
    public override void OnPlayerEnteredRoom(Player newPlayer)
    {
        int playersVR = 0;
        foreach (var item in PhotonNetwork.PlayerList)
        {
            if ((bool)item.CustomProperties["IsVR"])
            {
                _txts[playersVR].text = $"Player{playersVR + 1}: On";
                playersVR++;
            }
        }
    }


}
