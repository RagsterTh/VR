using Meta.XR.Editor.Id;
using System.Collections;
using System.Collections.Generic;
using Unity.XR.CoreUtils;
using UnityEngine;
using UnityEngine.InputSystem.XR;
using UnityEngine.XR;

public class ResetPosition : MonoBehaviour
{
    XROrigin _origin;
    PlayerPrefabNetwork _playerNetwork;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Awake()
    {
        _origin = GetComponent<XROrigin>();
        _playerNetwork = GetComponentInParent<PlayerPrefabNetwork>();
    }
    private IEnumerator Start()
    {
        yield return new WaitForSeconds(3);
        var subsystems = new List<XRInputSubsystem>();
        SubsystemManager.GetSubsystems(subsystems);

        foreach (var s in subsystems)
            s.TryRecenter(); // pede ao runtime para recentrar o HMD
        _origin.MoveCameraToWorldLocation(Vector3.zero);
        //_origin.Camera.transform.position = Vector3.zero;
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
