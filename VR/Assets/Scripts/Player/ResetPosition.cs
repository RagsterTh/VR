using Photon.Pun;
using System.Collections;
using System.Collections.Generic;
using Unity.XR.CoreUtils;
using UnityEngine;
using UnityEngine.XR;

public class ResetPosition : MonoBehaviour
{
    XROrigin _origin;
    PhotonView _phView;

    void Awake()
    {
        _origin = GetComponent<XROrigin>();
        _phView = GetComponentInParent<PhotonView>();
    }
    private IEnumerator Start()
    {
        if (!_phView.IsMine)
            yield break;

        yield return new WaitForSeconds(3);
        var subsystems = new List<XRInputSubsystem>();
        SubsystemManager.GetSubsystems(subsystems);

        foreach (var s in subsystems)
            s.TryRecenter();
        _origin.MoveCameraToWorldLocation(Vector3.zero);
    }
}
