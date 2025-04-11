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

    [SerializeField] List<TextMeshProUGUI> timerText;

    public void Start()
    {
        countdown = startTime;
    }
    public void StartTime()
    {
        if (PhotonNetwork.IsMasterClient)
        {
            StartCoroutine(PrintTime());
        }
    }

    IEnumerator PrintTime()
    {
        yield return new WaitForSeconds(0);
        countdown -= Time.deltaTime;
        print((int)countdown);
        if (countdown <= 0)
        {
            print("Cabo");
            yield return null;
        }
        StartCoroutine(PrintTime());
    }
}
