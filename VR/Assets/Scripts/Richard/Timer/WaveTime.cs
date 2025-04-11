using Photon.Pun;
using System.Collections;
using UnityEngine;
using UnityEngine.Events;
using TMPro;
using System.Collections.Generic;

public class WaveTime : MonoBehaviourPunCallbacks
{

    [SerializeField] int startTime;
    [SerializeField] float countdown;
    bool isEnded;
    public UnityEvent OnEndTime;

    public void Start()
    {
        countdown = startTime;
    }
    public void StartTime()
    {
        StartCoroutine(PrintTime());
    }

    IEnumerator PrintTime()
    {
        yield return new WaitForSeconds(0);
        if (countdown <= 0 && !isEnded)
        {
            print("Cabo");
            OnEndTime.Invoke();
            isEnded = true;
            yield return null;
        }
        countdown -= Time.deltaTime;
        print((int)countdown);
        StartCoroutine(PrintTime());
    }
}
