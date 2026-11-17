using System.Collections;
using System.Collections.Generic;
using Unity.XR.CoreUtils;
using UnityEngine;
using UnityEngine.XR;
using UnityEngine.XR.Interaction.Toolkit.Locomotion.Movement;
using UnityEngine.XR.Interaction.Toolkit.Locomotion.Teleportation;

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

    [Header("Movement")]
    [Tooltip("Offline the player stays on the start point: thumbstick movement and teleport are turned off (turning still works).")]
    [SerializeField] private bool _blockMovement = true;

    [Header("Timing")]
    [Tooltip("Max seconds to wait for headset tracking before placing anyway.")]
    [SerializeField] private float _trackingTimeout = 1.5f;

    private XROrigin _origin;
    private Vector3 _startPosition;
    private Vector3 _startForward;
    private readonly List<XRInputSubsystem> _inputSubsystems = new();

    public bool IsPlaced { get; private set; }

    public static OfflinePlayerRig Attach(GameObject playerRoot, Vector3 startPosition, Vector3 startForward)
    {
        OfflinePlayerRig rig = playerRoot.GetComponent<OfflinePlayerRig>();
        if (rig == null)
            rig = playerRoot.AddComponent<OfflinePlayerRig>();
        rig.SetStart(startPosition, startForward);
        if (OfflineSession.IsCombatScene)
            rig._headTarget = HeadTarget.StartPointFloor;
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

        foreach (ResetPosition reset in GetComponentsInChildren<ResetPosition>(true))
            reset.enabled = false;

        if (_blockMovement)
        {
            foreach (ContinuousMoveProvider move in GetComponentsInChildren<ContinuousMoveProvider>(true))
                move.enabled = false;
            foreach (TeleportationProvider teleport in GetComponentsInChildren<TeleportationProvider>(true))
                teleport.enabled = false;
        }
    }

    private IEnumerator Start()
    {
        OfflineSpawnPoint spawn = FindAnyObjectByType<OfflineSpawnPoint>();
        if (spawn != null)
        {
            SetStart(spawn.transform.position, spawn.transform.forward);
            _headTarget = HeadTarget.StartPointFloor;
        }

        KeepSingleAudioListener();
        if (GetComponent<OfflinePlayerFeedback>() == null)
            gameObject.AddComponent<OfflinePlayerFeedback>();

        float waited = 0f;
        while (!IsHeadTracked() && waited < _trackingTimeout)
        {
            waited += Time.unscaledDeltaTime;
            yield return null;
        }
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
