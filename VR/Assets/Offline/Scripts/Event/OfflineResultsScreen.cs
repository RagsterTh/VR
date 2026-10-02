using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// End-of-session screen (offline): what the visitor did, the score counting up, the grade, a 3-letter name entry
/// when the score makes the Top 10 and the ranking of the mode. Built at runtime inside the entry menu's panel
/// (same canvas, so the controller ray already works). Advances by itself so an event queue never gets stuck.
/// </summary>
public sealed class OfflineResultsScreen : MonoBehaviour
{
    private const string Letters = "ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789";
    private static readonly Color Gold = new(1f, 0.82f, 0.25f);
    private static readonly Color Cyan = new(0.45f, 0.9f, 1f);

    [Tooltip("Seconds without any click before the screen confirms/continues by itself.")]
    [SerializeField] private float _autoAdvance = 25f;

    private RectTransform _root;
    private readonly List<GameObject> _hidden = new();
    private readonly int[] _initials = { 0, 0, 0 };
    private readonly TextMeshProUGUI[] _initialTexts = new TextMeshProUGUI[3];
    private readonly List<TextMeshProUGUI> _rankRows = new();
    private GameObject _entry;
    private TextMeshProUGUI _headline;
    private Button _mainButton;
    private TextMeshProUGUI _mainLabel;
    private float _idle;
    private bool _saved;
    private bool _done;
    private int _rank = -1;

    /// <summary>Shows the result inside <paramref name="panel"/> and returns when the visitor (or the timer) continues.</summary>
    public static IEnumerator Show(Transform panel)
    {
        var screen = panel.gameObject.AddComponent<OfflineResultsScreen>();
        yield return screen.Run(panel);
        Destroy(screen);
    }

    private IEnumerator Run(Transform panel)
    {
        foreach (Transform child in panel)
        {
            if (child.gameObject.activeSelf)
            {
                _hidden.Add(child.gameObject);
                child.gameObject.SetActive(false);
            }
        }

        Build(panel);
        yield return Reveal();

        while (!_done)
        {
            _idle += Time.unscaledDeltaTime;
            if (_idle >= _autoAdvance)
                Advance();
            yield return null;
        }

        // The result stays on the panel while the caller fades to the next scene (the menu underneath stays hidden).
    }

    // ---------- Layout ----------

    private void Build(Transform panel)
    {
        var go = new GameObject("[Offline] Results", typeof(RectTransform));
        _root = (RectTransform)go.transform;
        _root.SetParent(panel, false);
        _root.sizeDelta = new Vector2(1400f, 820f);

        Image back = OfflineFx.AddImage(_root, "Back", OfflineFx.White, new Color(0.03f, 0.06f, 0.12f, 1f), _root.sizeDelta);
        back.raycastTarget = true;

        Text("Title", "RESULTADO", 64f, Color.white, new Vector2(1300, 80), new Vector2(0, 350));
        _headline = Text("Headline", string.Empty, 34f, Gold, new Vector2(1300, 50), new Vector2(0, 290));

        // Left: what happened.
        string mode = OfflineScore.Mode == OfflineExperienceMode.FullExperience ? "EXPERIÊNCIA COMPLETA" : "COMBATE";
        Text("Mode", mode, 30f, Cyan, new Vector2(600, 40), new Vector2(-340, 230));

        // Right: ranking of this mode.
        Text("Rank Title", "TOP 10 DE HOJE", 30f, Cyan, new Vector2(600, 40), new Vector2(340, 230));
        for (int i = 0; i < OfflineLeaderboard.MaxEntries; i++)
        {
            TextMeshProUGUI row = Text("Rank " + i, string.Empty, 26f, Color.white, new Vector2(560, 34), new Vector2(340, 185 - i * 36));
            row.alignment = TextAlignmentOptions.Left;
            _rankRows.Add(row);
        }
        RefreshRanking(-1);

        // Bottom: name entry (shown only when the score makes the Top 10) + main button.
        _entry = new GameObject("Initials", typeof(RectTransform));
        var entryRect = (RectTransform)_entry.transform;
        entryRect.SetParent(_root, false);
        entryRect.anchoredPosition = new Vector2(-340, -300);
        Text("Entry Label", "SUAS INICIAIS", 26f, Cyan, new Vector2(500, 36), new Vector2(0, 116), entryRect);
        for (int i = 0; i < 3; i++)
        {
            int slot = i;
            float x = (i - 1) * 135f;
            MakeButton(entryRect, "Up " + i, "+", new Vector2(x, 68), new Vector2(110, 52), new Color(0.2f, 0.45f, 0.75f), () => Step(slot, 1));
            _initialTexts[i] = Text("Letter " + i, "A", 70f, Color.white, new Vector2(100, 80), new Vector2(x, 0), entryRect);
            MakeButton(entryRect, "Down " + i, "-", new Vector2(x, -68), new Vector2(110, 52), new Color(0.2f, 0.45f, 0.75f), () => Step(slot, -1));
        }
        _entry.SetActive(false);

        _mainButton = MakeButton(_root, "Main Button", "CONTINUAR", new Vector2(340, -310), new Vector2(520, 90), new Color(0.15f, 0.6f, 0.3f), Advance);
        _mainLabel = _mainButton.GetComponentInChildren<TextMeshProUGUI>();
        _mainButton.gameObject.SetActive(false);
    }

    private IEnumerator Reveal()
    {
        var lines = new List<(string label, string value)>
        {
            ("INIMIGOS ELIMINADOS", OfflineScore.Kills.ToString()),
            ("PRECISÃO", Mathf.RoundToInt(OfflineScore.Accuracy * 100f) + "%"),
            ("VIDA RESTANTE", Mathf.RoundToInt(OfflineScore.LifeRatio * 100f) + "%"),
        };
        if (OfflineScore.Mode == OfflineExperienceMode.FullExperience)
            lines.Add(("TRATAMENTOS CORRETOS", OfflineScore.MedicalCorrect + "/" + (OfflineScore.MedicalCorrect + OfflineScore.MedicalWrong)));

        for (int i = 0; i < lines.Count; i++)
        {
            TextMeshProUGUI label = Text("Stat " + i, lines[i].label, 28f, new Color(0.8f, 0.85f, 0.9f), new Vector2(420, 40), new Vector2(-420, 170 - i * 48));
            label.alignment = TextAlignmentOptions.Left;
            TextMeshProUGUI value = Text("Value " + i, lines[i].value, 34f, Color.white, new Vector2(200, 40), new Vector2(-160, 170 - i * 48));
            value.alignment = TextAlignmentOptions.Right;
            StartCoroutine(OfflineFx.Punch(value.transform, 0.3f, 0.2f));
            OfflineFx.HapticAll(0.15f, 0.03f);
            yield return new WaitForSecondsRealtime(0.3f);
        }

        Text("Score Label", "PONTUAÇÃO", 28f, Cyan, new Vector2(400, 40), new Vector2(-420, -30));
        TextMeshProUGUI score = Text("Score", "0", 96f, Gold, new Vector2(460, 110), new Vector2(-420, -100));
        int total = OfflineScore.Total;
        float t = 0f;
        while (t < 1.2f)
        {
            t += Time.unscaledDeltaTime;
            score.text = Mathf.RoundToInt(total * OfflineFx.EaseOut(t / 1.2f)).ToString();
            if (Time.frameCount % 5 == 0)
                OfflineFx.HapticAll(0.08f, 0.02f);
            yield return null;
        }
        score.text = total.ToString();
        StartCoroutine(OfflineFx.Punch(score.transform, 0.25f, 0.25f));

        string grade = OfflineScore.Grade;
        Color gradeColor = grade == "S" ? Gold : grade == "A" ? new Color(0.35f, 1f, 0.5f) : grade == "B" ? Cyan : new Color(0.8f, 0.8f, 0.8f);
        Text("Grade Label", "NOTA", 26f, Cyan, new Vector2(160, 36), new Vector2(-150, -30));
        TextMeshProUGUI gradeText = Text("Grade", grade, 110f, gradeColor, new Vector2(200, 130), new Vector2(-150, -105));
        StartCoroutine(OfflineFx.Punch(gradeText.transform, 0.6f, 0.35f));
        OfflineFx.Burst(gradeText.transform.position, gradeColor, 50, 1.5f, 0.03f, 1f, 0.2f, 0.05f);
        OfflineFx.HapticAll(0.6f, 0.2f);
        yield return new WaitForSecondsRealtime(0.5f);

        _rank = OfflineLeaderboard.RankFor(OfflineScore.Mode, total);
        if (_rank >= 0)
        {
            _headline.text = _rank == 0 ? "NOVO RECORDE! VOCÊ É O Nº 1" : $"VOCÊ ENTROU NO TOP 10  -  {_rank + 1}º LUGAR";
            StartCoroutine(OfflineFx.Punch(_headline.transform, 0.2f, 0.3f));
            _entry.SetActive(true);
            _mainLabel.text = "SALVAR";
        }
        else
        {
            IReadOnlyList<OfflineLeaderboard.Entry> top = OfflineLeaderboard.Top(OfflineScore.Mode);
            int missing = top.Count > 0 ? top[top.Count - 1].score - total + 1 : 0;
            _headline.text = missing > 0 ? $"FALTARAM {missing} PONTOS PARA O TOP 10" : string.Empty;
            _mainLabel.text = "CONTINUAR";
        }
        _mainButton.gameObject.SetActive(true);
        _idle = 0f;
    }

    // ---------- Actions ----------

    private void Step(int slot, int delta)
    {
        if (_saved)
            return;
        _idle = 0f;
        _initials[slot] = (_initials[slot] + delta + Letters.Length) % Letters.Length;
        _initialTexts[slot].text = Letters[_initials[slot]].ToString();
        StartCoroutine(OfflineFx.Punch(_initialTexts[slot].transform, 0.25f, 0.12f));
    }

    private void Advance()
    {
        _idle = 0f;
        if (_rank >= 0 && !_saved)
        {
            _saved = true;
            string name = string.Concat(Letters[_initials[0]], Letters[_initials[1]], Letters[_initials[2]]);
            int rank = OfflineLeaderboard.Add(OfflineScore.Mode, name, OfflineScore.Total, OfflineScore.Grade);
            RefreshRanking(rank);
            _entry.SetActive(false);
            _mainLabel.text = "CONTINUAR";
            _headline.text = $"SALVO: {name}";
            OfflineFx.HapticAll(0.5f, 0.12f);
            // Give a moment to see the name on the board before the timer moves on.
            _autoAdvance = Mathf.Min(_autoAdvance, 10f);
            return;
        }
        _done = true;
    }

    private void RefreshRanking(int highlight)
    {
        IReadOnlyList<OfflineLeaderboard.Entry> top = OfflineLeaderboard.Top(OfflineScore.Mode);
        for (int i = 0; i < _rankRows.Count; i++)
        {
            TextMeshProUGUI row = _rankRows[i];
            if (i < top.Count)
            {
                row.text = $"{i + 1}.<pos=14%>{top[i].name}<pos=45%>{top[i].score}<pos=82%>{top[i].grade}";
                row.color = i == highlight ? Gold : Color.white;
                if (i == highlight)
                    StartCoroutine(OfflineFx.Punch(row.transform, 0.2f, 0.3f));
            }
            else
            {
                row.text = $"{i + 1}.<pos=14%>---";
                row.color = new Color(1f, 1f, 1f, 0.35f);
            }
        }
    }

    // ---------- UI helpers ----------

    private TextMeshProUGUI Text(string name, string text, float size, Color color, Vector2 box, Vector2 position, Transform parent = null)
    {
        TextMeshProUGUI tmp = OfflineFx.AddText(parent != null ? parent : _root, name, text, size, color, box, position);
        tmp.enableWordWrapping = false;
        return tmp;
    }

    private Button MakeButton(Transform parent, string name, string label, Vector2 position, Vector2 size, Color color, Action onClick)
    {
        Image image = OfflineFx.AddImage(parent, name, OfflineFx.White, color, size, position);
        image.raycastTarget = true;
        var button = image.gameObject.AddComponent<Button>();
        button.targetGraphic = image;
        ColorBlock colors = button.colors;
        colors.highlightedColor = new Color(1.25f, 1.25f, 1.25f, 1f);
        colors.pressedColor = new Color(0.7f, 0.7f, 0.7f, 1f);
        button.colors = colors;
        button.onClick.AddListener(() => onClick());
        Text(name + " Label", label, Mathf.Min(44f, size.y * 0.6f), Color.white, size, Vector2.zero, image.transform);
        image.gameObject.AddComponent<OfflineButtonFeedback>();
        return button;
    }
}
