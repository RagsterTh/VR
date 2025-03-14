using Photon.Pun;
using Photon.Realtime;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

public class GameController : MonoBehaviour
{
    public static GameController instance;
    [SerializeField] ObjectPool _enemyPool;
    [SerializeField]Transform[] _spawnPoints;
    [SerializeField]SceneResources _sceneResources;

    static Dictionary<ResourceTypes, GameObject> _resourcesRegister = new Dictionary<ResourceTypes, GameObject>();

    //[Header("Events")]

    private void Awake()
    {
        instance = this;
    }
    // Start is called before the first frame update
    void Start()
    {
        foreach (var item in _sceneResources.resources)
        {
            _resourcesRegister.Add(item.type, item.resource);
        }
        PhotonNetwork.Instantiate(GetResource(ResourceTypes.Player).name, _spawnPoints[Random.Range(0, _spawnPoints.Length)].position, transform.rotation);
        if (!PhotonNetwork.IsMasterClient)
            return;
        _enemyPool.CallObject(_spawnPoints[3].position);
    }
    public static GameObject GetResource(ResourceTypes resource)
    {
        return _resourcesRegister[resource];
    }
    
}
