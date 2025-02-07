using Photon.Pun;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

public class GameController : MonoBehaviour
{
    public static GameController instance;
    [SerializeField]SceneResources sceneResources;
    Dictionary<ResourceTypes, GameObject> resourcesRegister = new Dictionary<ResourceTypes, GameObject>();


    [Header("Events")]
    public UnityEvent OnGameSceneLoad;

    private void Awake()
    {
        if (!instance)
        {
            instance = this;
        }
    }
    // Start is called before the first frame update
    void Start()
    {
        foreach (var item in sceneResources.resources)
        {
            resourcesRegister.Add(item.type, item.resource);
        }
        OnGameSceneLoad.AddListener(delegate
        {
            PhotonNetwork.Instantiate(resourcesRegister[ResourceTypes.Player].name, Vector3.zero, transform.rotation);
        });
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
