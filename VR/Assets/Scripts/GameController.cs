using Photon.Pun;
using Photon.Realtime;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

public class GameController : MonoBehaviour
{
    public static GameController instance;
    [SerializeField]Transform[] _spawnPoints;
    [SerializeField]SceneResources _sceneResources;

    Dictionary<ResourceTypes, GameObject> _resourcesRegister = new Dictionary<ResourceTypes, GameObject>();

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
        PhotonNetwork.Instantiate(_resourcesRegister[ResourceTypes.Player].name, _spawnPoints[Random.Range(0, _spawnPoints.Length)].position, transform.rotation);
    }
    
}
