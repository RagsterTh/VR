using Photon.Pun;
using System.Collections;
using UnityEngine;
using UnityEngine.Events;
using TMPro;
using Unity.VisualScripting;

public class WaveTime : MonoBehaviourPunCallbacks
{

    [SerializeField] int startTime;
    [SerializeField] float countdown;
    [SerializeField] TextMeshProUGUI timerText;
    bool isEnded;
    public UnityEvent OnEndTime;

    public void Start()
    {
        countdown = startTime;
        timerText.text = "";
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
        if (isEnded)
        {
            Destroy(this);
        }
        countdown -= Time.deltaTime;
        string minutes, seconds, cents;
        int minutesValue,secondsValue;
        minutesValue = (int)(countdown / 60);
        minutes = minutesValue.ToString();
        secondsValue = (int)(countdown - (minutesValue * 60));
        seconds = secondsValue.ToString();
        if (secondsValue < 10)
        {
            seconds = $"0{seconds}";
        }
        cents = ((int)((countdown - (int)countdown)*100)).ToString();
        timerText.text = $"{minutes}:{seconds}:{cents}";

        StartCoroutine(PrintTime());
    }
}
