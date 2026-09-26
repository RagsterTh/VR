using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using UnityEngine.XR.Interaction.Toolkit.Inputs.Haptics;

/// <summary>Small tactile and visual acknowledgement for world-space VR buttons.</summary>
[RequireComponent(typeof(Button))]
public sealed class OfflineButtonFeedback : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IPointerClickHandler
{
    [SerializeField] private AudioClip _clickClip;
    private Button _button;
    private RectTransform _rect;
    private Vector3 _baseScale;
    private AudioSource _audio;
    private static AudioClip _fallbackClick;

    private void Awake()
    {
        _button = GetComponent<Button>();
        _rect = transform as RectTransform;
        if (_rect != null)
            _baseScale = _rect.localScale;
        if (_clickClip == null)
            _clickClip = GetFallbackClick();
        _audio = gameObject.AddComponent<AudioSource>();
        _audio.playOnAwake = false;
        _audio.spatialBlend = 0f;
        _audio.volume = 0.3f;
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        if (_button == null || !_button.IsInteractable())
            return;
        if (_rect != null)
            _rect.localScale = _baseScale * 1.035f;
        Pulse(0.16f, 0.045f);
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        if (_rect != null)
            _rect.localScale = _baseScale;
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        if (_button == null || !_button.IsInteractable())
            return;
        if (_rect != null)
            _rect.localScale = _baseScale;
        if (_audio != null)
            _audio.PlayOneShot(_clickClip);
        Pulse(0.35f, 0.08f);
    }

    private static void Pulse(float amplitude, float duration)
    {
        foreach (HapticImpulsePlayer haptic in FindObjectsByType<HapticImpulsePlayer>(FindObjectsInactive.Include, FindObjectsSortMode.None))
        {
            if (haptic != null && haptic.isActiveAndEnabled)
                haptic.SendHapticImpulse(amplitude, duration);
        }
    }

    private static AudioClip GetFallbackClick()
    {
        if (_fallbackClick != null)
            return _fallbackClick;
        const int sampleRate = 24000;
        const int sampleCount = 1800;
        float[] samples = new float[sampleCount];
        for (int i = 0; i < sampleCount; i++)
        {
            float t = i / (float)sampleRate;
            float envelope = 1f - i / (float)sampleCount;
            samples[i] = Mathf.Sin(t * 880f * Mathf.PI * 2f) * envelope * envelope * 0.16f;
        }
        _fallbackClick = AudioClip.Create("Offline UI Click", sampleCount, 1, sampleRate, false);
        _fallbackClick.SetData(samples, 0);
        return _fallbackClick;
    }
}
