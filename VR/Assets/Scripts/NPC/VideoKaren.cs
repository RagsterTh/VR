using UnityEngine;
using UnityEngine.Video;

public class VideoKaren : MonoBehaviour
{
    [SerializeField] VideoClip[] videos;
    VideoPlayer _videoPlayer;

    private void Start()
    {
        _videoPlayer = GetComponent<VideoPlayer>();
    }

    public void ChangeVideo(int index) 
    {
        if (index < 0 || index >= videos.Length)
        {
            Debug.LogError("Index out of bounds for audio clips array.");
            return;
        }
        _videoPlayer.clip = videos[index];
        _videoPlayer.Play();
    }
}
