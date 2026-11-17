using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

/// <summary>
/// Look and feel of the offline entry menu, built at runtime around the existing panel (its buttons keep working):
/// - the panel boots like a hologram and the two buttons become mission cards (what each mode is, today's record);
/// - two side wings curve around the visitor: today's Top 5 and how to play;
/// - space around: drifting star dust, shooting stars, a soft engine hum, the scene light tinted by the hovered mode;
/// - choosing a mode jumps to warp before the scene fades out.
/// </summary>
public sealed class OfflineMenuFeedback : MonoBehaviour
{
    private static readonly Color Cyan = new(0.35f, 0.9f, 1f);
    private static readonly Color Gold = new(1f, 0.82f, 0.25f);
    private static readonly Color CombatColor = new(1f, 0.32f, 0.22f);
    private static readonly Color FullColor = new(0.3f, 0.65f, 1f);
    private static readonly Color Glass = new(0.02f, 0.05f, 0.1f, 0.88f);

    private const float PanelWidth = 1000f;
    private const float PanelHeight = 760f;

    private RectTransform _panel;
    private Image _background;
    private Image _scan;
    private readonly List<Image> _corners = new();
    private readonly List<MenuCard> _cards = new();
    private readonly List<Graphic> _bootFade = new();
    private TMP_Text _status;
    private Light _light;
    private Color _lightBase;
    private float _lightIntensity;
    private ParticleSystem _dust;
    private AudioSource _hum;
    private float _nextShootingStar;
    private bool _launching;

    private static TMP_FontAsset _font;

    private sealed class MenuCard
    {
        public Button Button;
        public RectTransform Rect;
        public Image Face;
        public Image Accent;
        public Image Glow;
        public Color Color;
        public OfflineMenuHover Hover;
        public float Highlight;
        public OfflineExperienceMode Mode;
        public TMP_Text Record;
    }

    private RectTransform _rankingWing;
    private RectTransform _rankingRows;
    private readonly List<(OfflineDifficultyLevel level, Image face, TMP_Text label)> _difficultyChips = new();
    private TMP_Text _difficultyHint;

    // ---------- Entry points ----------

    /// <summary>Restyles <paramref name="panel"/>; <paramref name="status"/> stays the line the menu writes to.</summary>
    public static OfflineMenuFeedback Attach(RectTransform panel, TMP_Text status, Button combat, Button full)
    {
        var feedback = panel.gameObject.AddComponent<OfflineMenuFeedback>();
        feedback.Build(panel, status, combat, full);
        return feedback;
    }

    /// <summary>Warp jump played between the click and the scene change.</summary>
    public IEnumerator Launch(OfflineExperienceMode mode)
    {
        _launching = true;
        MenuCard chosen = _cards.Find(c => c.Button != null && (mode == OfflineExperienceMode.CombatOnly) == (c.Color == CombatColor));
        Color color = chosen != null ? chosen.Color : Cyan;
        foreach (MenuCard card in _cards)
            card.Button.interactable = false;

        // The chosen card flashes, the others dim.
        if (chosen != null)
        {
            StartCoroutine(OfflineFx.Punch(chosen.Rect, 0.12f, 0.25f));
            OfflineFx.Burst(chosen.Rect.position, color, 60, 1.6f, 0.03f, 0.8f, 0f, 0.3f);
        }
        if (_status != null)
        {
            _status.text = "INICIANDO MISSÃO";
            _status.color = color;
        }
        OfflineFx.HapticAll(0.5f, 0.15f);

        Camera head = Camera.main;
        ParticleSystem warp = head != null ? WarpTunnel(head.transform, color) : null;
        PlayWhoosh();

        // Panel folds into a line, stars stretch and rush past, the hum rises.
        Vector3 scale = _panel.localScale;
        float t = 0f;
        const float duration = 1.6f;
        while (t < duration)
        {
            t += Time.unscaledDeltaTime;
            float k = t / duration;
            float fold = Mathf.Clamp01((k - 0.15f) / 0.35f);
            _panel.localScale = new Vector3(scale.x * (1f + 0.15f * fold), scale.y * Mathf.Max(0.01f, 1f - OfflineFx.EaseOut(fold)), scale.z);
            foreach (MenuCard card in _cards)
                card.Face.color = Color.Lerp(card.Face.color, card == chosen ? color : Glass, 0.15f);
            if (warp != null)
            {
                var main = warp.main;
                main.simulationSpeed = Mathf.Lerp(0.3f, 3f, k * k);
            }
            if (_hum != null)
                _hum.pitch = Mathf.Lerp(1f, 2.2f, k * k);
            if (_light != null)
                _light.intensity = _lightIntensity * (1f + 2f * k);
            if (Time.frameCount % 6 == 0)
                OfflineFx.HapticAll(0.1f + 0.5f * k, 0.05f);
            yield return null;
        }
        OfflineViewOverlay.Flash(Color.white, 0.9f, 0.5f);
        OfflineFx.HapticAll(0.8f, 0.2f);
    }

    // ---------- Construction ----------

    private void Build(RectTransform panel, TMP_Text status, Button combat, Button full)
    {
        _panel = panel;
        _status = status;
        _font = UnityEngine.Resources.Load<TMP_FontAsset>("Zekton-Regular Offline Static SDF");

        panel.sizeDelta = new Vector2(PanelWidth, PanelHeight);
        Transform background = panel.Find("Background");
        _background = background != null ? background.GetComponent<Image>() : null;
        if (_background != null)
            _background.color = Glass;

        BuildFrame(panel, new Vector2(PanelWidth, PanelHeight), Cyan, true);

        // Header.
        Transform title = panel.Find("Title");
        if (title != null)
        {
            var titleRect = (RectTransform)title;
            titleRect.anchoredPosition = new Vector2(0f, 300f);
            var text = title.GetComponent<TMP_Text>();
            Restyle(text, 60f, Color.white);
            text.characterSpacing = 12f;
            StartCoroutine(Typewriter(text, text.text, 0.25f));
        }
        AddText(panel, "Subtitle", "ESCOLHA SUA MISSÃO", 26f, Cyan, new Vector2(0f, 248f), new Vector2(900f, 40f)).characterSpacing = 18f;
        Line(panel, new Vector2(0f, 224f), new Vector2(560f, 2f), Cyan, 0.5f);

        if (status != null)
        {
            var statusRect = status.rectTransform;
            statusRect.anchoredPosition = new Vector2(0f, -334f);
            statusRect.sizeDelta = new Vector2(900f, 50f);
            Restyle(status, 24f, new Color(0.8f, 0.9f, 1f, 0.85f));
            status.characterSpacing = 6f;
        }

        if (combat != null)
            _cards.Add(BuildCard(combat, CombatColor, "COMBATE", "DEFENDA A BASE CONTRA ONDAS DE DRONES", OfflineExperienceMode.CombatOnly, 105f));
        if (full != null)
            _cards.Add(BuildCard(full, FullColor, "EXPERIÊNCIA COMPLETA", "EVE  •  MAPA  •  COMBATE  •  RESGATE MÉDICO", OfflineExperienceMode.FullExperience, -115f));

        BuildDifficulty(panel);
        BuildWing(panel, true);
        BuildWing(panel, false);
        BuildSpace();

        StartCoroutine(Boot());
    }

    private MenuCard BuildCard(Button button, Color color, string title, string description, OfflineExperienceMode mode, float y)
    {
        var rect = (RectTransform)button.transform;
        rect.anchoredPosition = new Vector2(0f, y);
        rect.sizeDelta = new Vector2(860f, 190f);

        var card = new MenuCard { Button = button, Rect = rect, Color = color };
        card.Face = button.GetComponent<Image>();
        card.Face.sprite = OfflineFx.White;
        card.Face.color = new Color(color.r * 0.12f, color.g * 0.12f, color.b * 0.12f, 0.92f);
        ColorBlock colors = button.colors;
        colors.normalColor = Color.white;
        colors.highlightedColor = Color.white;
        colors.selectedColor = Color.white;
        colors.pressedColor = new Color(0.8f, 0.8f, 0.8f, 1f);
        colors.disabledColor = Color.white;
        button.colors = colors;

        // Old label replaced by a title + description + record.
        Transform label = rect.Find("Label");
        if (label != null)
            label.gameObject.SetActive(false);

        card.Glow = Image(rect, "Glow", OfflineFx.Soft, new Color(color.r, color.g, color.b, 0f), new Vector2(1100f, 420f), Vector2.zero);
        card.Glow.transform.SetAsFirstSibling();
        card.Accent = Image(rect, "Accent", OfflineFx.White, color, new Vector2(10f, 190f), new Vector2(-425f, 0f));
        BuildFrame(rect, new Vector2(860f, 190f), color, false);

        // Mode icon: ring + mark.
        Image(rect, "Icon Ring", OfflineFx.Ring, color, new Vector2(120f, 120f), new Vector2(-330f, 0f));
        if (mode == OfflineExperienceMode.CombatOnly)
        {
            // Crosshair.
            Image(rect, "Icon H", OfflineFx.White, color, new Vector2(70f, 6f), new Vector2(-330f, 0f));
            Image(rect, "Icon V", OfflineFx.White, color, new Vector2(6f, 70f), new Vector2(-330f, 0f));
            Image(rect, "Icon Dot", OfflineFx.Circle, Color.white, new Vector2(14f, 14f), new Vector2(-330f, 0f));
        }
        else
        {
            // Small globe.
            Image(rect, "Icon Core", OfflineFx.Circle, new Color(color.r, color.g, color.b, 0.35f), new Vector2(76f, 76f), new Vector2(-330f, 0f));
            Image(rect, "Icon Eq", OfflineFx.White, color, new Vector2(76f, 4f), new Vector2(-330f, 0f));
            Image(rect, "Icon Mer", OfflineFx.Ring, color, new Vector2(40f, 88f), new Vector2(-330f, 0f));
        }

        TMP_Text titleText = AddText(rect, "Title", title, 44f, Color.white, new Vector2(80f, 42f), new Vector2(620f, 64f));
        titleText.alignment = TextAlignmentOptions.Left;
        titleText.characterSpacing = 6f;
        TMP_Text descText = AddText(rect, "Description", description, 22f, new Color(0.85f, 0.9f, 1f, 0.8f), new Vector2(80f, -6f), new Vector2(620f, 34f));
        descText.alignment = TextAlignmentOptions.Left;
        descText.characterSpacing = 3f;

        card.Mode = mode;
        TMP_Text recordText = AddText(rect, "Record", string.Empty, 22f, Gold, new Vector2(80f, -52f), new Vector2(620f, 34f));
        card.Record = recordText;
        RefreshRecord(card);
        recordText.alignment = TextAlignmentOptions.Left;
        recordText.characterSpacing = 3f;

        Image arrow = Image(rect, "Arrow", OfflineFx.White, color, new Vector2(22f, 22f), new Vector2(380f, 0f));
        arrow.rectTransform.localRotation = Quaternion.Euler(0f, 0f, -45f);
        Image(arrow.rectTransform, "Notch", OfflineFx.White, card.Face.color, new Vector2(18f, 18f), new Vector2(-6f, -6f));

        card.Hover = button.gameObject.AddComponent<OfflineMenuHover>();
        card.Hover.Entered += () => OnHover(card);
        return card;
    }

    /// <summary>Recruta / Soldado / Elite chips under the cards (kept on the headset for the next visitors).</summary>
    private void BuildDifficulty(RectTransform panel)
    {
        TMP_Text title = AddText(panel, "Difficulty Title", "DIFICULDADE", 20f, new Color(Cyan.r, Cyan.g, Cyan.b, 0.8f), new Vector2(-330f, -240f), new Vector2(200f, 30f));
        title.characterSpacing = 8f;
        var levels = new[] { OfflineDifficultyLevel.Recruta, OfflineDifficultyLevel.Soldado, OfflineDifficultyLevel.Elite };
        for (int i = 0; i < levels.Length; i++)
        {
            OfflineDifficultyLevel level = levels[i];
            Vector2 position = new Vector2(-110f + i * 190f, -240f);
            Image face = Image(panel, "Difficulty " + level, OfflineFx.White, Glass, new Vector2(176f, 52f), position);
            face.raycastTarget = true;
            var button = face.gameObject.AddComponent<Button>();
            button.targetGraphic = face;
            ColorBlock colors = button.colors;
            colors.highlightedColor = new Color(1.3f, 1.3f, 1.3f, 1f);
            button.colors = colors;
            button.onClick.AddListener(() => SelectDifficulty(level));
            face.gameObject.AddComponent<OfflineButtonFeedback>();
            BuildFrame(face.rectTransform, new Vector2(176f, 52f), OfflineDifficulty.Color(level), false);
            TMP_Text label = AddText(face.rectTransform, "Label", OfflineDifficulty.Label(level), 26f, Color.white, Vector2.zero, new Vector2(176f, 52f));
            label.characterSpacing = 6f;
            _difficultyChips.Add((level, face, label));
        }
        _difficultyHint = AddText(panel, "Difficulty Hint", string.Empty, 18f, new Color(1f, 1f, 1f, 0.6f), new Vector2(80f, -284f), new Vector2(700f, 26f));
        _difficultyHint.characterSpacing = 4f;
        PaintDifficulty();
    }

    private void SelectDifficulty(OfflineDifficultyLevel level)
    {
        if (_launching)
            return;
        OfflineDifficulty.Current = level;
        PaintDifficulty();
        foreach (var chip in _difficultyChips)
            if (chip.level == level)
                OfflineFx.Burst(chip.face.rectTransform.position, OfflineDifficulty.Color(level), 24, 0.8f, 0.02f, 0.5f, 0f, 0.05f);
        foreach (MenuCard card in _cards)
            RefreshRecord(card);
        RefreshRanking();
    }

    private void PaintDifficulty()
    {
        OfflineDifficultyLevel current = OfflineDifficulty.Current;
        foreach (var chip in _difficultyChips)
        {
            Color color = OfflineDifficulty.Color(chip.level);
            bool on = chip.level == current;
            chip.face.color = on ? new Color(color.r * 0.45f, color.g * 0.45f, color.b * 0.45f, 0.95f) : Glass;
            chip.label.color = on ? Color.white : new Color(color.r, color.g, color.b, 0.75f);
        }
        if (_difficultyHint != null)
        {
            _difficultyHint.text = OfflineDifficulty.Description(current);
            _difficultyHint.color = new Color(OfflineDifficulty.Color(current).r, OfflineDifficulty.Color(current).g, OfflineDifficulty.Color(current).b, 0.8f);
        }
    }

    /// <summary>Curved side panel (a child of the menu panel, so it follows the head with it).</summary>
    private void BuildWing(RectTransform panel, bool left)
    {
        var go = new GameObject(left ? "[Offline] Wing Controls" : "[Offline] Wing Ranking", typeof(RectTransform));
        var wing = (RectTransform)go.transform;
        wing.SetParent(panel, false);
        float side = left ? -1f : 1f;
        wing.sizeDelta = new Vector2(520f, 640f);
        wing.anchoredPosition = new Vector2(side * 790f, -20f);
        // Turned toward the visitor, like the side screens of a cockpit.
        wing.localPosition += new Vector3(0f, 0f, -150f);
        wing.localRotation = Quaternion.Euler(0f, side * 32f, 0f);

        Image back = Image(wing, "Back", OfflineFx.White, new Color(Glass.r, Glass.g, Glass.b, 0.78f), wing.sizeDelta, Vector2.zero);
        _bootFade.Add(back);
        BuildFrame(wing, wing.sizeDelta, Cyan, false);

        if (left)
        {
            Header(wing, "COMO JOGAR");
            Row(wing, 0, "GATILHO", "ESCOLHER NO MENU");
            Row(wing, 1, "GRIP", "ATIRAR");
            Row(wing, 2, "A  /  X", "CURAR");
            Row(wing, 3, "MIRA", "SIGA O LASER DA ARMA");
            Row(wing, 4, "SETAS", "INIMIGOS FORA DA VISÃO");
        }
        else
        {
            Header(wing, "RANKING DE HOJE");
            _rankingWing = wing;
            RefreshRanking();
        }
    }

    private void RefreshRecord(MenuCard card)
    {
        if (card.Record == null)
            return;
        IReadOnlyList<OfflineLeaderboard.Entry> top = OfflineLeaderboard.Top(card.Mode);
        card.Record.text = top.Count > 0 ? $"RECORDE DE HOJE   {top[0].name}  {top[0].score}" : "NENHUM RECORDE HOJE  -  SEJA O PRIMEIRO";
        card.Record.color = top.Count > 0 ? Gold : new Color(1f, 1f, 1f, 0.5f);
    }

    private void RefreshRanking()
    {
        if (_rankingWing == null)
            return;
        if (_rankingRows != null)
            Destroy(_rankingRows.gameObject);
        var go = new GameObject("Rows", typeof(RectTransform));
        _rankingRows = (RectTransform)go.transform;
        _rankingRows.SetParent(_rankingWing, false);
        RectTransform wing = _rankingRows;
        {
            Color levelColor = OfflineDifficulty.Color(OfflineDifficulty.Current);
            AddText(wing, "Level", OfflineDifficulty.Label(OfflineDifficulty.Current), 24f, levelColor, new Vector2(0f, 212f), new Vector2(460f, 34f)).characterSpacing = 10f;
            int row = 1;
            foreach (OfflineExperienceMode mode in new[] { OfflineExperienceMode.CombatOnly, OfflineExperienceMode.FullExperience })
            {
                Color color = mode == OfflineExperienceMode.CombatOnly ? CombatColor : FullColor;
                TMP_Text label = AddText(wing, "Mode", mode == OfflineExperienceMode.CombatOnly ? "COMBATE" : "EXPERIÊNCIA COMPLETA",
                    22f, color, new Vector2(0f, 200f - row * 44f), new Vector2(460f, 34f));
                label.characterSpacing = 6f;
                row++;
                IReadOnlyList<OfflineLeaderboard.Entry> top = OfflineLeaderboard.Top(mode);
                if (top.Count == 0)
                {
                    AddText(wing, "Empty", "AINDA SEM PARTIDAS", 22f, new Color(1f, 1f, 1f, 0.45f), new Vector2(0f, 200f - row * 44f), new Vector2(460f, 34f));
                    row++;
                }
                for (int i = 0; i < Mathf.Min(3, top.Count); i++)
                {
                    TMP_Text entry = AddText(wing, "Entry",
                        $"{i + 1}.<pos=14%>{top[i].name}<pos=50%>{top[i].score}<pos=86%>{top[i].grade}",
                        26f, i == 0 ? Gold : Color.white, new Vector2(0f, 200f - row * 44f), new Vector2(420f, 36f));
                    entry.alignment = TextAlignmentOptions.Left;
                    row++;
                }
                row++;
            }
        }
    }

    private void Header(RectTransform wing, string text)
    {
        TMP_Text header = AddText(wing, "Header", text, 32f, Cyan, new Vector2(0f, 270f), new Vector2(480f, 50f));
        header.characterSpacing = 10f;
        Line(wing, new Vector2(0f, 240f), new Vector2(380f, 2f), Cyan, 0.5f);
    }

    private void Row(RectTransform wing, int index, string key, string action)
    {
        float y = 170f - index * 92f;
        Image chip = Image(wing, "Chip", OfflineFx.White, new Color(Cyan.r, Cyan.g, Cyan.b, 0.14f), new Vector2(170f, 56f), new Vector2(-140f, y));
        _bootFade.Add(chip);
        BuildFrame(chip.rectTransform, new Vector2(170f, 56f), Cyan, false);
        AddText(wing, "Key", key, 26f, Cyan, new Vector2(-140f, y), new Vector2(170f, 56f));
        TMP_Text label = AddText(wing, "Action", action, 22f, Color.white, new Vector2(95f, y), new Vector2(270f, 56f));
        label.alignment = TextAlignmentOptions.Left;
        label.textWrappingMode = TextWrappingModes.Normal;
    }

    /// <summary>Corner brackets around a rectangle (the menu panel ones pulse).</summary>
    private void BuildFrame(RectTransform parent, Vector2 size, Color color, bool track)
    {
        float hx = size.x * 0.5f, hy = size.y * 0.5f;
        float arm = Mathf.Min(70f, Mathf.Min(size.x, size.y) * 0.25f);
        const float thick = 4f;
        foreach (float sx in new[] { -1f, 1f })
        foreach (float sy in new[] { -1f, 1f })
        {
            Image h = Image(parent, "Corner", OfflineFx.White, color, new Vector2(arm, thick), new Vector2(sx * (hx - arm * 0.5f), sy * hy));
            Image v = Image(parent, "Corner", OfflineFx.White, color, new Vector2(thick, arm), new Vector2(sx * hx, sy * (hy - arm * 0.5f)));
            if (track)
            {
                _corners.Add(h);
                _corners.Add(v);
            }
        }
        if (track)
        {
            // Scanline sweeping down the glass.
            _scan = Image(parent, "Scanline", OfflineFx.White, new Color(color.r, color.g, color.b, 0.05f), new Vector2(size.x - 20f, 12f), Vector2.zero);
            _scan.transform.SetSiblingIndex(1);
        }
    }

    // ---------- Space around ----------

    private void BuildSpace()
    {
        Camera head = Camera.main;
        Vector3 center = head != null ? head.transform.position : Vector3.up * 1.5f;

        // Star dust drifting slowly past the visitor: gives depth and motion to the empty space.
        _dust = OfflineFx.Emitter(null, center, new Vector3(16f, 8f, 16f), new Color(0.75f, 0.9f, 1f, 0.9f), 70f, 9f, 0.025f,
            new Vector3(0.05f, 0.02f, -0.25f), 0.05f);
        if (_dust != null)
            _dust.gameObject.name = "[Offline] Star Dust";

        // Warm glow behind the panel, slowly breathing.
        _light = FindAnyObjectByType<Light>();
        foreach (Light light in FindObjectsByType<Light>(FindObjectsSortMode.None))
            if (light.type == LightType.Directional) _light = light;
        if (_light != null)
        {
            _lightBase = _light.color;
            _lightIntensity = _light.intensity;
        }

        _hum = gameObject.AddComponent<AudioSource>();
        _hum.clip = HumClip();
        _hum.loop = true;
        _hum.spatialBlend = 0f;
        _hum.volume = 0f;
        _hum.Play();

        _nextShootingStar = Time.unscaledTime + 2.5f;
    }

    private void Update()
    {
        float time = Time.unscaledTime;
        float dt = Time.unscaledDeltaTime;

        if (_hum != null && !_launching)
            _hum.volume = Mathf.MoveTowards(_hum.volume, 0.12f, dt * 0.05f);

        // Frame breathing + scanline.
        float pulse = 0.6f + 0.4f * Mathf.Sin(time * 2.2f);
        foreach (Image corner in _corners)
            corner.color = new Color(Cyan.r, Cyan.g, Cyan.b, 0.55f + 0.45f * pulse);
        if (_scan != null)
            _scan.rectTransform.anchoredPosition = new Vector2(0f, Mathf.Lerp(PanelHeight * 0.5f - 20f, -PanelHeight * 0.5f + 20f, Mathf.Repeat(time * 0.25f, 1f)));

        // Cards: hovered one glows, its color tints the scene light.
        MenuCard hovered = null;
        foreach (MenuCard card in _cards)
        {
            bool on = card.Hover != null && card.Hover.IsOver && !_launching;
            if (on)
                hovered = card;
            card.Highlight = Mathf.MoveTowards(card.Highlight, on ? 1f : 0f, dt * 5f);
            float h = card.Highlight;
            card.Glow.color = new Color(card.Color.r, card.Color.g, card.Color.b, 0.35f * h + 0.05f * pulse);
            card.Accent.rectTransform.sizeDelta = new Vector2(10f + 8f * h, 190f);
            if (!_launching)
                card.Face.color = Color.Lerp(new Color(card.Color.r * 0.12f, card.Color.g * 0.12f, card.Color.b * 0.12f, 0.92f),
                    new Color(card.Color.r * 0.3f, card.Color.g * 0.3f, card.Color.b * 0.3f, 0.95f), h);
        }
        if (_light != null && !_launching)
        {
            Color target = hovered != null ? Color.Lerp(_lightBase, hovered.Color, 0.35f) : _lightBase;
            _light.color = Color.Lerp(_light.color, target, 1f - Mathf.Exp(-4f * dt));
        }
        if (_dust != null)
        {
            var main = _dust.main;
            Color dustColor = hovered != null ? Color.Lerp(new Color(0.75f, 0.9f, 1f), hovered.Color, 0.5f) : new Color(0.75f, 0.9f, 1f);
            main.startColor = dustColor;
        }

        if (time >= _nextShootingStar && !_launching)
        {
            _nextShootingStar = time + UnityEngine.Random.Range(2.5f, 6f);
            StartCoroutine(ShootingStar());
        }
    }

    private void OnHover(MenuCard card)
    {
        if (_launching)
            return;
        OfflineFx.Burst(card.Accent.rectTransform.position, card.Color, 16, 0.6f, 0.015f, 0.5f, 0f, 0.08f);
        if (_status != null)
            _status.text = "APERTE O GATILHO PARA COMEÇAR";
    }

    private IEnumerator ShootingStar()
    {
        Camera head = Camera.main;
        if (head == null || OfflineFx.LineMaterial == null)
            yield break;

        // Somewhere far in front/above, crossing the sky diagonally.
        Vector3 forward = Vector3.ProjectOnPlane(head.transform.forward, Vector3.up).normalized;
        if (forward.sqrMagnitude < 0.01f)
            forward = Vector3.forward;
        Vector3 right = Vector3.Cross(Vector3.up, forward);
        Vector3 start = head.transform.position + forward * UnityEngine.Random.Range(40f, 70f)
                        + right * UnityEngine.Random.Range(-40f, 40f) + Vector3.up * UnityEngine.Random.Range(8f, 30f);
        Vector3 direction = (right * (UnityEngine.Random.value < 0.5f ? -1f : 1f) + Vector3.down * UnityEngine.Random.Range(0.2f, 0.6f)).normalized;

        var go = new GameObject("[Offline] Shooting Star");
        go.transform.position = start;
        var trail = go.AddComponent<TrailRenderer>();
        trail.sharedMaterial = OfflineFx.LineMaterial;
        trail.time = 0.35f;
        trail.widthCurve = new AnimationCurve(new Keyframe(0f, 0.35f), new Keyframe(1f, 0f));
        trail.startColor = new Color(0.85f, 0.95f, 1f, 0.9f);
        trail.endColor = new Color(0.4f, 0.7f, 1f, 0f);
        trail.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;

        float t = 0f;
        float speed = UnityEngine.Random.Range(50f, 80f);
        while (t < 1.2f && go != null)
        {
            t += Time.unscaledDeltaTime;
            go.transform.position += direction * speed * Time.unscaledDeltaTime;
            yield return null;
        }
        if (go != null)
            Destroy(go, 0.5f);
    }

    /// <summary>Stars streaking toward the visitor (stretched particles in a tunnel ahead of the head).</summary>
    private static ParticleSystem WarpTunnel(Transform head, Color color)
    {
        if (OfflineFx.ParticleMaterial == null)
            return null;
        var go = new GameObject("[Offline] Warp");
        go.transform.SetPositionAndRotation(head.position + head.forward * 30f, Quaternion.LookRotation(-head.forward, Vector3.up));
        var particles = go.AddComponent<ParticleSystem>();
        particles.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);

        var main = particles.main;
        main.loop = true;
        main.duration = 2f;
        main.startLifetime = 1.4f;
        main.startSpeed = new ParticleSystem.MinMaxCurve(18f, 30f);
        main.startSize = new ParticleSystem.MinMaxCurve(0.05f, 0.12f);
        main.startColor = new ParticleSystem.MinMaxGradient(Color.white, Color.Lerp(Color.white, color, 0.6f));
        main.simulationSpace = ParticleSystemSimulationSpace.World;
        main.maxParticles = 1200;
        main.simulationSpeed = 0.3f;

        var emission = particles.emission;
        emission.rateOverTime = 600f;

        var shape = particles.shape;
        shape.shapeType = ParticleSystemShapeType.Circle;
        shape.radius = 7f;
        shape.radiusThickness = 0.6f;

        var renderer = go.GetComponent<ParticleSystemRenderer>();
        renderer.sharedMaterial = OfflineFx.ParticleMaterial;
        renderer.renderMode = ParticleSystemRenderMode.Stretch;
        renderer.velocityScale = 0.12f;
        renderer.lengthScale = 4f;

        particles.Play();
        Destroy(go, 4f);
        return particles;
    }

    // ---------- Boot ----------

    private IEnumerator Boot()
    {
        // Hologram unfolding: thin line -> full height, with a couple of flickers.
        Vector3 scale = _panel.localScale;
        _panel.localScale = new Vector3(scale.x, scale.y * 0.01f, scale.z);
        yield return new WaitForSecondsRealtime(0.4f);
        OfflineFx.HapticAll(0.2f, 0.05f);
        float t = 0f;
        while (t < 0.5f)
        {
            t += Time.unscaledDeltaTime;
            _panel.localScale = new Vector3(scale.x, scale.y * Mathf.Max(0.01f, OfflineFx.EaseOutBack(t / 0.5f)), scale.z);
            yield return null;
        }
        _panel.localScale = scale;

        var group = _panel.GetComponent<CanvasGroup>();
        if (group == null)
            group = _panel.gameObject.AddComponent<CanvasGroup>();
        foreach (float alpha in new[] { 0.4f, 1f, 0.6f, 1f })
        {
            group.alpha = alpha;
            yield return new WaitForSecondsRealtime(0.05f);
        }

        // Cards slide in one after the other.
        for (int i = 0; i < _cards.Count; i++)
        {
            StartCoroutine(SlideIn(_cards[i].Rect, i * 0.12f));
        }
    }

    private static IEnumerator SlideIn(RectTransform rect, float delay)
    {
        Vector2 target = rect.anchoredPosition;
        rect.anchoredPosition = target + new Vector2(-120f, 0f);
        var group = rect.gameObject.AddComponent<CanvasGroup>();
        group.alpha = 0f;
        yield return new WaitForSecondsRealtime(delay);
        float t = 0f;
        while (t < 0.4f && rect != null)
        {
            t += Time.unscaledDeltaTime;
            float k = OfflineFx.EaseOut(t / 0.4f);
            rect.anchoredPosition = Vector2.Lerp(target + new Vector2(-120f, 0f), target, k);
            group.alpha = k;
            yield return null;
        }
        if (rect != null)
        {
            rect.anchoredPosition = target;
            group.alpha = 1f;
        }
    }

    private static IEnumerator Typewriter(TMP_Text text, string full, float delay)
    {
        text.text = string.Empty;
        yield return new WaitForSecondsRealtime(delay + 0.5f);
        for (int i = 1; i <= full.Length; i++)
        {
            if (text == null)
                yield break;
            text.text = full.Substring(0, i) + (i < full.Length ? "_" : string.Empty);
            yield return new WaitForSecondsRealtime(0.04f);
        }
    }

    // ---------- Audio ----------

    /// <summary>Low engine hum loop (two detuned tones + slow wobble), seamless at 4 s.</summary>
    private static AudioClip HumClip()
    {
        const int rate = 22050;
        const int seconds = 4;
        int count = rate * seconds;
        float[] samples = new float[count];
        for (int i = 0; i < count; i++)
        {
            float t = i / (float)rate;
            float wobble = 0.75f + 0.25f * Mathf.Sin(2f * Mathf.PI * 0.25f * t);
            samples[i] = (Mathf.Sin(2f * Mathf.PI * 55f * t) * 0.5f
                          + Mathf.Sin(2f * Mathf.PI * 82.5f * t) * 0.25f
                          + Mathf.Sin(2f * Mathf.PI * 110f * t) * 0.12f) * wobble * 0.5f;
        }
        AudioClip clip = AudioClip.Create("Offline Menu Hum", count, 1, rate, false);
        clip.SetData(samples, 0);
        return clip;
    }

    private void PlayWhoosh()
    {
        const int rate = 22050;
        int count = (int)(rate * 1.7f);
        float[] samples = new float[count];
        var random = new System.Random(7);
        float low = 0f;
        for (int i = 0; i < count; i++)
        {
            float k = i / (float)count;
            // Filtered noise opening up + a rising tone.
            float cutoff = Mathf.Lerp(0.02f, 0.35f, k * k);
            low += cutoff * ((float)random.NextDouble() * 2f - 1f - low);
            float t = i / (float)rate;
            float tone = Mathf.Sin(2f * Mathf.PI * (80f * t + 160f * t * t * t));
            float envelope = Mathf.Clamp01(k * 3f) * (1f - Mathf.Pow(k, 6f));
            samples[i] = (low * 0.9f + tone * 0.25f) * envelope * 0.6f;
        }
        AudioClip clip = AudioClip.Create("Offline Warp", count, 1, rate, false);
        clip.SetData(samples, 0);
        AudioSource source = gameObject.AddComponent<AudioSource>();
        source.spatialBlend = 0f;
        source.volume = 0.5f;
        source.PlayOneShot(clip);
    }

    // ---------- UI helpers ----------

    private static void Restyle(TMP_Text text, float size, Color color)
    {
        if (text == null)
            return;
        if (_font != null)
        {
            text.font = _font;
            text.fontSharedMaterial = _font.material;
        }
        text.fontSize = size;
        text.color = color;
        text.textWrappingMode = TextWrappingModes.NoWrap;
    }

    private static TMP_Text AddText(Transform parent, string name, string text, float size, Color color, Vector2 position, Vector2 box)
    {
        // Built from scratch with the font set first: switching the font of an existing text (with an outline
        // material instance already made for the default font) leaves the old atlas material and scrambles glyphs.
        var go = new GameObject(name, typeof(RectTransform));
        var rect = (RectTransform)go.transform;
        rect.SetParent(parent, false);
        rect.sizeDelta = box;
        rect.anchoredPosition = position;
        var tmp = go.AddComponent<TextMeshProUGUI>();
        if (_font != null)
        {
            tmp.font = _font;
            tmp.fontSharedMaterial = _font.material;
        }
        tmp.text = text;
        tmp.fontSize = size;
        tmp.color = color;
        tmp.fontStyle = FontStyles.Bold;
        tmp.alignment = TextAlignmentOptions.Center;
        tmp.textWrappingMode = TextWrappingModes.NoWrap;
        tmp.raycastTarget = false;
        return tmp;
    }

    private static Image Image(Transform parent, string name, Sprite sprite, Color color, Vector2 size, Vector2 position) =>
        OfflineFx.AddImage(parent, name, sprite, color, size, position);

    private static Image Line(Transform parent, Vector2 position, Vector2 size, Color color, float alpha) =>
        OfflineFx.AddImage(parent, "Line", OfflineFx.White, new Color(color.r, color.g, color.b, alpha), size, position);

    private void OnDestroy()
    {
        if (_dust != null)
            Destroy(_dust.gameObject);
        if (_light != null)
        {
            _light.color = _lightBase;
            _light.intensity = _lightIntensity;
        }
    }
}

/// <summary>Tells whether the controller ray is on a menu card.</summary>
public sealed class OfflineMenuHover : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    public bool IsOver { get; private set; }
    public event Action Entered;

    public void OnPointerEnter(PointerEventData eventData)
    {
        IsOver = true;
        Entered?.Invoke();
    }

    public void OnPointerExit(PointerEventData eventData) => IsOver = false;

    private void OnDisable() => IsOver = false;
}
