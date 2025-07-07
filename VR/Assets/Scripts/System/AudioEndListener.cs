using System.Collections;
using UnityEngine;
using UnityEngine.Events;

public class AudioEndListener : MonoBehaviour
{
    AudioSource _audioSource;
    [SerializeField] UnityEvent OnVoiceEnds;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        _audioSource = GetComponent<AudioSource>();
    }

    // Update is called once per frame
    void Update()
    {
        
    }
    public void AudioDuration()
    {
        StartCoroutine(WaitingClip(_audioSource.clip.length));
    }
    IEnumerator WaitingClip(float duration)
    {
        yield return new WaitForSeconds(duration);
        yield return new WaitForSeconds(1);
        OnVoiceEnds.Invoke();
    }

}
