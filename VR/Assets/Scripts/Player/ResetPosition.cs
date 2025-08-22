using UnityEngine;
using UnityEngine.InputSystem.XR;

public class ResetPosition : MonoBehaviour
{
    TrackedPoseDriver trackingVR;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Awake()
    {
        trackingVR = GetComponent<TrackedPoseDriver>();
        trackingVR.trackingType = TrackedPoseDriver.TrackingType.RotationOnly;
        //transform.position = Vector3.zero;
    }
    private void Start()
    {
        trackingVR.trackingType = TrackedPoseDriver.TrackingType.RotationAndPosition;
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
