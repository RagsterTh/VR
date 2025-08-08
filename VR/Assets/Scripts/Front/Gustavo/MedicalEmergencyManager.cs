using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.Events;
using Photon.Pun;
using System.Collections.Generic;

public class MedicalEmergencyManager : MonoBehaviour
{
    PhotonView _phView;
    [SerializeField] private MedicalEmergency[] allWounds;
    [SerializeField] private int numberToActivate = 3;
    [SerializeField] UnityEvent OnSceneLoad;

    private Queue<MedicalEmergency> woundQueue = new Queue<MedicalEmergency>();
    private MedicalEmergency currentWound;

    private void Start()
    {
        _phView = GetComponent<PhotonView>();
        PrepareRandomWounds();

        if (PhotonNetwork.IsMasterClient)
            _phView.RPC("RPC_ActiveScene", RpcTarget.AllBuffered);

        ActivateNextWound();
    }

    [PunRPC]
    public void RPC_ActiveScene()
    {
        OnSceneLoad?.Invoke();
    }

    private void PrepareRandomWounds()
    {
        foreach (var wound in allWounds)
        {
            wound.gameObject.SetActive(false);
        }

        List<int> indices = new List<int>();
        while (indices.Count < numberToActivate)
        {
            int rand = Random.Range(0, allWounds.Length);
            if (!indices.Contains(rand))
                indices.Add(rand);
        }

        foreach (int idx in indices)
        {
            woundQueue.Enqueue(allWounds[idx]);
        }
    }

    public void ActivateNextWound()
    {
        if (currentWound != null)
        {
            currentWound.gameObject.SetActive(false);
            currentWound = null;
        }

        if (woundQueue.Count > 0)
        {
            currentWound = woundQueue.Dequeue();
            currentWound.gameObject.SetActive(true);
        }
        else
        {
            Debug.Log("Todos os ferimentos foram tratados!");
        }
    }
}
