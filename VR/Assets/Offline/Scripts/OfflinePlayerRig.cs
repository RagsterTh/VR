using System.Collections;
using System.Collections.Generic;
using Unity.XR.CoreUtils;
using UnityEngine;
using UnityEngine.XR;

/// <summary>
/// Offline replacement for the rig start-up done by ResetPosition + Photon ownership:
/// keeps a single local XR rig (one camera, one AudioListener), then places the headset
/// on the start point, turning the rig so the player's current physical heading matches
/// the start direction. Re-applied when the user recenters.
/// </summary>
[DisallowMultipleComponent]
public sealed class OfflinePlayerRig : MonoBehaviour
{
    public enum HeadTarget
    {
        [Tooltip("Same target as the original ResetPosition: headset moved to the world origin.")]
        ProjectDefaultOrigin,
        [Tooltip("Floor of the rig on the start point; the headset keeps the player's real height.")]
        StartPointFloor
    }

    [Header("Start point")]
    [Tooltip("Where the player starts. Empty = this object's transform when the scene starts.")]
    [SerializeField] private Transform _startPoint;
    [SerializeField] private HeadTarget _headTarget = HeadTarget.ProjectDefaultOrigin;
    [Tooltip("Turn the rig so the headset faces the start point's forward direction.")]
    [SerializeField] private bool _alignHeading = true;

    [Header("Timing")]
    [Tooltip("Max seconds to wait for headset tracking before placing anyway.")]
    [SerializeField] private float _trackingTimeout = 1.5f;

    private XROrigin _origin;
    private Vector3 _startPosition;
    private Vector3 _startForward;
    private readonly List<XRInputSubsystem> _inputSubsystems = new();

    public bool IsPlaced { get; private set; }

    /// <summary>Adds the rig setup to a player spawned by offline code. Call right after Instantiate.</summary>
    public static OfflinePlayerRig Attach(GameObject playerRoot, Vector3 startPosition, Vector3 startForward)
    {
        OfflinePlayerRig rig = playerRoot.GetComponent<OfflinePlayerRig>();
        if (rig == null)
            rig = playerRoot.AddComponent<OfflinePlayerRig>();
        rig.SetStart(startPosition, startForward);
        return rig;
    }

    public void SetStart(Vector3 position, Vector3 forward)
    {
        _startPosition = position;
        _startForward = forward;
    }

    private void Awake()
    {
        _origin = GetComponentInChildren<XROrigin>(true);
        Transform start = _startPoint != null ? _startPoint : transform;
        SetStart(start.position, start.forward);

        // ResetPosition teleports the camera 3s after spawn; placement is handled here instead.
        foreach (ResetPosition reset in GetComponentsInChildren<ResetPosition>(true))
            reset.enabled = false;
    }

    private IEnumerator Start()
    {
        KeepSingleAudioListener();

        float waited = 0f;
        while (!IsHeadTracked() && waited < _trackingTimeout)
        {
            waited += Time.unscaledDeltaTime;
            yield return null;
        }
        // One extra frame so the tracked pose is applied to the camera transform.
        yield return null;

        Place();

        SubsystemManager.GetSubsystems(_inputSubsystems);
        foreach (XRInputSubsystem subsystem in _inputSubsystems)
            subsystem.trackingOriginUpdated += OnTrackingOriginUpdated;
    }

    private void OnDestroy()
    {
        foreach (XRInputSubsystem subsystem in _inputSubsystems)
        {
            if (subsystem != null)
                subsystem.trackingOriginUpdated -= OnTrackingOriginUpdated;
        }
    }

    private void OnTrackingOriginUpdated(XRInputSubsystem _)
    {
        Place();
    }

    public void Place()
    {
        if (_origin == null || _origin.Camera == null)
        {
            transform.position = _startPosition;
            IsPlaced = true;
            return;
        }

        Vector3 up = _origin.transform.up;

        if (_alignHeading)
        {
            Vector3 cameraForward = Vector3.ProjectOnPlane(_origin.Camera.transform.forward, up);
            Vector3 targetForward = Vector3.ProjectOnPlane(_startForward, up);
            if (cameraForward.sqrMagnitude > 0.0001f && targetForward.sqrMagnitude > 0.0001f)
                _origin.RotateAroundCameraUsingOriginUp(Vector3.SignedAngle(cameraForward, targetForward, up));
        }

        if (_headTarget == HeadTarget.ProjectDefaultOrigin)
        {
            _origin.MoveCameraToWorldLocation(Vector3.zero);
        }
        else
        {
            // Keep the physical offset of the head: only the horizontal position is snapped.
            Vector3 cameraOnFloor = Vector3.ProjectOnPlane(_origin.Camera.transform.position - _origin.transform.position, up);
            _origin.transform.position = _startPosition - cameraOnFloor;
        }

        IsPlaced = true;
    }

    private bool IsHeadTracked()
    {
        InputDevice head = InputDevices.GetDeviceAtXRNode(XRNode.Head);
        return head.isValid && head.TryGetFeatureValue(CommonUsages.isTracked, out bool tracked) && tracked;
    }

    private void KeepSingleAudioListener()
    {
        AudioListener mine = GetComponentInChildren<AudioListener>(true);
        if (mine == null)
            return;

        foreach (AudioListener listener in FindObjectsByType<AudioListener>(FindObjectsSortMode.None))
        {
            if (listener != mine)
                listener.enabled = false;
        }
    }
}
