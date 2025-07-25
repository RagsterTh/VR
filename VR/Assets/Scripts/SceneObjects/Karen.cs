using UnityEngine;

public class Karen : MonoBehaviour
{
    public AudioClip[] audioClips;
    ISoundable soundable;
    AudioSource audioSource;
    void Start()
    {
        soundable = GetComponent<ISoundable>();
        audioSource = GetComponent<AudioSource>();
    }

    public void ChangeSound(int index)
    {
        if (index < 0 || index >= audioClips.Length)
        {
            Debug.LogError("Index out of bounds for audio clips array.");
            return;
        }
        audioSource.clip = audioClips[index];
        soundable.PlaySound();
    }
}
