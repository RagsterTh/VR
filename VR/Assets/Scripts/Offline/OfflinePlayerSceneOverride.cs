using UnityEngine;

/// <summary>
/// Replaces a scene's legacy player rig only while an offline session is active.
/// The online scene setup is left untouched.
/// </summary>
public sealed class OfflinePlayerSceneOverride : MonoBehaviour
{
    [SerializeField] private GameObject _onlinePlayerRig;
    [SerializeField] private GameObject _offlinePlayerPrefab;

    private void Awake()
    {
        if (!OfflineSession.IsOffline || _onlinePlayerRig == null || _offlinePlayerPrefab == null)
            return;

        Transform legacyTransform = _onlinePlayerRig.transform;
        Vector3 spawnPosition = legacyTransform.position;
        Quaternion spawnRotation = legacyTransform.rotation;

        _onlinePlayerRig.SetActive(false);
        Instantiate(_offlinePlayerPrefab, spawnPosition, spawnRotation);
    }
}
