using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.Events;
using Photon.Pun;
public class MedicalEmergencyManager : MonoBehaviour
{
    PhotonView _phView;
    [SerializeField]private MedicalEmergency[] allWounds;
    [SerializeField] private int numberToActivate = 3;
    [SerializeField] UnityEvent OnSceneLoad;
    private void Start()
    {
        _phView = GetComponent<PhotonView>();
        //allWounds = GetComponentsInChildren<MedicalEmergency>();
        ActivateRandomWounds();
        if(ConnectionManager.isVR)
            PhotonNetwork.LeaveRoom();

        if (PhotonNetwork.IsMasterClient)
            _phView.RPC("RPC_ActiveScene", RpcTarget.AllBuffered);
    }
    [PunRPC]
    public void RPC_ActiveScene()
    {
        OnSceneLoad?.Invoke();
    }

    private void ActivateRandomWounds()
    {
        foreach (var wound in allWounds)
        {
            wound.gameObject.SetActive(false);
        }

        int activated = 0;
        while (activated < numberToActivate)
        {
            int rand = Random.Range(0, allWounds.Length);
            if (!allWounds[rand].gameObject.activeSelf)
            {
                allWounds[rand].gameObject.SetActive(true);
                activated++;
            }
        }
    }
}
