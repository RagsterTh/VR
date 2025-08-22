using Meta.XR.Editor.Id;
using System.Collections;
using Unity.XR.CoreUtils;
using UnityEngine;
using UnityEngine.InputSystem.XR;

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
    private void Start()
    {
        _origin.MoveCameraToWorldLocation(Vector3.zero);
        //_origin.Camera.transform.position = Vector3.zero;
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
