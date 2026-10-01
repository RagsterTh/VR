using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.UI;
using UnityEngine.XR.Interaction.Toolkit.Inputs.Haptics;

/// <summary>
/// Shared "juice" helpers for the offline mode: particle bursts, haptics, hit-stop, floating texts,
/// procedurally generated sprites and small tweens. Everything is created at runtime so no scene setup is needed.
/// </summary>
public sealed class OfflineFx : MonoBehaviour
{
    private static OfflineFx _host;
    private static Material _particleMaterial;
    private static Material _lineMaterial;
    private static Material _overlayMaterial;
    private static Sprite _circle;
    private static Sprite _ring;
    private static Sprite _soft;
    private static Sprite _white;
    private static Coroutine _hitStop;

    private static OfflineFx Host
    {
        get
        {
            if (_host == null)
            {
                var go = new GameObject("[Offline] Fx");
                DontDestroyOnLoad(go);
                _host = go.AddComponent<OfflineFx>();
            }
            return _host;
        }
    }

    public static Coroutine Run(IEnumerator routine) => Host.StartCoroutine(routine);

    // ---------- Materials & sprites ----------

    /// <summary>Additive URP particle material (Assets/Offline/Resources/OfflineFxParticle.mat).</summary>
    public static Material ParticleMaterial
    {
        get
        {
            if (_particleMaterial == null)
            {
                Material source = UnityEngine.Resources.Load<Material>("OfflineFxParticle");
                if (source == null)
                    return null;

                // The asset only provides the URP additive particle shader/keywords. Its texture is a lava sphere,
                // so use a generated white soft glow instead: the color then comes only from each effect.
                _particleMaterial = new Material(source) { name = "OfflineFxParticle (soft glow)" };
                Texture2D glow = SoftGlowTexture();
                if (_particleMaterial.HasProperty("_BaseMap")) _particleMaterial.SetTexture("_BaseMap", glow);
                if (_particleMaterial.HasProperty("_MainTex")) _particleMaterial.SetTexture("_MainTex", glow);
                if (_particleMaterial.HasProperty("_BaseColor")) _particleMaterial.SetColor("_BaseColor", Color.white);
            }
            return _particleMaterial;
        }
    }

    /// <summary>
    /// Additive material for LineRenderer/TrailRenderer: constant along the length, soft across the width.
    /// (The round glow texture would fade the beam out at both ends when stretched along a line.)
    /// </summary>
    public static Material LineMaterial
    {
        get
        {
            if (_lineMaterial == null && ParticleMaterial != null)
            {
                _lineMaterial = new Material(ParticleMaterial) { name = "OfflineFxParticle (beam)" };
                const int size = 32;
                var texture = new Texture2D(4, size, TextureFormat.RGBA32, false) { wrapMode = TextureWrapMode.Clamp };
                for (int y = 0; y < size; y++)
                {
                    float d = Mathf.Abs((y / (size - 1f)) * 2f - 1f);
                    float a = Mathf.Pow(Mathf.Clamp01(1f - d), 1.5f);
                    for (int x = 0; x < 4; x++)
                        texture.SetPixel(x, y, new Color(1f, 1f, 1f, a));
                }
                texture.Apply();
                if (_lineMaterial.HasProperty("_BaseMap")) _lineMaterial.SetTexture("_BaseMap", texture);
                if (_lineMaterial.HasProperty("_MainTex")) _lineMaterial.SetTexture("_MainTex", texture);
            }
            return _lineMaterial;
        }
    }

    private static Texture2D SoftGlowTexture()
    {
        const int size = 64;
        var texture = new Texture2D(size, size, TextureFormat.RGBA32, true) { wrapMode = TextureWrapMode.Clamp };
        float half = (size - 1) * 0.5f;
        for (int y = 0; y < size; y++)
        for (int x = 0; x < size; x++)
        {
            float d = Mathf.Sqrt((x - half) * (x - half) + (y - half) * (y - half)) / half;
            // Bright core with a soft falloff; fully transparent at the edge so quads never show.
            float a = Mathf.Pow(Mathf.Clamp01(1f - d), 2.2f);
            texture.SetPixel(x, y, new Color(1f, 1f, 1f, a));
        }
        texture.Apply(true);
        return texture;
    }

    /// <summary>UI material drawn on top of everything (HUD elements attached to the head).</summary>
    public static Material OverlayMaterial
    {
        get
        {
            if (_overlayMaterial == null)
            {
                _overlayMaterial = new Material(Shader.Find("UI/Default"));
                _overlayMaterial.SetInt("unity_GUIZTestMode", (int)CompareFunction.Always);
            }
            return _overlayMaterial;
        }
    }

    public static Sprite Circle => _circle != null ? _circle : _circle = MakeSprite(64, (d) => d < 0.95f ? 1f : Mathf.Clamp01((1f - d) * 20f));
    public static Sprite Ring => _ring != null ? _ring : _ring = MakeSprite(128, (d) => Mathf.Clamp01(1f - Mathf.Abs(d - 0.85f) * 12f));
    public static Sprite Soft => _soft != null ? _soft : _soft = MakeSprite(64, (d) => Mathf.Pow(Mathf.Clamp01(1f - d), 2f));
    public static Sprite White => _white != null ? _white : _white = MakeSprite(4, (d) => 1f);

    private static Sprite MakeSprite(int size, System.Func<float, float> alphaByDistance)
    {
        var texture = new Texture2D(size, size, TextureFormat.RGBA32, false) { wrapMode = TextureWrapMode.Clamp };
        float half = (size - 1) * 0.5f;
        for (int y = 0; y < size; y++)
        for (int x = 0; x < size; x++)
        {
            float d = Mathf.Sqrt((x - half) * (x - half) + (y - half) * (y - half)) / half;
            texture.SetPixel(x, y, new Color(1f, 1f, 1f, alphaByDistance(d)));
        }
        texture.Apply();
        return Sprite.Create(texture, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), size);
    }

    // ---------- Particles ----------

    public static ParticleSystem Burst(Vector3 position, Color color, int count = 24, float speed = 2f, float size = 0.05f,
        float lifetime = 0.6f, float gravity = 0f, float radius = 0.05f)
    {
        if (ParticleMaterial == null)
            return null;

        var go = new GameObject("[Offline] Burst");
        go.transform.position = position;
        var particles = go.AddComponent<ParticleSystem>();
        particles.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);

        var main = particles.main;
        main.playOnAwake = false;
        main.loop = false;
        main.duration = 0.1f;
        main.startLifetime = new ParticleSystem.MinMaxCurve(lifetime * 0.6f, lifetime);
        main.startSpeed = new ParticleSystem.MinMaxCurve(speed * 0.4f, speed);
        main.startSize = new ParticleSystem.MinMaxCurve(size * 0.5f, size);
        main.startColor = color;
        main.gravityModifier = gravity;
        main.simulationSpace = ParticleSystemSimulationSpace.World;
        main.maxParticles = count * 2;

        var emission = particles.emission;
        emission.rateOverTime = 0f;
        emission.SetBursts(new[] { new ParticleSystem.Burst(0f, (short)count) });

        var shape = particles.shape;
        shape.shapeType = ParticleSystemShapeType.Sphere;
        shape.radius = radius;

        var fade = particles.colorOverLifetime;
        fade.enabled = true;
        var gradient = new Gradient();
        gradient.SetKeys(
            new[] { new GradientColorKey(Color.white, 0f), new GradientColorKey(Color.white, 1f) },
            new[] { new GradientAlphaKey(1f, 0f), new GradientAlphaKey(0.8f, 0.5f), new GradientAlphaKey(0f, 1f) });
        fade.color = gradient;

        var shrink = particles.sizeOverLifetime;
        shrink.enabled = true;
        shrink.size = new ParticleSystem.MinMaxCurve(1f, AnimationCurve.EaseInOut(0f, 1f, 1f, 0f));

        var renderer = go.GetComponent<ParticleSystemRenderer>();
        renderer.sharedMaterial = ParticleMaterial;

        particles.Play();
        Destroy(go, lifetime + 0.5f);
        return particles;
    }

    /// <summary>Looping emitter in a box (ambient dust, hologram sparkles). <paramref name="drift"/> = constant velocity.</summary>
    public static ParticleSystem Emitter(Transform parent, Vector3 position, Vector3 boxSize, Color color, float rate,
        float lifetime, float size, Vector3 drift, float randomSpeed = 0.05f)
    {
        if (ParticleMaterial == null)
            return null;

        var go = new GameObject("[Offline] Emitter");
        go.transform.SetParent(parent, false);
        go.transform.position = position;
        var particles = go.AddComponent<ParticleSystem>();
        particles.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);

        var main = particles.main;
        main.loop = true;
        main.playOnAwake = true;
        main.duration = 5f;
        main.prewarm = true;
        main.startLifetime = new ParticleSystem.MinMaxCurve(lifetime * 0.6f, lifetime);
        main.startSpeed = new ParticleSystem.MinMaxCurve(0f, randomSpeed);
        main.startSize = new ParticleSystem.MinMaxCurve(size * 0.4f, size);
        main.startColor = color;
        main.simulationSpace = ParticleSystemSimulationSpace.World;
        main.maxParticles = Mathf.CeilToInt(rate * lifetime * 1.5f) + 10;

        var emission = particles.emission;
        emission.rateOverTime = rate;

        var shape = particles.shape;
        shape.shapeType = ParticleSystemShapeType.Box;
        shape.scale = boxSize;

        var velocity = particles.velocityOverLifetime;
        velocity.enabled = true;
        velocity.space = ParticleSystemSimulationSpace.World;
        velocity.x = drift.x;
        velocity.y = drift.y;
        velocity.z = drift.z;

        var fade = particles.colorOverLifetime;
        fade.enabled = true;
        var gradient = new Gradient();
        gradient.SetKeys(
            new[] { new GradientColorKey(Color.white, 0f), new GradientColorKey(Color.white, 1f) },
            new[] { new GradientAlphaKey(0f, 0f), new GradientAlphaKey(1f, 0.25f), new GradientAlphaKey(1f, 0.7f), new GradientAlphaKey(0f, 1f) });
        fade.color = gradient;

        go.GetComponent<ParticleSystemRenderer>().sharedMaterial = ParticleMaterial;
        particles.Play();
        return particles;
    }

    // ---------- Haptics ----------

    public static void HapticAll(float amplitude, float duration)
    {
        foreach (HapticImpulsePlayer haptic in FindObjectsByType<HapticImpulsePlayer>(FindObjectsSortMode.None))
            haptic.SendHapticImpulse(amplitude, duration);
    }

    /// <summary>Vibrates the controller that holds <paramref name="near"/> (e.g. the gun), or both if unknown.</summary>
    public static void HapticNear(Transform near, float amplitude, float duration)
    {
        HapticImpulsePlayer haptic = near != null ? near.GetComponentInParent<HapticImpulsePlayer>() : null;
        if (haptic != null)
            haptic.SendHapticImpulse(amplitude, duration);
        else
            HapticAll(amplitude, duration);
    }

    // ---------- Time ----------

    /// <summary>Tiny freeze that gives weight to a hit.</summary>
    public static void HitStop(float seconds = 0.05f)
    {
        if (_hitStop != null)
            return;
        _hitStop = Run(HitStopRoutine(seconds));
    }

    private static IEnumerator HitStopRoutine(float seconds)
    {
        Time.timeScale = 0.05f;
        yield return new WaitForSecondsRealtime(seconds);
        Time.timeScale = 1f;
        _hitStop = null;
    }

    // ---------- UI in the world ----------

    /// <summary>World-space canvas; 1 canvas unit = <paramref name="metersPerUnit"/> meters.</summary>
    public static RectTransform CreateCanvas(string name, Transform parent, Vector2 size, float metersPerUnit = 0.001f, int sortingOrder = 100)
    {
        var go = new GameObject(name, typeof(RectTransform));
        var rect = (RectTransform)go.transform;
        if (parent != null)
            rect.SetParent(parent, false);
        var canvas = go.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.WorldSpace;
        canvas.sortingOrder = sortingOrder;
        rect.sizeDelta = size;
        rect.localScale = Vector3.one * metersPerUnit;
        return rect;
    }

    public static Image AddImage(Transform parent, string name, Sprite sprite, Color color, Vector2 size, Vector2 position = default)
    {
        var go = new GameObject(name, typeof(RectTransform));
        var rect = (RectTransform)go.transform;
        rect.SetParent(parent, false);
        rect.sizeDelta = size;
        rect.anchoredPosition = position;
        var image = go.AddComponent<Image>();
        image.sprite = sprite;
        image.color = color;
        image.raycastTarget = false;
        return image;
    }

    public static TextMeshProUGUI AddText(Transform parent, string name, string text, float fontSize, Color color, Vector2 size, Vector2 position = default)
    {
        var go = new GameObject(name, typeof(RectTransform));
        var rect = (RectTransform)go.transform;
        rect.SetParent(parent, false);
        rect.sizeDelta = size;
        rect.anchoredPosition = position;
        var tmp = go.AddComponent<TextMeshProUGUI>();
        tmp.text = text;
        tmp.fontSize = fontSize;
        tmp.color = color;
        tmp.fontStyle = FontStyles.Bold;
        tmp.alignment = TextAlignmentOptions.Center;
        tmp.raycastTarget = false;
        tmp.outlineWidth = 0.2f;
        tmp.outlineColor = new Color32(0, 0, 0, 200);
        return tmp;
    }

    /// <summary>Text that pops, rises and fades out, always facing the player.</summary>
    public static void FloatingText(Vector3 position, string text, Color color, float fontSize = 90f, float lifetime = 1.1f, float rise = 0.25f)
    {
        RectTransform canvas = CreateCanvas("[Offline] Floating Text", null, new Vector2(900, 200), 0.001f, 200);
        canvas.position = position;
        canvas.gameObject.AddComponent<OfflineBillboard>();
        TextMeshProUGUI label = AddText(canvas, "Text", text, fontSize, color, new Vector2(900, 200));
        Run(FloatRoutine(canvas, label, lifetime, rise));
    }

    private static IEnumerator FloatRoutine(RectTransform canvas, TextMeshProUGUI label, float lifetime, float rise)
    {
        Vector3 start = canvas.position;
        Vector3 baseScale = canvas.localScale;
        float t = 0f;
        while (t < lifetime && canvas != null)
        {
            t += Time.unscaledDeltaTime;
            float k = t / lifetime;
            canvas.position = start + Vector3.up * (rise * EaseOut(k));
            canvas.localScale = baseScale * (k < 0.15f ? Mathf.Lerp(0.4f, 1.15f, k / 0.15f) : Mathf.Lerp(1.15f, 1f, Mathf.Clamp01((k - 0.15f) / 0.15f)));
            Color c = label.color;
            c.a = k < 0.7f ? 1f : 1f - (k - 0.7f) / 0.3f;
            label.color = c;
            yield return null;
        }
        if (canvas != null)
            Destroy(canvas.gameObject);
    }

    // ---------- Tweens ----------

    public static float EaseOut(float k) => 1f - Mathf.Pow(1f - Mathf.Clamp01(k), 3f);

    public static float EaseOutBack(float k)
    {
        k = Mathf.Clamp01(k);
        const float c1 = 1.70158f, c3 = c1 + 1f;
        return 1f + c3 * Mathf.Pow(k - 1f, 3f) + c1 * Mathf.Pow(k - 1f, 2f);
    }

    public static IEnumerator Punch(Transform target, float amount = 0.15f, float duration = 0.2f)
    {
        if (target == null)
            yield break;
        Vector3 baseScale = target.localScale;
        float t = 0f;
        while (t < duration && target != null)
        {
            t += Time.unscaledDeltaTime;
            float k = t / duration;
            target.localScale = baseScale * (1f + amount * Mathf.Sin(k * Mathf.PI));
            yield return null;
        }
        if (target != null)
            target.localScale = baseScale;
    }

    public static IEnumerator Shake(Transform target, float amount = 12f, float duration = 0.35f)
    {
        if (target == null)
            yield break;
        Vector3 basePosition = target.localPosition;
        float t = 0f;
        while (t < duration && target != null)
        {
            t += Time.unscaledDeltaTime;
            float falloff = 1f - t / duration;
            target.localPosition = basePosition + new Vector3(Mathf.Sin(t * 70f) * amount * falloff, 0f, 0f);
            yield return null;
        }
        if (target != null)
            target.localPosition = basePosition;
    }
}

/// <summary>Keeps a world-space UI facing the player's head.</summary>
public sealed class OfflineBillboard : MonoBehaviour
{
    private void LateUpdate()
    {
        Camera head = Camera.main;
        if (head != null)
            transform.rotation = Quaternion.LookRotation(transform.position - head.transform.position, Vector3.up);
    }
}
