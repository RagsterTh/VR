using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using TMPro;
using UnityEngine;
using UnityEngine.Playables;
using UnityEngine.UI;

/// <summary>
/// Offline "juice" for the GloboV2 lobby: living Eve hologram (sparkles, floor glow, glitches, typed subtitles),
/// holographic sweep when the walls fall, an epic globe entrance with halo and orbit ring, glowing map spots,
/// ambient dust, breathing lights and a welcome title. Created by OfflineSceneBootstrap when the map choice exists.
/// </summary>
public sealed class OfflineLobbyFeedback : MonoBehaviour
{
    private static readonly Color Holo = new(0.35f, 0.85f, 1f);

    [SerializeField] private string _title = "SENAC PALHOÇA";
    [SerializeField] private float _wallsFallTime = 14.5f;

    private PlayableDirector _eveDirector;
    private Transform _eve;
    private Renderer[] _eveRenderers;
    private List<string> _lines;
    private float _voiceLength = 11.8f;
    private Transform _lobbyRoot;
    private GameObject _globe;
    private Transform _earth;
    private readonly List<(Light light, float baseIntensity)> _lights = new();

    private RectTransform _subtitle;
    private TextMeshProUGUI _subtitleText;
    private RectTransform _titleCanvas;
    private RectTransform _eveGlow;
    private ParticleSystem _eveSparkles;
    private bool _wallsFell;
    private bool _globeShown;
    private float _nextGlitch;

    private IEnumerator Start()
    {
        yield return null;

        foreach (GameObject root in gameObject.scene.GetRootGameObjects())
            if (root.name == "Lobby/Globe")
                _lobbyRoot = root.transform;
        if (_lobbyRoot == null)
            yield break;

        foreach (PlayableDirector director in FindObjectsByType<PlayableDirector>(FindObjectsSortMode.None))
            if (director.playableAsset != null && director.playableAsset.name.Contains("EVE"))
                _eveDirector = director;

        _eve = _lobbyRoot.Find("Lobby/Sala/Bancadas/Bancada1/TimelineEVE/EVE");
        if (_eve != null)
        {
            _eveRenderers = _eve.GetComponentsInChildren<Renderer>(true);
            var voice = _eve.GetComponent<AudioSource>();
            if (voice != null && voice.clip != null)
                _voiceLength = voice.clip.length;
        }

        Balcony balcony = _lobbyRoot.Find("Lobby/Sala/Bancadas/Bancada1")?.GetComponent<Balcony>();
        if (balcony != null)
            _lines = typeof(Balcony).GetField("_dialogueLines", BindingFlags.NonPublic | BindingFlags.Instance)?.GetValue(balcony) as List<string>;

        var selection = FindAnyObjectByType<OfflineMapSelection>();
        if (selection != null)
            _globe = typeof(OfflineMapSelection).GetField("_globe", BindingFlags.NonPublic | BindingFlags.Instance)?.GetValue(selection) as GameObject;
        if (_globe != null)
            _earth = _globe.transform.Find("table/Globo_Hollo");

        foreach (string room in new[] { "Lobby/Sala", "Lobby/Sala Holo" })
        {
            Transform t = _lobbyRoot.Find(room);
            if (t == null) continue;
            foreach (Light light in t.GetComponentsInChildren<Light>(true))
                _lights.Add((light, light.intensity));
        }

        BuildAmbient();
        BuildEve();
        BuildMapSpots(selection);
    }

    // ---------- Ambient ----------

    private void BuildAmbient()
    {
        Transform sala = _lobbyRoot.Find("Lobby/Sala");
        if (sala == null)
            return;

        Bounds bounds = RendererBounds(sala);
        OfflineFx.Emitter(transform, bounds.center, new Vector3(bounds.size.x * 0.8f, bounds.size.y * 0.8f, bounds.size.z * 0.8f),
            new Color(0.6f, 0.9f, 1f, 0.5f), 30f, 9f, 0.025f, new Vector3(0f, 0.03f, 0f), 0.04f);

        _titleCanvas = OfflineFx.CreateCanvas("[Offline] Lobby Title", null, new Vector2(2400, 400), 0.002f, 90);
        _titleCanvas.position = new Vector3(bounds.center.x, bounds.max.y - 0.5f, bounds.center.z);
        _titleCanvas.gameObject.AddComponent<OfflineBillboard>();
        OfflineFx.AddImage(_titleCanvas, "Glow", OfflineFx.Soft, new Color(Holo.r, Holo.g, Holo.b, 0.25f), new Vector2(2200, 500));
        OfflineFx.AddText(_titleCanvas, "Title", _title, 200f, new Color(0.75f, 0.95f, 1f), new Vector2(2400, 300));
    }

    // ---------- Eve ----------

    private void BuildEve()
    {
        if (_eve == null)
            return;

        Vector3 feet = _eve.position;
        _eveSparkles = OfflineFx.Emitter(_eve, feet + Vector3.up * 0.1f, new Vector3(0.9f, 0.1f, 0.9f),
            new Color(Holo.r, Holo.g, Holo.b, 0.9f), 18f, 2.5f, 0.035f, new Vector3(0f, 0.55f, 0f), 0.08f);

        _eveGlow = OfflineFx.CreateCanvas("[Offline] Eve Floor Glow", _eve, new Vector2(1600, 1600), 0.001f, 80);
        _eveGlow.position = feet + Vector3.up * 0.02f;
        _eveGlow.rotation = Quaternion.Euler(90f, 0f, 0f);
        OfflineFx.AddImage(_eveGlow, "Glow", OfflineFx.Soft, new Color(Holo.r, Holo.g, Holo.b, 0.5f), new Vector2(1600, 1600));
        OfflineFx.AddImage(_eveGlow, "Ring", OfflineFx.Ring, new Color(Holo.r, Holo.g, Holo.b, 0.8f), new Vector2(1100, 1100));

        _subtitle = OfflineFx.CreateCanvas("[Offline] Eve Subtitle", null, new Vector2(1400, 260), 0.0012f, 150);
        _subtitle.position = feet + Vector3.up * 2.45f;
        _subtitle.gameObject.AddComponent<OfflineBillboard>();
        Image back = OfflineFx.AddImage(_subtitle, "Back", OfflineFx.White, new Color(0.03f, 0.08f, 0.14f, 0.75f), new Vector2(1400, 200));
        back.material = OfflineFx.OverlayMaterial;
        _subtitleText = OfflineFx.AddText(_subtitle, "Text", string.Empty, 64f, new Color(0.8f, 0.97f, 1f), new Vector2(1320, 200));
        _subtitleText.gameObject.AddComponent<OfflineTypewriter>();
        _subtitle.gameObject.SetActive(false);

        _nextGlitch = Time.time + Random.Range(2f, 4f);
    }

    private void UpdateEve()
    {
        if (_eve == null)
            return;

        bool visible = _eve.gameObject.activeInHierarchy;
        float t = Time.time;
        if (_eveGlow != null)
        {
            float pulse = 0.5f + 0.5f * Mathf.Sin(t * 2.5f);
            _eveGlow.localScale = Vector3.one * 0.001f * (0.9f + 0.15f * pulse);
        }

        // Holographic glitch: the model blinks off for a couple of frames now and then.
        if (visible && _eveRenderers != null && t >= _nextGlitch)
        {
            _nextGlitch = t + Random.Range(2.5f, 5f);
            StartCoroutine(Glitch());
        }

        // Subtitles follow the voice line by line, then fade away.
        if (_eveDirector != null && _lines != null && _lines.Count > 0 && _subtitle != null)
        {
            double time = _eveDirector.time;
            bool talking = _eveDirector.state == PlayState.Playing && time < _voiceLength + 1.5;
            _subtitle.gameObject.SetActive(talking && visible);
            if (talking)
            {
                int index = Mathf.Clamp((int)(time / (_voiceLength / _lines.Count)), 0, _lines.Count - 1);
                string line = _lines[index];
                if (_subtitleText.text != line)
                    _subtitleText.text = line;
            }

            if (!_wallsFell && _eveDirector.state == PlayState.Playing && time >= _wallsFallTime)
            {
                _wallsFell = true;
                StartCoroutine(WallsSweep());
            }
        }
    }

    private IEnumerator Glitch()
    {
        // Remember which renderers were on: some start disabled on purpose.
        var wasOn = new bool[_eveRenderers.Length];
        for (int i = 0; i < _eveRenderers.Length; i++)
            wasOn[i] = _eveRenderers[i] != null && _eveRenderers[i].enabled;

        for (int blink = 0; blink < 3; blink++)
        {
            for (int i = 0; i < _eveRenderers.Length; i++)
                if (wasOn[i] && _eveRenderers[i] != null) _eveRenderers[i].enabled = false;
            yield return new WaitForSeconds(Random.Range(0.02f, 0.06f));
            for (int i = 0; i < _eveRenderers.Length; i++)
                if (wasOn[i] && _eveRenderers[i] != null) _eveRenderers[i].enabled = true;
            yield return new WaitForSeconds(Random.Range(0.03f, 0.08f));
        }
    }

    private IEnumerator WallsSweep()
    {
        OfflineViewOverlay.Flash(Holo, 0.35f, 0.8f);
        OfflineFx.HapticAll(0.3f, 0.2f);
        Transform walls = _lobbyRoot.Find("Lobby/Sala Holo/SalaV2");
        if (walls == null)
            yield break;

        foreach (Transform wall in walls)
        {
            Bounds b = RendererBounds(wall);
            if (b.size == Vector3.zero)
                continue;
            for (int i = 0; i < 4; i++)
            {
                Vector3 p = b.center + new Vector3(Random.Range(-b.extents.x, b.extents.x), Random.Range(-b.extents.y, b.extents.y), Random.Range(-b.extents.z, b.extents.z)) * 0.8f;
                OfflineFx.Burst(p, Holo, 30, 1.5f, 0.06f, 1.2f, -0.1f, 0.4f);
            }
            yield return new WaitForSeconds(0.15f);
        }
    }

    // ---------- Globe ----------

    private void UpdateGlobe()
    {
        if (_globeShown || _globe == null || !_globe.activeInHierarchy)
            return;
        _globeShown = true;
        StartCoroutine(GlobeEntrance());
    }

    private IEnumerator GlobeEntrance()
    {
        if (_titleCanvas != null)
            StartCoroutine(FadeOut(_titleCanvas, 0.8f));

        Transform globe = _globe.transform;
        Vector3 baseScale = globe.localScale;
        Vector3 center = _earth != null ? RendererBounds(_earth).center : globe.position + Vector3.up * 3f;
        Bounds earthBounds = _earth != null ? RendererBounds(_earth) : new Bounds(center, Vector3.one * 4f);
        float radius = Mathf.Max(earthBounds.extents.x, earthBounds.extents.z);

        OfflineFx.Burst(center, Color.white, 60, 6f, 0.1f, 0.7f, 0f, radius * 0.3f);
        OfflineFx.Burst(center, Holo, 120, 4f, 0.08f, 1.4f, 0f, radius * 0.5f);
        OfflineViewOverlay.Flash(Holo, 0.5f, 0.9f);
        OfflineFx.HapticAll(0.7f, 0.3f);

        // Shockwave on the floor.
        RectTransform wave = OfflineFx.CreateCanvas("[Offline] Globe Shockwave", null, new Vector2(1000, 1000), 0.001f, 90);
        wave.position = new Vector3(center.x, globe.position.y + 0.05f, center.z);
        wave.rotation = Quaternion.Euler(90f, 0f, 0f);
        Image waveRing = OfflineFx.AddImage(wave, "Ring", OfflineFx.Ring, Holo, new Vector2(1000, 1000));
        StartCoroutine(Shockwave(wave, waveRing, radius * 4f));

        float t = 0f;
        while (t < 0.9f)
        {
            t += Time.deltaTime;
            globe.localScale = baseScale * Mathf.Max(0.01f, OfflineFx.EaseOutBack(t / 0.9f));
            yield return null;
        }
        globe.localScale = baseScale;

        // Holographic halo + orbit ring around the earth.
        RectTransform halo = OfflineFx.CreateCanvas("[Offline] Globe Halo", null, new Vector2(1000, 1000), radius * 0.0032f, 70);
        halo.position = center;
        halo.gameObject.AddComponent<OfflineBillboard>();
        Image haloImage = OfflineFx.AddImage(halo, "Halo", OfflineFx.Soft, new Color(Holo.r, Holo.g, Holo.b, 0.25f), new Vector2(1000, 1000));

        RectTransform orbit = OfflineFx.CreateCanvas("[Offline] Globe Orbit", null, new Vector2(1000, 1000), radius * 0.0026f, 71);
        orbit.position = center;
        orbit.rotation = Quaternion.Euler(80f, 0f, 0f);
        Image orbitRing = OfflineFx.AddImage(orbit, "Orbit", OfflineFx.Ring, new Color(Holo.r, Holo.g, Holo.b, 0.6f), new Vector2(1000, 1000));
        Image moon = OfflineFx.AddImage(orbit, "Satellite", OfflineFx.Circle, Color.white, new Vector2(40, 40), new Vector2(0f, 460f));

        OfflineFx.Emitter(transform, center, new Vector3(radius * 2f, radius * 2f, radius * 2f), new Color(Holo.r, Holo.g, Holo.b, 0.7f),
            25f, 3f, 0.05f, new Vector3(0f, 0.15f, 0f), 0.1f);

        while (halo != null)
        {
            float pulse = 0.5f + 0.5f * Mathf.Sin(Time.time * 2f);
            haloImage.color = new Color(Holo.r, Holo.g, Holo.b, 0.15f + 0.15f * pulse);
            orbit.Rotate(0f, 0f, 12f * Time.deltaTime, Space.Self);
            orbitRing.color = new Color(Holo.r, Holo.g, Holo.b, 0.35f + 0.25f * pulse);
            moon.rectTransform.localScale = Vector3.one * (0.8f + 0.4f * pulse);
            yield return null;
        }
    }

    private static IEnumerator Shockwave(RectTransform wave, Image ring, float maxRadius)
    {
        float t = 0f;
        while (t < 1.2f)
        {
            t += Time.deltaTime;
            float k = t / 1.2f;
            wave.localScale = Vector3.one * 0.001f * Mathf.Lerp(0.2f, maxRadius * 2f, OfflineFx.EaseOut(k));
            ring.color = new Color(Holo.r, Holo.g, Holo.b, 1f - k);
            yield return null;
        }
        Destroy(wave.gameObject);
    }

    // ---------- Map spots ----------

    private void BuildMapSpots(OfflineMapSelection selection)
    {
        if (selection == null)
            return;
        var maps = typeof(OfflineMapSelection).GetField("_maps", BindingFlags.NonPublic | BindingFlags.Instance)?.GetValue(selection) as OfflineMapSelection.MapButton[];
        if (maps == null)
            return;

        foreach (OfflineMapSelection.MapButton map in maps)
        {
            if (map.Button == null)
                continue;
            var spot = map.Button.gameObject.AddComponent<OfflineMapSpot>();
            spot.Label = MapLabel(map.Scene);
        }
    }

    public static string MapLabel(string scene) => scene switch
    {
        OfflineSession.CambirelaScene => "CAMBIRELA",
        OfflineSession.GuardaScene => "GUARDA DO EMBAÚ",
        OfflineSession.PedraBrancaScene => "PEDRA BRANCA",
        _ => scene,
    };

    /// <summary>Called by the map choice before loading: the spot bursts into light and the view "warps".</summary>
    public static IEnumerator MapChosen(Button button)
    {
        Vector3 at = button != null ? button.transform.position : Vector3.zero;
        if (button != null)
            OfflineFx.Run(OfflineFx.Punch(button.transform, 0.4f, 0.3f));
        OfflineFx.Burst(at, Color.white, 50, 3f, 0.05f, 0.6f);
        OfflineFx.Burst(at, Holo, 80, 2f, 0.04f, 1f);
        OfflineFx.HapticAll(0.8f, 0.25f);

        Camera head = Camera.main;
        if (head != null)
        {
            // Particles rushing past the player: the "teleport".
            for (int i = 0; i < 4; i++)
            {
                Vector3 p = head.transform.position + head.transform.forward * (3f + i);
                OfflineFx.Burst(p, i % 2 == 0 ? Holo : Color.white, 60, 8f, 0.04f, 0.5f, 0f, 1.5f);
            }
        }

        float t = 0f;
        while (t < 0.7f)
        {
            t += Time.unscaledDeltaTime;
            OfflineViewOverlay.Flash(Color.white, Mathf.Lerp(0.2f, 1f, t / 0.7f), 0.3f);
            yield return null;
        }
    }

    // ---------- Loop ----------

    private void Update()
    {
        UpdateEve();
        UpdateGlobe();

        float time = Time.time;
        for (int i = 0; i < _lights.Count; i++)
        {
            var (light, baseIntensity) = _lights[i];
            if (light != null)
                light.intensity = baseIntensity * (1f + 0.15f * Mathf.Sin(time * 0.8f + i * 1.3f));
        }

        if (_titleCanvas != null)
            _titleCanvas.position += Vector3.up * (Mathf.Sin(time * 0.8f) * 0.0015f);
    }

    // ---------- Helpers ----------

    private static IEnumerator FadeOut(RectTransform canvas, float duration)
    {
        Graphic[] graphics = canvas.GetComponentsInChildren<Graphic>();
        float t = 0f;
        while (t < duration && canvas != null)
        {
            t += Time.deltaTime;
            foreach (Graphic g in graphics)
            {
                Color c = g.color;
                c.a *= 1f - t / duration;
                g.color = c;
            }
            yield return null;
        }
        if (canvas != null)
            Destroy(canvas.gameObject);
    }

    private static Bounds RendererBounds(Transform root)
    {
        Renderer[] renderers = root.GetComponentsInChildren<Renderer>();
        if (renderers.Length == 0)
            return new Bounds(root.position, Vector3.zero);
        Bounds b = renderers[0].bounds;
        foreach (Renderer r in renderers)
            b.Encapsulate(r.bounds);
        return b;
    }
}

/// <summary>A map hotspot on the globe panel: pulsing glow, ring and the place name; grows when pointed at.</summary>
public sealed class OfflineMapSpot : MonoBehaviour, UnityEngine.EventSystems.IPointerEnterHandler, UnityEngine.EventSystems.IPointerExitHandler
{
    public string Label;

    private Image _glow;
    private Image _ring;
    private TextMeshProUGUI _label;
    private float _hover;

    private void Start()
    {
        var rect = (RectTransform)transform;
        float size = Mathf.Min(rect.rect.width, rect.rect.height);
        _glow = OfflineFx.AddImage(transform, "Spot Glow", OfflineFx.Soft, new Color(0.35f, 0.85f, 1f, 0.4f), new Vector2(size * 1.6f, size * 1.6f));
        _glow.transform.SetAsFirstSibling();
        _ring = OfflineFx.AddImage(transform, "Spot Ring", OfflineFx.Ring, new Color(0.35f, 0.85f, 1f, 0.9f), new Vector2(size, size));
        _label = OfflineFx.AddText(transform, "Spot Label", Label, size * 0.28f, Color.white, new Vector2(rect.rect.width * 1.4f, size * 0.4f), new Vector2(0f, -size * 0.65f));
    }

    public void OnPointerEnter(UnityEngine.EventSystems.PointerEventData eventData) => _hover = 1f;
    public void OnPointerExit(UnityEngine.EventSystems.PointerEventData eventData) => _hover = 0f;

    private void Update()
    {
        if (_ring == null)
            return;
        float t = Time.unscaledTime;
        float cycle = Mathf.Repeat(t * 0.8f, 1f);
        float boost = 1f + 0.35f * _hover;
        _ring.rectTransform.localScale = Vector3.one * Mathf.Lerp(0.7f, 1.3f, cycle) * boost;
        _ring.color = new Color(0.35f, 0.85f, 1f, (1f - cycle) * 0.9f);
        _glow.color = new Color(0.35f, 0.85f, 1f, (0.25f + 0.2f * Mathf.Sin(t * 3f)) * boost);
        _label.color = new Color(1f, 1f, 1f, 0.75f + 0.25f * _hover);
    }
}
