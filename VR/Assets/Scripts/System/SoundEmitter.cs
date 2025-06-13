using UnityEngine;
using Photon.Pun;
[RequireComponent(typeof(AudioSource), typeof(PhotonView))]
public class SoundEmitter : MonoBehaviour, ISoundable
{
    AudioSource audioSource;
    PhotonView phView;

    public void PlaySound()
    {
        phView.RPC("RPC_PlaySound", RpcTarget.All);
    }

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        audioSource = GetComponent<AudioSource>();
        phView = GetComponent<PhotonView>();
    }
    [PunRPC]
    public void RPC_PlaySound()
    {
        audioSource.PlayOneShot(audioSource.clip);
        print("tocou");
    }
    
}
