using Photon.Pun;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

public class LobbiesController : MonoBehaviour
{
    [SerializeField]GameObject[] _lobbies;
    [SerializeField] Transform[] _spawnPoints;
    [SerializeField] SceneResources _sceneResources;
    static Dictionary<ResourceTypes, GameObject> _resourcesRegister = new Dictionary<ResourceTypes, GameObject>();
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    IEnumerator Start()
    {
        foreach (var item in _sceneResources.resources)
        {
            _resourcesRegister.Add(item.type, item.resource);
        }
        yield return new WaitUntil(() => PhotonNetwork.InRoom);
        int playerID;
        if (ConnectionManager.isVR)
        {
            playerID = PhotonNetwork.Instantiate(GetResource(ResourceTypes.PlayerVR).name, _spawnPoints[PhotonNetwork.LocalPlayer.ActorNumber - 1].position, transform.rotation).GetPhotonView().ViewID;
        }
        else
        {
            playerID = PhotonNetwork.Instantiate(GetResource(ResourceTypes.Player).name, _spawnPoints[PhotonNetwork.LocalPlayer.ActorNumber - 1].position, transform.rotation).GetPhotonView().ViewID;
        }
        PhotonNetwork.AutomaticallySyncScene = true;
    }
    public static GameObject GetResource(ResourceTypes resource)
    {
        return _resourcesRegister[resource];
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
