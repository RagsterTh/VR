using System.Collections;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>
/// Comfort fade for the offline mode. A black world-space quad follows the active camera
/// (screen-space overlays do not render inside the headset) and survives scene loads,
/// so every transition is: fade to black -> load -> fade back in.
/// </summary>
public sealed class OfflineScreenFade : MonoBehaviour
{
    private const float DefaultDuration = 0.6f;

    private static OfflineScreenFade _instance;

    private Canvas _canvas;
    private Image _image;
    private Camera _target;
    private float _alpha;
    private bool _loading;

    public static float Duration { get; set; } = DefaultDuration;

    public static void FadeOutAndLoad(string sceneName)
    {
        Instance.StartCoroutine(Instance.LoadRoutine(sceneName));
    }

    public static void FadeIn()
    {
        Instance.StartCoroutine(Instance.FadeTo(0f));
    }

    public static void SetBlack()
    {
        Instance.SetAlpha(1f);
    }

    private static OfflineScreenFade Instance
    {
        get
        {
            if (_instance == null)
                _instance = new GameObject("[Offline] Screen Fade").AddComponent<OfflineScreenFade>();
            return _instance;
        }
    }

    private void Awake()
    {
        DontDestroyOnLoad(gameObject);

        _canvas = gameObject.AddComponent<Canvas>();
        _canvas.renderMode = RenderMode.WorldSpace;
        _canvas.sortingOrder = short.MaxValue;

        var rect = (RectTransform)transform;
        rect.sizeDelta = new Vector2(100f, 100f);

        var imageObject = new GameObject("Black", typeof(RectTransform));
        imageObject.transform.SetParent(transform, false);
        var imageRect = (RectTransform)imageObject.transform;
        imageRect.anchorMin = Vector2.zero;
        imageRect.anchorMax = Vector2.one;
        imageRect.offsetMin = Vector2.zero;
        imageRect.offsetMax = Vector2.zero;

        _image = imageObject.AddComponent<Image>();
        _image.raycastTarget = false;
        // Draw on top of everything, including hands and weapons close to the face.
        var material = new Material(Shader.Find("UI/Default"));
        material.SetInt("unity_GUIZTestMode", (int)CompareFunction.Always);
        _image.material = material;

        SetAlpha(0f);
    }

    private void LateUpdate()
    {
        if (_target == null || !_target.isActiveAndEnabled)
            _target = FindActiveCamera();

        if (_target == null)
            return;

        float distance = Mathf.Max(_target.nearClipPlane * 2f, 0.05f);
        transform.SetPositionAndRotation(
            _target.transform.position + _target.transform.forward * distance,
            _target.transform.rotation);
        // 100 units wide at 1cm scale = 1m square, far wider than the view at this distance.
        transform.localScale = Vector3.one * 0.01f * Mathf.Max(1f, distance * 10f);
    }

    private IEnumerator LoadRoutine(string sceneName)
    {
        if (_loading)
            yield break;

        _loading = true;
        yield return FadeTo(1f);
        AsyncOperation load = SceneManager.LoadSceneAsync(sceneName);
        while (load != null && !load.isDone)
            yield return null;
        _loading = false;
        // The new scene's OfflineSceneBootstrap fades back in once the rig is placed.
    }

    private IEnumerator FadeTo(float target)
    {
        float start = _alpha;
        float time = 0f;
        while (time < Duration)
        {
            time += Time.unscaledDeltaTime;
            SetAlpha(Mathf.Lerp(start, target, time / Duration));
            yield return null;
        }
        SetAlpha(target);
    }

    private void SetAlpha(float alpha)
    {
        _alpha = alpha;
        _image.color = new Color(0f, 0f, 0f, alpha);
        _canvas.enabled = alpha > 0.001f;
    }

    private static Camera FindActiveCamera()
    {
        Camera main = Camera.main;
        if (main != null && main.isActiveAndEnabled)
            return main;

        foreach (Camera camera in Camera.allCameras)
        {
            if (camera.stereoTargetEye != StereoTargetEyeMask.None)
                return camera;
        }

        return Camera.allCameras.Length > 0 ? Camera.allCameras[0] : null;
    }
}
