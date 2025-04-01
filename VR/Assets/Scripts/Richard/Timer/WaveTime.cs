using Photon.Pun;
using System.Collections;
using UnityEngine;
using UnityEngine.Events;
using TMPro;
using Photon.Pun.Demo.PunBasics;
using NUnit.Framework;
using System.Collections.Generic;

public class WaveTime : MonoBehaviourPunCallbacks
{

    [SerializeField] int startTime;
    [SerializeField] float countdown;

    public UnityEvent OnStartTime;
    [SerializeField] List<TextMeshProUGUI> timerText;
    List<GameObject> players = new List<GameObject>();


    private void Awake()
    {
        foreach (GameObject player in players)
        {
            timerText.Add(player.GetComponentInChildren<TextMeshProUGUI>());
            print(player);
        }
        countdown = startTime;
        OnStartTime.AddListener(delegate
        {
            StartCoroutine(PrintTime());
        });
    }

    public void StartTime()
    {
        OnStartTime.Invoke();
    }

    private float TimeRemaining()
    {
        int timer = PhotonNetwork.ServerTimestamp - this.startTime;
        return this.countdown - timer / 1000f;
    }

    IEnumerator PrintTime()
    {
        yield return new WaitForSeconds(1);
        TimeRemaining();
        foreach (var text in timerText)
        {
            text.text = TimeRemaining().ToString();
            print(text);
        }
        print(countdown);
        if (countdown <= 0)
        {
            print("Cabo");
        }
    }
    /*
    public static bool TryGetStartTime(out int startTimestamp)
    {
        startTimestamp = PhotonNetwork.ServerTimestamp;

        object startTimeFromProps;
        if (PhotonNetwork.CurrentRoom.CustomProperties.TryGetValue(CountdownStartTime, out startTimeFromProps))
        {
            startTimestamp = (int)startTimeFromProps;
            return true;
        }

        return false;
    }


    public static void SetStartTime()
    {
        int startTime = 0;
        bool wasSet = TryGetStartTime(out startTime);

        Hashtable props = new Hashtable
            {
                {CountdownTimer.CountdownStartTime, (int)PhotonNetwork.ServerTimestamp}
            };
        PhotonNetwork.CurrentRoom.SetCustomProperties(props);


        Debug.Log("Set Custom Props for Time: " + props.ToStringFull() + " wasSet: " + wasSet);
    }*/
}
