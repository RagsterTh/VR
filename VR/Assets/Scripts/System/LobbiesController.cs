using Photon.Pun;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

public class LobbiesController : MonoBehaviour
{
    PhotonView _phView;
    [SerializeField]GameObject[] _lobbies;
    [SerializeField] Transform[] _spawnPoints;
    [SerializeField] SceneResources _sceneResources;
    static Dictionary<ResourceTypes, GameObject> _resourcesRegister = new Dictionary<ResourceTypes, GameObject>();

    [SerializeField] UnityEvent OnSceneLoaded;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    IEnumerator Start()
    {
        _phView = GetComponent<PhotonView>();
        _resourcesRegister.Clear();
        foreach (var item in _sceneResources.resources)
        {
            _resourcesRegister.Add(item.type, item.resource);
        }
        yield return new WaitUntil(() => PhotonNetwork.InRoom);
        if (ConnectionManager.isVR)
        {
            PhotonNetwork.Instantiate(GetResource(ResourceTypes.PlayerVR).name, _spawnPoints[Random.Range(1, _spawnPoints.Length)].position, Quaternion.LookRotation(_spawnPoints[0].up)).GetPhotonView();
        }
        yield return new WaitForSeconds(2);
        if (PhotonNetwork.IsMasterClient)
            _phView.RPC("RPC_ActiveScene", RpcTarget.AllBuffered);
    }
    public static GameObject GetResource(ResourceTypes resource)
    {
        return _resourcesRegister[resource];
    }

    // Update is called once per frame
    void Update()
    {
        
    }
    [PunRPC]
    public void RPC_ActiveScene()
    {
        OnSceneLoaded?.Invoke();
    }

}
