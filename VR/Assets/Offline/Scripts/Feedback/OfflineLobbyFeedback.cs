using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using TMPro;
using UnityEngine;
using UnityEngine.Playables;
using UnityEngine.UI;

/// <summary>
/// Offline "juice" for the GloboV2 lobby: living Eve hologram (sparkles, floor glow, typed subtitles),
/// holographic sweep when the walls fall, an epic globe entrance with halo and orbit ring, glowing map spots,
/// ambient dust and breathing lights. Created by OfflineSceneBootstrap when the map choice exists.
/// </summary>
public sealed class OfflineLobbyFeedback : MonoBehaviour
{
    private static readonly Color Holo = new(0.35f, 0.85f, 1f);

    [SerializeField] private float _wallsFallTime = 14.5f;

    [Tooltip("Subtitles of Eve's intro (\"Audio 01 - Hall de Entrada\"). Each line stays on screen for a share of the " +
             "audio proportional to its length.")]
    [TextArea]
    [SerializeField] private string[] _eveLines =
    {
        "Olá, que bom te ver no Senac!",
        "Meu nome é Eve e vou te apresentar os cursos que temos disponíveis no nosso portfólio,",
        "para que você possa escolher de acordo com as suas necessidades.",
    };

    private PlayableDirector _eveDirector;
    private Transform _eve;
    private List<string> _lines;
    private float _voiceLength = 11.8f;
    private Transform _lobbyRoot;
    private GameObject _globe;
    private Transform _earth;
    private readonly List<(Light light, float baseIntensity)> _lights = new();

    private RectTransform _subtitle;
    private TextMeshProUGUI _subtitleText;
    private RectTransform _eveGlow;
    private ParticleSystem _eveSparkles;
    private bool _wallsFell;
    private bool _globeShown;

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

        // The voice is played by the timeline's audio track (the AudioSource has no clip of its own).
        if (_eveDirector != null && _eveDirector.playableAsset is UnityEngine.Timeline.TimelineAsset timeline)
        {
            foreach (var track in timeline.GetOutputTracks())
            {
                if (track is not UnityEngine.Timeline.AudioTrack)
                    continue;
                foreach (var clip in track.GetClips())
                    _voiceLength = Mathf.Max(0.1f, (float)clip.end);
            }
        }

        _lines = new List<string>();
        if (_eveLines != null)
            foreach (string line in _eveLines)
                if (!string.IsNullOrWhiteSpace(line))
                    _lines.Add(line.Trim());

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
    }

    // ---------- Eve ----------

    private void BuildEve()
    {
        if (_eve == null)
            return;

        Vector3 feet = _eve.position;

        var animator = _eve.gameObject.AddComponent<OfflineEveAnimator>();
        animator.Director = _eveDirector;

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

        // Subtitles follow the voice line by line, then fade away.
        if (_eveDirector != null && _lines != null && _lines.Count > 0 && _subtitle != null)
        {
            double time = _eveDirector.time;
            bool talking = _eveDirector.state == PlayState.Playing && time < _voiceLength + 1.5;
            _subtitle.gameObject.SetActive(talking && visible);
            if (talking)
            {
                string line = LineAt((float)time);
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

    /// <summary>Line spoken at <paramref name="time"/>: each line gets a share of the voice proportional to its length.</summary>
    private string LineAt(float time)
    {
        int totalChars = 0;
        foreach (string line in _lines)
            totalChars += line.Length;

        float elapsed = 0f;
        foreach (string line in _lines)
        {
            elapsed += _voiceLength * line.Length / Mathf.Max(1, totalChars);
            if (time < elapsed)
                return line;
        }
        return _lines[_lines.Count - 1];
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
            map.Button.gameObject.AddComponent<OfflineMapSpot>();
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

/// <summary>
/// A map hotspot on the globe panel (the place name is already printed on the map picture).
/// Idle: a soft glow breathes behind the name so it reads as clickable. While pointed at ("hold"): the glow
/// lights up, the spot grows a little and the name gives off energy: sparks rising from it and expanding waves.
/// </summary>
public sealed class OfflineMapSpot : MonoBehaviour, UnityEngine.EventSystems.IPointerEnterHandler, UnityEngine.EventSystems.IPointerExitHandler
{
    private static readonly Color Energy = new(0.35f, 0.85f, 1f);
    private const int SparkCount = 28;
    private const int WaveCount = 3;

    private RectTransform _rect;
    private Image _glow;
    private readonly Image[] _sparks = new Image[SparkCount];
    private readonly Vector2[] _sparkStart = new Vector2[SparkCount];
    private readonly Vector2[] _sparkVelocity = new Vector2[SparkCount];
    private readonly float[] _sparkLife = new float[SparkCount];
    private readonly float[] _sparkDuration = new float[SparkCount];
    private readonly Image[] _waves = new Image[WaveCount];
    private readonly float[] _waveLife = new float[WaveCount];
    private Vector3 _baseScale;
    private bool _pointed;
    private float _hover;
    private float _nextSpark;
    private float _nextWave;
    private int _sparkIndex;
    private int _waveIndex;

    private void Start()
    {
        _rect = (RectTransform)transform;
        _baseScale = _rect.localScale;
        Vector2 size = _rect.rect.size;

        _glow = OfflineFx.AddImage(transform, "Spot Glow", OfflineFx.Soft, new Color(Energy.r, Energy.g, Energy.b, 0.2f), new Vector2(size.x * 1.25f, size.y * 1.5f));
        _glow.transform.SetAsFirstSibling();

        for (int i = 0; i < WaveCount; i++)
        {
            _waves[i] = OfflineFx.AddImage(transform, "Spot Wave", OfflineFx.Ring, Color.clear, new Vector2(size.y, size.y));
            _waves[i].enabled = false;
            _waveLife[i] = 1f;
        }
        for (int i = 0; i < SparkCount; i++)
        {
            _sparks[i] = OfflineFx.AddImage(transform, "Spot Spark", OfflineFx.Soft, Color.clear, Vector2.one * 16f);
            _sparks[i].enabled = false;
            _sparkLife[i] = 1f;
            _sparkDuration[i] = 1f;
        }
    }

    public void OnPointerEnter(UnityEngine.EventSystems.PointerEventData eventData) => _pointed = true;
    public void OnPointerExit(UnityEngine.EventSystems.PointerEventData eventData) => _pointed = false;

    private void OnDisable()
    {
        _pointed = false;
    }

    private void Update()
    {
        if (_glow == null)
            return;

        float dt = Time.unscaledDeltaTime;
        float t = Time.unscaledTime;
        _hover = Mathf.MoveTowards(_hover, _pointed ? 1f : 0f, dt * 6f);
        Vector2 size = _rect.rect.size;

        // Glow: breathing when idle, bright while pointed at.
        float breathe = 0.5f + 0.5f * Mathf.Sin(t * 2.2f);
        _glow.color = new Color(Energy.r, Energy.g, Energy.b, Mathf.Lerp(0.12f + 0.12f * breathe, 0.65f, _hover));
        _glow.rectTransform.localScale = Vector3.one * (1f + 0.06f * breathe + 0.15f * _hover);
        _rect.localScale = _baseScale * (1f + 0.07f * OfflineFx.EaseOut(_hover));

        if (_pointed)
        {
            if (t >= _nextSpark)
            {
                _nextSpark = t + 0.035f;
                SpawnSpark(size);
            }
            if (t >= _nextWave)
            {
                _nextWave = t + 0.45f;
                _waveLife[_waveIndex] = 0f;
                _waveIndex = (_waveIndex + 1) % WaveCount;
            }
        }

        for (int i = 0; i < SparkCount; i++)
        {
            if (_sparkLife[i] >= 1f)
            {
                _sparks[i].enabled = false;
                continue;
            }
            _sparkLife[i] += dt / _sparkDuration[i];
            float k = Mathf.Clamp01(_sparkLife[i]);
            RectTransform spark = _sparks[i].rectTransform;
            // Rise fast, slow down, sway a little.
            spark.anchoredPosition = _sparkStart[i] + _sparkVelocity[i] * OfflineFx.EaseOut(k) + new Vector2(Mathf.Sin((t + i) * 9f) * 6f, 0f);
            spark.localScale = Vector3.one * Mathf.Lerp(1.3f, 0.3f, k);
            Color c = i % 3 == 0 ? Color.white : Energy;
            c.a = (k < 0.15f ? k / 0.15f : 1f - (k - 0.15f) / 0.85f) * 0.95f;
            _sparks[i].color = c;
            _sparks[i].enabled = true;
        }

        for (int i = 0; i < WaveCount; i++)
        {
            if (_waveLife[i] >= 1f)
            {
                _waves[i].enabled = false;
                continue;
            }
            _waveLife[i] += dt / 0.9f;
            float k = Mathf.Clamp01(_waveLife[i]);
            // Stretched ring: follows the wide shape of the name.
            _waves[i].rectTransform.sizeDelta = new Vector2(size.x, size.y) * Mathf.Lerp(0.6f, 1.7f, OfflineFx.EaseOut(k));
            _waves[i].color = new Color(Energy.r, Energy.g, Energy.b, (1f - k) * 0.7f);
            _waves[i].enabled = true;
        }
    }

    private void SpawnSpark(Vector2 size)
    {
        int i = _sparkIndex;
        _sparkIndex = (_sparkIndex + 1) % SparkCount;
        _sparkStart[i] = new Vector2(Random.Range(-0.45f, 0.45f) * size.x, Random.Range(-0.25f, 0.2f) * size.y);
        _sparkVelocity[i] = new Vector2(Random.Range(-0.08f, 0.08f) * size.x, Random.Range(0.45f, 1.1f) * size.y);
        _sparkDuration[i] = Random.Range(0.5f, 0.9f);
        _sparkLife[i] = 0f;
        float px = Mathf.Max(10f, size.y * Random.Range(0.06f, 0.13f));
        _sparks[i].rectTransform.sizeDelta = new Vector2(px, px);
    }
}
