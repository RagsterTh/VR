using System.Collections.Generic;
using System.Reflection;
using TMPro;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.UI;

/// <summary>
/// Offline combat HUD drawn like a helmet visor: life as a curved vertical gauge on the left of the view,
/// the heal ability on the right, the mission (targets) and objective on top, a compass and small readouts.
/// Built at runtime and locked to the head; it replaces the life bar/heal widget of the player prefab
/// (their canvas is only hidden, the gameplay scripts keep running). Online keeps the original HUD.
/// </summary>
public sealed class OfflineHelmetHud : MonoBehaviour
{
    private const int Segments = 24;
    private const float ArcRadius = 700f;   // canvas units: 1 = 1 mm at the HUD distance
    private const float ArcSpan = 30f;      // degrees of the arc above/below its middle
    private const float SideAngle = 34f;    // how far to each side of the view the gauges sit (degrees)
    private const float TopAngle = 19f;     // mission block, above the line of sight
    private const float BottomAngle = -25f; // readouts, below the line of sight
    private const float RimAngle = 34f;     // visor rim lines
    private const float Distance = 1.3f;
    private const float CompassY = 175f;     // inside the top block
    private const float CompassHalfWidth = 340f;
    private const float CompassUnitsPerDegree = 8f;
    private const float CompassStep = 15f;

    private static readonly Color Cyan = new(0.35f, 0.9f, 1f);
    private static readonly Color Amber = new(1f, 0.7f, 0.2f);
    private static readonly Color Red = new(1f, 0.25f, 0.2f);
    private static readonly Color Green = new(0.3f, 1f, 0.55f);
    private static readonly Color Cooling = new(0.5f, 0.68f, 0.8f);

    private static OfflineHelmetHud _instance;
    private static TMP_FontAsset _font;
    private static bool _fontLoaded;
    private static Material _textMaterial;

    private Camera _head;
    private RectTransform _root;
    private CanvasGroup _group;
    private PlayersLifeBar _life;
    private PlayerHeal _heal;
    private GameOverManager _gameOver;
    private int _killCap = -1;

    // Life (left)
    private readonly Image[] _lifeSegments = new Image[Segments];
    private readonly float[] _lifeFlash = new float[Segments];
    private readonly List<Graphic> _lifeFrame = new();
    private TextMeshProUGUI _lifeValue;
    private TextMeshProUGUI _lifeStatus;
    private float _lastFraction = -1f;
    private float _shownFraction;
    private int _shownLife = -1;

    // Heal (right)
    private readonly Image[] _healSegments = new Image[Segments];
    private readonly List<Graphic> _healFrame = new();
    private TextMeshProUGUI _healValue;
    private TextMeshProUGUI _healStatus;
    private string _healButton = "A / X";
    private float _cooldownTotal;
    private bool _wasReady = true;
    private string _shownHeal;

    // Mission (top)
    private RectTransform _mission;
    private TextMeshProUGUI _missionText;
    private TextMeshProUGUI _objective;
    private readonly List<Image> _pips = new();
    private int _kills;
    private int _shownKills = -1;
    private string _objectiveText = string.Empty;
    private Color _objectiveColor = Color.white;

    // Compass + readouts
    private Image[] _compassTicks;
    private TextMeshProUGUI[] _compassLabels;
    private int[] _compassShown;
    private TextMeshProUGUI _accuracy;
    private TextMeshProUGUI _score;
    private int _shownAccuracy = -2;
    private int _shownScore = -1;
    private TextMeshProUGUI _bootText;

    private float _boot;
    private float _glitch;

    /// <summary>Creates the HUD (once per scene). It only shows up while the player has an active life bar.</summary>
    public static void Ensure()
    {
        if (_instance == null)
            _instance = new GameObject("[Offline] Helmet HUD").AddComponent<OfflineHelmetHud>();
    }

    public static void SetKills(int kills)
    {
        Ensure();
        _instance._kills = kills;
    }

    public static void SetObjective(string text, Color color)
    {
        Ensure();
        OfflineHelmetHud hud = _instance;
        bool changed = hud._objectiveText != text;
        hud._objectiveText = text;
        hud._objectiveColor = color;
        if (hud._objective == null)
            return;
        hud._objective.text = text;
        hud._objective.color = color;
        if (changed)
            hud.StartCoroutine(OfflineFx.Punch(hud._objective.transform, 0.2f, 0.2f));
    }

    private void OnDestroy()
    {
        if (_instance == this)
            _instance = null;
        if (_root != null)
            Destroy(_root.gameObject);
    }

    private void LateUpdate()
    {
        Camera head = Camera.main;
        if (head == null)
            return;
        if (_root == null || _head != head)
            Build(head);

        if (_life == null || !_life.isActiveAndEnabled)
        {
            _life = FindAnyObjectByType<PlayersLifeBar>();
            if (_life != null)
                HidePrefabHud();
        }
        if (_heal == null)
        {
            _heal = FindAnyObjectByType<PlayerHeal>();
            if (_heal != null)
            {
                foreach (TMP_Text text in _heal.GetComponentsInChildren<TMP_Text>(true))
                    if (text.name == "ButtonText" && !string.IsNullOrEmpty(text.text))
                        _healButton = text.text;
            }
        }

        float dt = Time.unscaledDeltaTime;
        bool show = _life != null && _life.isActiveAndEnabled;
        _group.alpha = Mathf.MoveTowards(_group.alpha, show ? 1f : 0f, dt * 3f);
        if (!show)
        {
            _boot = 0f;
            _lastFraction = -1f;
            return;
        }

        _boot = Mathf.MoveTowards(_boot, 1f, dt / 1.3f);
        UpdateLife(dt);
        UpdateHeal(dt);
        UpdateMission();
        UpdateCompass();
        UpdateReadouts();
        UpdateBoot();

        // Taking damage shakes the whole visor for a moment.
        _glitch = Mathf.Max(0f, _glitch - dt);
        Vector3 jitter = _glitch > 0f
            ? new Vector3(Random.Range(-1f, 1f), Random.Range(-1f, 1f), 0f) * (0.012f * _glitch / 0.3f)
            : Vector3.zero;
        _root.localPosition = new Vector3(0f, 0f, Distance) + jitter;
    }

    /// <summary>The prefab's own life bar + heal widget: hidden, not disabled (PlayerHeal needs the life bar active).</summary>
    private void HidePrefabHud()
    {
        Canvas canvas = _life.GetComponentInParent<Canvas>();
        if (canvas != null && canvas.transform != _root)
            canvas.enabled = false;
    }

    // ---------- Life ----------

    private void UpdateLife(float dt)
    {
        float max = Mathf.Max(1f, _life.MaxLife);
        float fraction = Mathf.Clamp01(_life.CurrentLife / max);
        if (_lastFraction < 0f)
        {
            _shownFraction = fraction;
        }
        else if (fraction < _lastFraction - 0.001f)
        {
            // The segments just lost flash white before going dark.
            for (int i = LitCount(fraction); i < LitCount(_lastFraction) && i < Segments; i++)
                _lifeFlash[i] = 1f;
            _shownFraction = fraction;
            _glitch = 0.3f;
        }
        _lastFraction = fraction;
        OfflineScore.SetLifeRatio(fraction);
        // Healing fills the gauge upwards instead of jumping.
        _shownFraction = Mathf.MoveTowards(_shownFraction, fraction, dt * 0.9f);

        bool critical = fraction <= 0.25f;
        Color state = critical ? Red : fraction <= 0.5f ? Amber : Cyan;
        float blink = critical ? 0.55f + 0.45f * Mathf.Sin(Time.unscaledTime * 10f) : 1f;
        int lit = Mathf.Min(LitCount(_shownFraction), Mathf.FloorToInt(_boot * (Segments + 1)));
        float shimmer = Mathf.Repeat(Time.unscaledTime * 14f, Segments + 40f);

        for (int i = 0; i < Segments; i++)
        {
            Color color = i < lit
                ? new Color(state.r, state.g, state.b, 0.95f * blink)
                : new Color(state.r, state.g, state.b, 0.13f);
            if (i < lit)
                color = Color.Lerp(color, Color.white, 0.55f * Mathf.Clamp01(1f - Mathf.Abs(i - shimmer) / 2.5f));
            if (_lifeFlash[i] > 0f)
            {
                color = Color.Lerp(color, new Color(1f, 1f, 1f, _lifeFlash[i]), _lifeFlash[i]);
                _lifeFlash[i] = Mathf.Max(0f, _lifeFlash[i] - dt * 2.5f);
            }
            _lifeSegments[i].color = color;
        }

        Tint(_lifeFrame, critical ? Red : Cyan);

        int life = Mathf.CeilToInt(_life.CurrentLife);
        if (life != _shownLife)
        {
            if (_shownLife >= 0)
                StartCoroutine(OfflineFx.Punch(_lifeValue.transform, 0.25f, 0.18f));
            _shownLife = life;
            _lifeValue.text = life.ToString();
        }
        _lifeValue.color = state;
        _lifeStatus.text = critical ? "CRÍTICO" : fraction <= 0.5f ? "ALERTA" : "ESTÁVEL";
        _lifeStatus.color = new Color(state.r, state.g, state.b, critical ? blink : 0.75f);
    }

    private static int LitCount(float fraction) =>
        fraction <= 0f ? 0 : Mathf.Clamp(Mathf.CeilToInt(fraction * Segments - 0.001f), 1, Segments);

    // ---------- Heal ----------

    private void UpdateHeal(float dt)
    {
        if (_heal == null)
        {
            foreach (Image segment in _healSegments)
                segment.color = new Color(Cooling.r, Cooling.g, Cooling.b, 0.1f);
            SetHealTexts("--", "INDISPONÍVEL", Cooling, 0.5f);
            return;
        }

        float remaining = _heal.CooldownRemaining;
        bool ready = remaining <= 0f;
        _cooldownTotal = ready ? 0f : Mathf.Max(_cooldownTotal, remaining);
        float charge = ready ? 1f : 1f - remaining / Mathf.Max(0.01f, _cooldownTotal);
        bool needed = ready && _lastFraction >= 0f && _lastFraction <= 0.5f;

        Color state = ready ? Green : Cooling;
        float pulse = !ready ? 1f
            : needed ? 0.55f + 0.45f * Mathf.Sin(Time.unscaledTime * 9f)
            : 0.82f + 0.18f * Mathf.Sin(Time.unscaledTime * 3f);
        int lit = Mathf.Min(Mathf.FloorToInt(charge * Segments + 0.001f), Mathf.FloorToInt(_boot * (Segments + 1)));
        for (int i = 0; i < Segments; i++)
        {
            _healSegments[i].color = i < lit
                ? new Color(state.r, state.g, state.b, 0.95f * pulse)
                : new Color(state.r, state.g, state.b, 0.13f);
        }
        Tint(_healFrame, ready ? Green : Cyan);

        if (ready && !_wasReady)
        {
            StartCoroutine(OfflineFx.Punch(_healValue.transform, 0.4f, 0.3f));
            OfflineFx.HapticAll(0.25f, 0.06f);
        }
        _wasReady = ready;

        if (ready)
            SetHealTexts(_healButton, needed ? "APERTE AGORA" : "PRONTO", Green, pulse);
        else
            SetHealTexts(Mathf.CeilToInt(remaining) + "s", "RECARREGANDO", Cooling, 0.75f);
    }

    private void SetHealTexts(string value, string status, Color color, float statusAlpha)
    {
        if (_shownHeal != value)
        {
            _shownHeal = value;
            _healValue.text = value;
        }
        _healValue.color = color;
        _healStatus.text = status;
        _healStatus.color = new Color(color.r, color.g, color.b, statusAlpha);
    }

    // ---------- Mission ----------

    private void UpdateMission()
    {
        // The shooting practice panel uses the same spot: the mission shows up when the battle is about to start.
        bool visible = !OfflineTutorial.Blocking;
        if (_mission.gameObject.activeSelf != visible)
            _mission.gameObject.SetActive(visible);
        if (!visible)
            return;

        if (_gameOver == null)
        {
            _gameOver = FindAnyObjectByType<GameOverManager>();
            if (_gameOver != null && _killCap < 0)
            {
                // The number of targets that ends the battle (private in GameOverManager).
                FieldInfo cap = typeof(GameOverManager).GetField("enemiesKilledCap", BindingFlags.NonPublic | BindingFlags.Instance);
                _killCap = cap != null ? (int)cap.GetValue(_gameOver) : 0;
                BuildPips(_killCap);
            }
        }

        int kills = _gameOver != null && _killCap > 0 ? Mathf.Min(_gameOver.EnemiesKilled, _killCap) : _kills;
        if (kills != _shownKills)
        {
            if (_shownKills >= 0)
                StartCoroutine(OfflineFx.Punch(_missionText.transform, 0.3f, 0.2f));
            _shownKills = kills;
            _missionText.text = _killCap > 0 ? $"ALVOS  {kills:00} / {_killCap:00}" : $"ABATES  {kills:00}";
        }

        bool done = _killCap > 0 && kills >= _killCap;
        _missionText.color = done ? Green : Cyan;
        for (int i = 0; i < _pips.Count; i++)
        {
            Color color = done ? Green : Cyan;
            color.a = i < kills ? 0.95f : 0.16f;
            _pips[i].color = color;
        }
    }

    private void BuildPips(int count)
    {
        count = Mathf.Clamp(count, 0, 30);
        float width = Mathf.Min(36f, 660f / Mathf.Max(1, count) - 8f);
        float step = width + 8f;
        for (int i = 0; i < count; i++)
        {
            Image pip = Rect(_mission, "Pip", new Vector2((i - (count - 1) * 0.5f) * step, -38f), new Vector2(width, 12f), Cyan, 0.16f);
            _pips.Add(pip);
        }
    }

    // ---------- Compass, readouts, boot ----------

    private void UpdateCompass()
    {
        float yaw = _head.transform.eulerAngles.y;
        float first = Mathf.Ceil((yaw - CompassHalfWidth / CompassUnitsPerDegree) / CompassStep) * CompassStep;
        for (int i = 0; i < _compassTicks.Length; i++)
        {
            float heading = first + i * CompassStep;
            float x = (heading - yaw) * CompassUnitsPerDegree;
            bool visible = Mathf.Abs(x) <= CompassHalfWidth;
            _compassTicks[i].enabled = visible;
            int degrees = ((Mathf.RoundToInt(heading) % 360) + 360) % 360;
            bool major = degrees % 45 == 0;
            _compassLabels[i].enabled = visible && major;
            if (!visible)
                continue;

            float fade = 1f - Mathf.Pow(Mathf.Abs(x) / CompassHalfWidth, 2f);
            RectTransform tick = _compassTicks[i].rectTransform;
            tick.anchoredPosition = new Vector2(x, CompassY + (major ? 13f : 7f));
            tick.sizeDelta = new Vector2(major ? 4f : 3f, major ? 26f : 14f);
            _compassTicks[i].color = new Color(Cyan.r, Cyan.g, Cyan.b, (major ? 0.9f : 0.5f) * fade);

            if (!major)
                continue;
            _compassLabels[i].rectTransform.anchoredPosition = new Vector2(x, CompassY + 52f);
            _compassLabels[i].color = new Color(Cyan.r, Cyan.g, Cyan.b, fade);
            if (_compassShown[i] != degrees)
            {
                _compassShown[i] = degrees;
                _compassLabels[i].text = degrees switch
                {
                    0 => "N", 45 => "NE", 90 => "L", 135 => "SE", 180 => "S", 225 => "SO", 270 => "O", _ => "NO"
                };
            }
        }
    }

    private void UpdateReadouts()
    {
        int accuracy = OfflineScore.Shots > 0 ? Mathf.RoundToInt(OfflineScore.Accuracy * 100f) : -1;
        if (accuracy != _shownAccuracy)
        {
            _shownAccuracy = accuracy;
            _accuracy.text = accuracy >= 0 ? $"PRECISÃO  {accuracy}%" : "PRECISÃO  --";
        }

        // Same formula as the result screen, live: it goes up with good shots and fast kills, down with misses and damage.
        int score = OfflineScore.Total;
        if (score != _shownScore)
        {
            if (_shownScore >= 0 && Mathf.Abs(score - _shownScore) >= 100)
                StartCoroutine(OfflineFx.Punch(_score.transform, 0.25f, 0.2f));
            _shownScore = score;
            _score.text = $"PONTOS  {score}";
        }
    }

    private void UpdateBoot()
    {
        if (_bootText == null)
            return;
        if (_boot >= 1f)
        {
            Destroy(_bootText.gameObject);
            _bootText = null;
            return;
        }
        // Blinks in, holds, fades out while the gauges fill up.
        float alpha = _boot < 0.7f ? (Mathf.Repeat(_boot * 12f, 1f) < 0.7f ? 1f : 0.35f) : 1f - (_boot - 0.7f) / 0.3f;
        _bootText.color = new Color(Cyan.r, Cyan.g, Cyan.b, alpha * 0.9f);
    }

    // ---------- Construction ----------

    private void Build(Camera head)
    {
        if (_root != null)
            Destroy(_root.gameObject);
        _head = head;
        _lifeFrame.Clear();
        _healFrame.Clear();
        _pips.Clear();
        _gameOver = null;
        _killCap = -1;
        _shownLife = _shownKills = _shownScore = -1;
        _shownAccuracy = -2;
        _shownHeal = null;
        _boot = 0f;

        _root = OfflineFx.CreateCanvas("[Offline] Helmet HUD", head.transform, new Vector2(2400, 1800), 0.001f, 151);
        _root.localPosition = new Vector3(0f, 0f, Distance);
        _root.localRotation = Quaternion.identity;
        _group = _root.gameObject.AddComponent<CanvasGroup>();
        _group.alpha = 0f;
        _group.blocksRaycasts = false;
        _group.interactable = false;

        // The visor wraps around the head: every block sits on a sphere centered on the eyes and faces them,
        // so the gauges live in the periphery of the view (like a helmet) instead of floating in the middle.
        RectTransform left = Panel("Life Wing", -SideAngle, 0f);
        RectTransform right = Panel("Heal Wing", SideAngle, 0f);
        BuildGauge(left, true, _lifeSegments, _lifeFrame);
        BuildGauge(right, false, _healSegments, _healFrame);

        // Texts sit just beyond the ends of each arc, on the inner side.
        _lifeFrame.Add(Text(left, "Life Label", "VIDA", 44f, Cyan, new Vector2(110f, 445f), 0.9f));
        _lifeValue = Text(left, "Life Value", "100", 100f, Cyan, new Vector2(115f, -460f));
        _lifeStatus = Text(left, "Life Status", "ESTÁVEL", 30f, Cyan, new Vector2(115f, -545f));
        _healFrame.Add(Text(right, "Heal Label", "CURA", 44f, Cyan, new Vector2(-110f, 445f), 0.9f));
        _healValue = Text(right, "Heal Value", _healButton, 84f, Green, new Vector2(-115f, -460f));
        _healStatus = Text(right, "Heal Status", "PRONTO", 30f, Green, new Vector2(-115f, -545f));

        // Mission: targets + objective, between two brackets, above the line of sight.
        RectTransform top = Panel("Top", 0f, TopAngle);
        var mission = new GameObject("Mission", typeof(RectTransform));
        _mission = (RectTransform)mission.transform;
        _mission.SetParent(top, false);
        Rect(_mission, "Plate", Vector2.zero, new Vector2(760f, 108f), new Color(0f, 0.05f, 0.1f), 0.35f);
        _missionText = Text(_mission, "Targets", "ALVOS  00", 60f, Cyan, new Vector2(0f, 14f));
        _objective = Text(_mission, "Objective", _objectiveText, 42f, _objectiveColor, new Vector2(0f, -112f));
        foreach (float side in new[] { -1f, 1f })
        {
            Rect(_mission, "Bracket", new Vector2(side * 386f, 0f), new Vector2(5f, 108f), Cyan, 0.8f);
            Rect(_mission, "Bracket Top", new Vector2(side * 374f, 52f), new Vector2(28f, 5f), Cyan, 0.8f);
            Rect(_mission, "Bracket Bottom", new Vector2(side * 374f, -52f), new Vector2(28f, 5f), Cyan, 0.8f);
        }

        // Compass tape.
        Rect(top, "Compass Line", new Vector2(0f, CompassY), new Vector2(CompassHalfWidth * 2f, 3f), Cyan, 0.3f);
        Image caret = Rect(top, "Compass Caret", new Vector2(0f, CompassY - 16f), new Vector2(13f, 13f), Cyan, 0.9f);
        caret.rectTransform.localRotation = Quaternion.Euler(0f, 0f, 45f);
        int ticks = Mathf.CeilToInt(CompassHalfWidth * 2f / CompassUnitsPerDegree / CompassStep) + 1;
        _compassTicks = new Image[ticks];
        _compassLabels = new TextMeshProUGUI[ticks];
        _compassShown = new int[ticks];
        for (int i = 0; i < ticks; i++)
        {
            _compassTicks[i] = Rect(top, "Compass Tick", Vector2.zero, new Vector2(3f, 14f), Cyan, 0.5f);
            _compassLabels[i] = Text(top, "Compass Label", string.Empty, 32f, Cyan, Vector2.zero);
            _compassShown[i] = -1;
        }

        // Readouts below the line of sight.
        RectTransform bottom = Panel("Bottom", 0f, BottomAngle);
        Rect(bottom, "Readout Line", new Vector2(0f, 28f), new Vector2(640f, 3f), Cyan, 0.3f);
        Rect(bottom, "Readout Split", new Vector2(0f, -6f), new Vector2(3f, 40f), Cyan, 0.4f);
        _accuracy = Text(bottom, "Accuracy", "PRECISÃO  --", 30f, Cyan, new Vector2(-170f, -6f), 0.85f);
        _score = Text(bottom, "Score", "PONTOS  0", 30f, Cyan, new Vector2(170f, -6f), 0.85f);

        // Rim of the visor: two faint lines running around the view, stronger towards the sides.
        const float rimStep = 4f;
        float rimLength = Distance * 1000f * rimStep * Mathf.Deg2Rad + 2f;
        for (float yaw = -44f; yaw <= 44f; yaw += rimStep)
        {
            float alpha = 0.08f + 0.3f * Mathf.Abs(yaw) / 44f;
            foreach (float pitch in new[] { RimAngle, -RimAngle })
                Rect(Panel("Rim", yaw, pitch), "Line", Vector2.zero, new Vector2(rimLength, 3f), Cyan, alpha);
        }

        _bootText = Text(_root, "Boot", "SISTEMAS ONLINE", 46f, Cyan, new Vector2(0f, 120f));
    }

    /// <summary>
    /// Container on the sphere around the eyes, at the given direction and facing them.
    /// Its children are laid out in its own plane (1 unit = 1 mm).
    /// </summary>
    private RectTransform Panel(string name, float yaw, float pitch)
    {
        var go = new GameObject(name, typeof(RectTransform));
        var rect = (RectTransform)go.transform;
        rect.SetParent(_root, false);
        rect.sizeDelta = Vector2.zero;
        Quaternion rotation = Quaternion.Euler(-pitch, yaw, 0f);
        float radius = Distance * 1000f;
        // The canvas origin is straight ahead at the HUD distance; the eyes are "radius" behind it.
        rect.localPosition = rotation * Vector3.forward * radius - Vector3.forward * radius;
        rect.localRotation = rotation;
        return rect;
    }

    /// <summary>One curved vertical gauge: left (life) or right (heal). Segment 0 is at the bottom.</summary>
    private void BuildGauge(RectTransform wing, bool left, Image[] segments, List<Graphic> frame)
    {
        // The arc bulges outwards and touches the wing's origin at mid height.
        Vector2 pivot = new Vector2(left ? ArcRadius : -ArcRadius, 0f);
        float center = left ? 180f : 0f;
        // Angles grow counter-clockwise: on the left side "up" means a smaller angle.
        float up = left ? -1f : 1f;
        float step = ArcSpan * 2f / (Segments - 1);
        string side = left ? "Life" : "Heal";

        for (int i = 0; i < Segments; i++)
        {
            float angle = center + up * (-ArcSpan + i * step);
            segments[i] = Polar(wing, pivot, side + " Segment", ArcRadius, angle, new Vector2(66f, 23f), Cyan, 0.13f);
        }

        // Thin lines following the arc, outside and inside the segments.
        const int pieces = 36;
        float frameSpan = ArcSpan + 3.5f;
        float pieceStep = frameSpan * 2f / pieces;
        for (int i = 0; i < pieces; i++)
        {
            float angle = center - frameSpan + (i + 0.5f) * pieceStep;
            float length = pieceStep * Mathf.Deg2Rad;
            frame.Add(Polar(wing, pivot, side + " Outer", ArcRadius + 48f, angle, new Vector2(4f, (ArcRadius + 48f) * length + 1.5f), Cyan, 0.75f));
            frame.Add(Polar(wing, pivot, side + " Inner", ArcRadius - 48f, angle, new Vector2(2.5f, (ArcRadius - 48f) * length + 1.5f), Cyan, 0.25f));
        }

        // Scale marks every quarter and a cap at each end.
        for (int i = 0; i <= 4; i++)
        {
            float angle = center + up * (-ArcSpan + i * ArcSpan * 0.5f);
            frame.Add(Polar(wing, pivot, side + " Mark", ArcRadius + 68f, angle, new Vector2(i % 2 == 0 ? 32f : 18f, 4f), Cyan, 0.75f));
        }
        frame.Add(Polar(wing, pivot, side + " Cap", ArcRadius - 4f, center - frameSpan, new Vector2(108f, 4f), Cyan, 0.75f));
        frame.Add(Polar(wing, pivot, side + " Cap", ArcRadius - 4f, center + frameSpan, new Vector2(108f, 4f), Cyan, 0.75f));
    }

    private static void Tint(List<Graphic> graphics, Color color)
    {
        foreach (Graphic graphic in graphics)
        {
            color.a = graphic.color.a;
            graphic.color = color;
        }
    }

    /// <summary>Rectangle placed on a circle around <paramref name="pivot"/>; its first size is along the radius.</summary>
    private static Image Polar(Transform parent, Vector2 pivot, string name, float radius, float angle, Vector2 size, Color color, float alpha)
    {
        float radians = angle * Mathf.Deg2Rad;
        Image image = Rect(parent, name, pivot + new Vector2(Mathf.Cos(radians), Mathf.Sin(radians)) * radius, size, color, alpha);
        image.rectTransform.localRotation = Quaternion.Euler(0f, 0f, angle);
        return image;
    }

    private static Image Rect(Transform parent, string name, Vector2 position, Vector2 size, Color color, float alpha)
    {
        Image image = OfflineFx.AddImage(parent, name, OfflineFx.White, new Color(color.r, color.g, color.b, alpha), size, position);
        image.material = OfflineFx.OverlayMaterial;
        return image;
    }

    private static TextMeshProUGUI Text(Transform parent, string name, string text, float size, Color color, Vector2 position, float alpha = 1f)
    {
        var go = new GameObject(name, typeof(RectTransform));
        var rect = (RectTransform)go.transform;
        rect.SetParent(parent, false);
        rect.sizeDelta = new Vector2(700f, size * 1.4f);
        rect.anchoredPosition = position;

        var tmp = go.AddComponent<TextMeshProUGUI>();
        TMP_FontAsset font = Font;
        if (font != null)
        {
            tmp.font = font;
            tmp.fontSharedMaterial = TextMaterial(font);
        }
        tmp.text = text;
        tmp.fontSize = size;
        tmp.color = new Color(color.r, color.g, color.b, alpha);
        tmp.alignment = TextAlignmentOptions.Center;
        tmp.textWrappingMode = TextWrappingModes.NoWrap;
        tmp.characterSpacing = 6f;
        tmp.raycastTarget = false;
        return tmp;
    }

    /// <summary>The sci-fi font of the offline menus (static atlas, safe in builds); TMP's default when missing.</summary>
    private static TMP_FontAsset Font
    {
        get
        {
            if (!_fontLoaded)
            {
                _fontLoaded = true;
                _font = UnityEngine.Resources.Load<TMP_FontAsset>("Zekton-Regular Offline Static SDF");
                if (_font == null)
                    _font = TMP_Settings.defaultFontAsset;
            }
            return _font;
        }
    }

    /// <summary>Same font material, drawn on top of the scene like the rest of the visor.</summary>
    private static Material TextMaterial(TMP_FontAsset font)
    {
        if (_textMaterial == null)
        {
            _textMaterial = new Material(font.material) { name = font.name + " (HUD overlay)" };
            _textMaterial.SetInt("unity_GUIZTestMode", (int)CompareFunction.Always);
            // Dark rim so the readouts stay legible over a bright sky or an explosion.
            _textMaterial.EnableKeyword("OUTLINE_ON");
            _textMaterial.SetFloat(ShaderUtilities.ID_OutlineWidth, 0.16f);
            _textMaterial.SetColor(ShaderUtilities.ID_OutlineColor, new Color(0f, 0.04f, 0.08f, 0.9f));
        }
        return _textMaterial;
    }
}
