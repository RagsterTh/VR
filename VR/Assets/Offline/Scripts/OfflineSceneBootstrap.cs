using System.Collections;
using UnityEngine;
using UnityEngine.UI;

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

        EnsurePointableCanvasModule();
        OfflineKiosk.Ensure();
        if (OfflineSession.IsCombatScene)
            OfflineTutorial.Prepare(gameObject);

        foreach (GameObject root in gameObject.scene.GetRootGameObjects())
        {
            foreach (Button button in root.GetComponentsInChildren<Button>(true))
            {
                if (button.GetComponent<OfflineButtonFeedback>() == null)
                    button.gameObject.AddComponent<OfflineButtonFeedback>();
            }
        }
    }

    private void EnsurePointableCanvasModule()
    {
        System.Type canvasType = null, moduleType = null;
        foreach (var assembly in System.AppDomain.CurrentDomain.GetAssemblies())
        {
            canvasType ??= assembly.GetType("Oculus.Interaction.PointableCanvas");
            moduleType ??= assembly.GetType("Oculus.Interaction.PointableCanvasModule");
        }
        if (canvasType == null || moduleType == null || FindAnyObjectByType(moduleType) != null)
            return;
        if (FindAnyObjectByType(canvasType, FindObjectsInactive.Include) == null)
            return;

        var eventSystem = FindAnyObjectByType<UnityEngine.EventSystems.EventSystem>();
        GameObject host = eventSystem != null ? eventSystem.gameObject : new GameObject("EventSystem", typeof(UnityEngine.EventSystems.EventSystem));
        host.AddComponent(moduleType);
    }

    public void ReturnToEntry()
    {
        OfflineSession.ReturnToEntry();
    }

    private IEnumerator Start()
    {
        if (FindAnyObjectByType<MedicalQuestions>() != null && FindAnyObjectByType<OfflineMedicalFeedback>() == null)
            gameObject.AddComponent<OfflineMedicalFeedback>();
        if (FindAnyObjectByType<GameController>() != null && FindAnyObjectByType<OfflineCombatFeedback>() == null)
            gameObject.AddComponent<OfflineCombatFeedback>();
        if (FindAnyObjectByType<TitleFunctions>() != null && GetComponent<OfflineCreditsSkip>() == null)
            gameObject.AddComponent<OfflineCreditsSkip>();
        if (FindAnyObjectByType<OfflineMapSelection>() != null && GetComponent<OfflineLobbyFeedback>() == null)
            gameObject.AddComponent<OfflineLobbyFeedback>();

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
