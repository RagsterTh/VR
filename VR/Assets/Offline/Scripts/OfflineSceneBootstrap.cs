using System.Collections;
using UnityEngine;

/// <summary>
/// First thing that runs in every offline scene: forces the VR path (no PC operator view),
/// switches off objects that only make sense online, and fades in once the local rig is placed.
/// </summary>
[DefaultExecutionOrder(-10000)]
public sealed class OfflineSceneBootstrap : MonoBehaviour
{
    [Tooltip("Online-only objects of this scene (Photon connection, PC operator camera, VR detector...). Disabled on load.")]
    [SerializeField] private GameObject[] _disableOnLoad;

    [Header("Comfort")]
    [SerializeField] private float _fadeDuration = 0.6f;
    [Tooltip("Max seconds to keep the screen black waiting for the player rig to be placed.")]
    [SerializeField] private float _maxWaitForRig = 4f;

    private void Awake()
    {
        ConnectionManager.isVR = true;
        OfflineScreenFade.Duration = _fadeDuration;
        OfflineScreenFade.SetBlack();

        foreach (GameObject item in _disableOnLoad)
        {
            if (item != null)
                item.SetActive(false);
        }
    }

    /// <summary>Target for scene events that used to wait for the host (e.g. end of the credits).</summary>
    public void ReturnToEntry()
    {
        OfflineSession.ReturnToEntry();
    }

    private IEnumerator Start()
    {
        float waited = 0f;
        OfflinePlayerRig rig = null;
        while (waited < _maxWaitForRig)
        {
            if (rig == null)
                rig = FindAnyObjectByType<OfflinePlayerRig>();
            if (rig != null && rig.IsPlaced)
                break;

            waited += Time.unscaledDeltaTime;
            yield return null;
        }

        OfflineScreenFade.FadeIn();
    }
}
