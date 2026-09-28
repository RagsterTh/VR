using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Colored vignette on the edges of the player's view (damage flash, heal flash, low-life pulse).
/// A world-space quad just in front of the headset; screen-space overlays do not show in VR.
/// </summary>
public sealed class OfflineViewOverlay : MonoBehaviour
{
    private static OfflineViewOverlay _instance;

    private Image _image;
    private Camera _target;
    private Color _flashColor;
    private float _flash;
    private float _flashDuration = 0.3f;
    private bool _lowLife;

    public static OfflineViewOverlay Instance
    {
        get
        {
            if (_instance == null)
            {
                var go = new GameObject("[Offline] View Overlay", typeof(RectTransform));
                _instance = go.AddComponent<OfflineViewOverlay>();
            }
            return _instance;
        }
    }

    /// <summary>Colored flash on the edges of the view.</summary>
    public static void Flash(Color color, float strength = 0.8f, float duration = 0.35f)
    {
        OfflineViewOverlay overlay = Instance;
        overlay._flashColor = color;
        overlay._flash = Mathf.Max(overlay._flash, strength);
        overlay._flashDuration = duration;
    }

    public static void SetLowLife(bool lowLife)
    {
        // Turning it off must not create the overlay (it is called from OnDestroy while a scene unloads).
        if (_instance == null && !lowLife)
            return;
        Instance._lowLife = lowLife;
    }

    private void Awake()
    {
        var canvas = gameObject.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.WorldSpace;
        canvas.sortingOrder = short.MaxValue - 1;
        ((RectTransform)transform).sizeDelta = new Vector2(100f, 100f);

        var vignette = new GameObject("Vignette", typeof(RectTransform));
        vignette.transform.SetParent(transform, false);
        var rect = (RectTransform)vignette.transform;
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;
        _image = vignette.AddComponent<Image>();
        _image.sprite = VignetteSprite();
        _image.material = OfflineFx.OverlayMaterial;
        _image.raycastTarget = false;
        _image.color = Color.clear;
    }

    private void LateUpdate()
    {
        if (_target == null || !_target.isActiveAndEnabled)
            _target = Camera.main;
        if (_target == null)
            return;

        float distance = Mathf.Max(_target.nearClipPlane * 2.5f, 0.06f);
        transform.SetPositionAndRotation(_target.transform.position + _target.transform.forward * distance, _target.transform.rotation);
        transform.localScale = Vector3.one * 0.01f * Mathf.Max(1f, distance * 12f);

        _flash = Mathf.MoveTowards(_flash, 0f, Time.unscaledDeltaTime / Mathf.Max(0.05f, _flashDuration));
        float lowPulse = _lowLife ? 0.25f + 0.2f * Mathf.Sin(Time.unscaledTime * 5f) : 0f;

        Color low = new Color(0.9f, 0.05f, 0.05f, lowPulse);
        Color flash = new Color(_flashColor.r, _flashColor.g, _flashColor.b, _flash);
        Color result = flash.a >= low.a ? flash : low;
        _image.color = result;
        _image.enabled = result.a > 0.01f;
    }

    private static Sprite VignetteSprite()
    {
        const int size = 128;
        var texture = new Texture2D(size, size, TextureFormat.RGBA32, false) { wrapMode = TextureWrapMode.Clamp };
        float half = (size - 1) * 0.5f;
        for (int y = 0; y < size; y++)
        for (int x = 0; x < size; x++)
        {
            float d = Mathf.Sqrt((x - half) * (x - half) + (y - half) * (y - half)) / half;
            // Clear center, strong edges.
            texture.SetPixel(x, y, new Color(1f, 1f, 1f, Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(0.35f, 1f, d))));
        }
        texture.Apply();
        return Sprite.Create(texture, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), size);
    }
}
