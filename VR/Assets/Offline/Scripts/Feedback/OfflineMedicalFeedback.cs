using System;
using System.Collections;
using System.Reflection;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.XR.Interaction.Toolkit.Interactables;

public sealed class OfflineMedicalFeedback : MonoBehaviour
{
    public static OfflineMedicalFeedback Instance { get; private set; }

    private static readonly Color Green = new(0.2f, 0.9f, 0.45f);
    private static readonly Color Red = new(0.95f, 0.25f, 0.2f);

    [SerializeField] private float _answerHold = 1.3f;
    [SerializeField] private float _celebrationTime = 3.5f;

    private MedicalQuestions _questions;
    private TMP_Text _report;
    private int _total = 3;
    private int _treated;
    private TextMeshProUGUI _progressText;
    private Image _progressFill;
    private float _progressShown;

    [Tooltip("Seconds to choose a treatment. Time out = counts as a wrong answer (the right one is explained).")]
    [SerializeField] private float _answerTime = OfflineScore.SlowAnswerSeconds;

    private GameObject _questionPanel;
    private bool _panelWasOpen;
    private bool _answering;
    private bool _timedOut;
    private float _questionStart = -1f;
    private RectTransform _timer;
    private Image _timerFill;
    private TextMeshProUGUI _timerText;
    private RectTransform _explanation;
    private TextMeshProUGUI _explanationText;

    private void Awake()
    {
        Instance = this;
    }

    private void OnDestroy()
    {
        if (Instance == this)
            Instance = null;
    }

    private IEnumerator Start()
    {
        yield return null;

        _questions = FindAnyObjectByType<MedicalQuestions>();
        if (_questions == null)
            yield break;

        _report = Field<TMP_Text>(_questions, "displayText");
        var panel = Field<GameObject>(_questions, "buttonPanel");
        _questionPanel = panel;
        var buttons = Field<Button[]>(_questions, "answerButtons");

        if (_report != null && _report.GetComponent<OfflineTypewriter>() == null)
            _report.gameObject.AddComponent<OfflineTypewriter>();
        if (panel != null && panel.GetComponent<OfflinePanelPop>() == null)
            panel.AddComponent<OfflinePanelPop>().Buttons = buttons;

        var manager = FindAnyObjectByType<MedicalEmergencyManager>();
        if (manager != null)
            _total = Mathf.Max(1, Field<int>(manager, "numberToActivate"));

        foreach (MedicalEmergency wound in FindObjectsByType<MedicalEmergency>(FindObjectsInactive.Include, FindObjectsSortMode.None))
        {
            if (wound.GetComponent<OfflineWoundMarker>() == null)
                wound.gameObject.AddComponent<OfflineWoundMarker>();
            TMP_Text label = Field<TMP_Text>(wound, "hoverLabel");
            if (label != null && label.GetComponent<OfflineTextFade>() == null)
                label.gameObject.AddComponent<OfflineTextFade>();
        }

        BuildProgress();
        BuildTimer();
        BuildExplanation();
    }

    private void Update()
    {
        UpdateTimer();
        if (_progressFill == null)
            return;
        float target = (float)_treated / _total;
        _progressShown = Mathf.MoveTowards(_progressShown, target, Time.unscaledDeltaTime * 1.2f);
        _progressFill.fillAmount = _progressShown;
    }


    private void BuildProgress()
    {
        if (_report == null)
            return;

        var reportRect = _report.rectTransform;
        var holder = new GameObject("[Offline] Progress", typeof(RectTransform));
        var rect = (RectTransform)holder.transform;
        rect.SetParent(reportRect, false);
        rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 1f);
        rect.pivot = new Vector2(0.5f, 1f);
        rect.localScale = Vector3.one;
        rect.localRotation = Quaternion.identity;

        float width = reportRect.rect.width * 0.9f;
        rect.sizeDelta = new Vector2(width, 90f);
        rect.anchoredPosition = new Vector2(0f, -6f);

        float fontSize = Mathf.Clamp(_report.fontSize * 0.75f, 10f, 40f);
        _progressText = OfflineFx.AddText(rect, "Label", ProgressLabel(), fontSize, Color.white, new Vector2(width, 60f), new Vector2(0f, 12f));
        _progressText.enableWordWrapping = true;
        OfflineFx.AddImage(rect, "Bar Back", OfflineFx.White, new Color(0f, 0f, 0f, 0.55f), new Vector2(width * 0.8f, 10f), new Vector2(0f, -28f));
        _progressFill = OfflineFx.AddImage(rect, "Bar Fill", OfflineFx.White, Green, new Vector2(width * 0.8f, 10f), new Vector2(0f, -28f));
        _progressFill.type = Image.Type.Filled;
        _progressFill.fillMethod = Image.FillMethod.Horizontal;
        _progressFill.fillAmount = 0f;
    }

    private string ProgressLabel() => $"FERIMENTOS TRATADOS  {_treated}/{_total}";


    private void BuildTimer()
    {
        if (_report == null)
            return;
        var go = new GameObject("[Offline] Answer Timer", typeof(RectTransform));
        _timer = (RectTransform)go.transform;
        _timer.SetParent(_report.rectTransform, false);
        _timer.anchorMin = _timer.anchorMax = new Vector2(0.5f, 0f);
        _timer.pivot = new Vector2(0.5f, 0f);
        _timer.localScale = Vector3.one;
        _timer.localRotation = Quaternion.identity;
        float width = _report.rectTransform.rect.width * 0.8f;
        _timer.sizeDelta = new Vector2(width, 60f);
        _timer.anchoredPosition = new Vector2(0f, 4f);
        OfflineFx.AddImage(_timer, "Back", OfflineFx.White, new Color(0f, 0f, 0f, 0.55f), new Vector2(width, 10f), new Vector2(0f, 8f));
        _timerFill = OfflineFx.AddImage(_timer, "Fill", OfflineFx.White, Green, new Vector2(width, 10f), new Vector2(0f, 8f));
        _timerFill.type = Image.Type.Filled;
        _timerFill.fillMethod = Image.FillMethod.Horizontal;
        float fontSize = Mathf.Clamp(_report.fontSize * 0.6f, 8f, 32f);
        _timerText = OfflineFx.AddText(_timer, "Seconds", string.Empty, fontSize, Color.white, new Vector2(width, 40f), new Vector2(0f, 34f));
        go.SetActive(false);
    }

    private void UpdateTimer()
    {
        if (_questionPanel == null)
            return;

        bool open = _questionPanel.activeInHierarchy;
        if (open && !_panelWasOpen && !_answering)
        {
            _questionStart = Time.unscaledTime;
            _timedOut = false;
            if (_timer != null)
                _timer.gameObject.SetActive(true);
        }
        _panelWasOpen = open;

        if (!open || _answering || _questionStart < 0f)
        {
            if (_timer != null && !_answering)
                _timer.gameObject.SetActive(false);
            return;
        }

        float left = _answerTime - (Time.unscaledTime - _questionStart);
        if (_timerFill != null)
        {
            float k = Mathf.Clamp01(left / _answerTime);
            _timerFill.fillAmount = k;
            bool hurry = left <= 5f;
            _timerFill.color = hurry ? (Mathf.Repeat(Time.unscaledTime, 0.5f) < 0.25f ? Red : Color.white) : Color.Lerp(Red, Green, k * 1.5f);
            _timerText.text = hurry ? $"DECIDA RÁPIDO!  {Mathf.CeilToInt(Mathf.Max(0f, left))}s" : $"{Mathf.CeilToInt(left)}s";
            _timerText.color = hurry ? Red : Color.white;
        }

        if (left <= 0f)
        {
            _timedOut = true;
            OfflineFx.HapticAll(0.6f, 0.2f);
            _questions.CheckAnswer(-1);
        }
    }


    private void BuildExplanation()
    {
        if (_report == null)
            return;
        var go = new GameObject("[Offline] Explanation", typeof(RectTransform));
        _explanation = (RectTransform)go.transform;
        _explanation.SetParent(_report.rectTransform, false);
        _explanation.anchorMin = Vector2.zero;
        _explanation.anchorMax = Vector2.one;
        _explanation.offsetMin = new Vector2(-10f, -10f);
        _explanation.offsetMax = new Vector2(10f, 10f);
        _explanation.localScale = Vector3.one;
        _explanation.localRotation = Quaternion.identity;
        go.AddComponent<CanvasGroup>();

        Image back = go.AddComponent<Image>();
        back.sprite = OfflineFx.White;
        back.color = new Color(0.02f, 0.05f, 0.08f, 0.96f);
        back.raycastTarget = false;

        var textGo = new GameObject("Text", typeof(RectTransform));
        var textRect = (RectTransform)textGo.transform;
        textRect.SetParent(_explanation, false);
        textRect.anchorMin = Vector2.zero;
        textRect.anchorMax = Vector2.one;
        textRect.offsetMin = new Vector2(18f, 12f);
        textRect.offsetMax = new Vector2(-18f, -12f);
        _explanationText = textGo.AddComponent<TextMeshProUGUI>();
        _explanationText.fontSize = Mathf.Clamp(_report.fontSize * 0.8f, 8f, 48f);
        _explanationText.enableAutoSizing = true;
        _explanationText.fontSizeMin = 6f;
        _explanationText.fontSizeMax = _explanationText.fontSize;
        _explanationText.alignment = TextAlignmentOptions.Center;
        _explanationText.textWrappingMode = TextWrappingModes.Normal;
        _explanationText.raycastTarget = false;
        go.SetActive(false);
    }

    private IEnumerator ShowExplanation(string text, float duration)
    {
        if (_explanation == null || string.IsNullOrEmpty(text))
            yield break;
        _explanation.gameObject.SetActive(true);
        _explanationText.text = text;
        var group = _explanation.GetComponent<CanvasGroup>();
        float t = 0f;
        while (t < duration)
        {
            t += Time.unscaledDeltaTime;
            group.alpha = Mathf.Clamp01(Mathf.Min(t / 0.25f, (duration - t) / 0.25f));
            yield return null;
        }
        _explanation.gameObject.SetActive(false);
    }

    private string ExplanationFor(bool correct, Button clicked, MedicalEmergency wound)
    {
        MedicalData data = wound != null ? wound.GetAssignedMedicalData() : null;
        if (data == null)
            return string.Empty;
        TreatmentType right = data.treatmentType;
        string why = OfflineMedicalGuide.Explain(right);
        string rightLabel = OfflineMedicalGuide.Label(right).ToUpperInvariant();

        if (correct)
            return $"<color=#33E673><b>CORRETO: {rightLabel}</b></color>\n<size=85%>{why}</size>";

        string header = _timedOut
            ? $"<color=#F2C230><b>TEMPO ESGOTADO</b></color>\nO certo era <b>{rightLabel}</b>"
            : $"<color=#F24033><b>O CERTO ERA: {rightLabel}</b></color>";
        string chosenText = string.Empty;
        TMP_Text label = clicked != null ? clicked.GetComponentInChildren<TMP_Text>() : null;
        if (!_timedOut && label != null && OfflineMedicalGuide.TryParse(label.text, out TreatmentType chosen))
            chosenText = $"\n\n<size=80%><color=#FFB3AA>{OfflineMedicalGuide.ExplainWrong(chosen)}</color></size>";
        return $"{header}\n<size=85%>{why}</size>{chosenText}";
    }


    public void ShowAnswer(bool correct, Button clicked, Button rightButton, MedicalEmergency wound, Action resolve)
    {
        float seconds = _questionStart >= 0f ? Time.unscaledTime - _questionStart : -1f;
        if (_timedOut)
            seconds = _answerTime;
        OfflineScore.AddTreatment(correct, seconds);
        _answering = true;
        _questionStart = -1f;
        if (_timer != null)
            _timer.gameObject.SetActive(false);
        StartCoroutine(AnswerRoutine(correct, clicked, rightButton, wound, resolve));
    }

    private IEnumerator AnswerRoutine(bool correct, Button clicked, Button rightButton, MedicalEmergency wound, Action resolve)
    {
        Button[] buttons = Field<Button[]>(_questions, "answerButtons") ?? Array.Empty<Button>();
        foreach (Button b in buttons)
            if (b != null) b.interactable = false;

        Image clickedImage = clicked != null ? clicked.targetGraphic as Image : null;
        Image rightImage = rightButton != null ? rightButton.targetGraphic as Image : null;
        Color clickedColor = clickedImage != null ? clickedImage.color : Color.white;
        Color rightColor = rightImage != null ? rightImage.color : Color.white;
        Vector3 textPoint = _report != null ? _report.transform.position : (wound != null ? wound.transform.position : transform.position);

        if (correct)
        {
            if (clickedImage != null) clickedImage.color = Green;
            if (clicked != null) StartCoroutine(OfflineFx.Punch(clicked.transform, 0.18f, 0.25f));
            OfflineFx.FloatingText(textPoint + Vector3.up * 0.15f, "CORRETO!", Green, 110f, 1.2f);
            OfflineViewOverlay.Flash(Green, 0.35f, 0.5f);
            OfflineFx.HapticAll(0.45f, 0.12f);
            if (wound != null)
                StartCoroutine(Heal(wound.transform));
            _treated = Mathf.Min(_total, _treated + 1);
            if (_progressText != null)
            {
                _progressText.text = ProgressLabel();
                StartCoroutine(OfflineFx.Punch(_progressText.transform, 0.3f, 0.25f));
            }
        }
        else
        {
            if (clickedImage != null) clickedImage.color = Red;
            if (clicked != null) StartCoroutine(OfflineFx.Shake(clicked.transform, 14f, 0.4f));
            OfflineFx.FloatingText(textPoint + Vector3.up * 0.15f, "INCORRETO", Red, 110f, 1.2f);
            OfflineViewOverlay.Flash(Red, 0.6f, 0.45f);
            OfflineFx.HapticAll(0.9f, 0.25f);
            if (rightImage != null)
                StartCoroutine(Blink(rightImage, Green, rightColor, _answerHold));
        }

        float hold = correct ? Mathf.Max(_answerHold, 3.5f) : Mathf.Max(_answerHold, 7f);
        string explanation = ExplanationFor(correct, clicked, wound);
        if (!string.IsNullOrEmpty(explanation))
            StartCoroutine(ShowExplanation(explanation, hold));
        yield return new WaitForSecondsRealtime(hold);

        if (clickedImage != null) clickedImage.color = clickedColor;
        if (rightImage != null) rightImage.color = rightColor;
        foreach (Button b in buttons)
            if (b != null) b.interactable = true;

        _answering = false;
        _timedOut = false;
        resolve?.Invoke();
    }

    private static IEnumerator Heal(Transform wound)
    {
        Vector3 center = wound.position;
        OfflineFx.Burst(center, Green, 40, 1.6f, 0.06f, 0.9f, -0.3f, 0.08f);
        OfflineFx.Burst(center, Color.white, 14, 2.5f, 0.035f, 0.5f);
        Vector3 start = wound.localScale;
        float t = 0f;
        while (t < 0.45f && wound != null)
        {
            t += Time.unscaledDeltaTime;
            float k = t / 0.45f;
            wound.localScale = start * (k < 0.25f ? Mathf.Lerp(1f, 1.25f, k / 0.25f) : Mathf.Lerp(1.25f, 0f, (k - 0.25f) / 0.75f));
            yield return null;
        }
    }

    private static IEnumerator Blink(Graphic graphic, Color on, Color off, float duration)
    {
        float t = 0f;
        while (t < duration && graphic != null)
        {
            t += Time.unscaledDeltaTime;
            graphic.color = Mathf.Repeat(t, 0.3f) < 0.18f ? on : off;
            yield return null;
        }
    }


    public void Celebrate(Action done)
    {
        StartCoroutine(CelebrateRoutine(done));
    }

    private IEnumerator CelebrateRoutine(Action done)
    {
        Camera head = Camera.main;
        Vector3 front = head != null ? head.transform.position + Vector3.ProjectOnPlane(head.transform.forward, Vector3.up).normalized * 1.4f : transform.position;
        OfflineFx.FloatingText(front + Vector3.up * 0.1f, "PACIENTE ESTABILIZADO!", Green, 120f, _celebrationTime, 0.15f);
        OfflineViewOverlay.Flash(Green, 0.5f, 1f);

        Color[] colors = { Green, new(0.3f, 0.7f, 1f), new(1f, 0.85f, 0.2f), new(1f, 0.4f, 0.7f), Color.white };
        for (int i = 0; i < 6; i++)
        {
            Vector3 offset = new Vector3(UnityEngine.Random.Range(-0.8f, 0.8f), UnityEngine.Random.Range(0.1f, 0.7f), UnityEngine.Random.Range(-0.2f, 0.4f));
            OfflineFx.Burst(front + offset, colors[i % colors.Length], 45, 2.8f, 0.07f, 1.3f, 0.35f, 0.1f);
            OfflineFx.HapticAll(0.35f, 0.06f);
            yield return new WaitForSecondsRealtime(0.25f);
        }

        yield return new WaitForSecondsRealtime(Mathf.Max(0f, _celebrationTime - 1.5f));
        done?.Invoke();
    }


    private static T Field<T>(object target, string name)
    {
        FieldInfo field = target.GetType().GetField(name, BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);
        return field != null && field.GetValue(target) is T value ? value : default;
    }
}

public sealed class OfflineTypewriter : MonoBehaviour
{
    [SerializeField] private float _charactersPerSecond = 70f;
    private TMP_Text _text;
    private string _last;
    private float _shown;

    private void Awake() => _text = GetComponent<TMP_Text>();

    private void LateUpdate()
    {
        if (_text == null)
            return;
        if (_text.text != _last)
        {
            _last = _text.text;
            _shown = 0f;
        }
        _shown += Time.unscaledDeltaTime * _charactersPerSecond;
        _text.maxVisibleCharacters = Mathf.FloorToInt(_shown);
    }
}

public sealed class OfflineTextFade : MonoBehaviour
{
    private TMP_Text _text;
    private string _last;
    private float _alpha = 1f;

    private void Awake() => _text = GetComponent<TMP_Text>();

    private void LateUpdate()
    {
        if (_text == null)
            return;
        if (_text.text != _last)
        {
            _last = _text.text;
            if (!string.IsNullOrEmpty(_last))
                _alpha = 0f;
        }
        _alpha = Mathf.MoveTowards(_alpha, 1f, Time.unscaledDeltaTime * 5f);
        _text.alpha = _alpha;
    }
}

public sealed class OfflinePanelPop : MonoBehaviour
{
    public Button[] Buttons;
    private CanvasGroup _group;
    private Vector3 _baseScale;
    private bool _captured;

    private void OnEnable()
    {
        if (!_captured)
        {
            _baseScale = transform.localScale;
            _group = GetComponent<CanvasGroup>();
            if (_group == null)
                _group = gameObject.AddComponent<CanvasGroup>();
            _captured = true;
        }
        StartCoroutine(Pop());
    }

    private IEnumerator Pop()
    {
        float t = 0f;
        if (Buttons != null)
            foreach (Button b in Buttons)
                if (b != null) b.transform.localScale = Vector3.zero;

        while (t < 0.6f)
        {
            t += Time.unscaledDeltaTime;
            float k = Mathf.Clamp01(t / 0.25f);
            transform.localScale = _baseScale * Mathf.LerpUnclamped(0.85f, 1f, OfflineFx.EaseOutBack(k));
            _group.alpha = k;
            if (Buttons != null)
            {
                for (int i = 0; i < Buttons.Length; i++)
                {
                    if (Buttons[i] == null) continue;
                    float bk = Mathf.Clamp01((t - 0.12f - i * 0.1f) / 0.22f);
                    Buttons[i].transform.localScale = Vector3.one * OfflineFx.EaseOutBack(bk);
                }
            }
            yield return null;
        }
        transform.localScale = _baseScale;
        _group.alpha = 1f;
        if (Buttons != null)
            foreach (Button b in Buttons)
                if (b != null) b.transform.localScale = Vector3.one;
    }
}

public sealed class OfflineWoundMarker : MonoBehaviour
{
    private RectTransform _canvas;
    private Image _glow;
    private Image _ring;
    private Image _pin;
    private float _size = 0.15f;
    private float _hover;

    private void Start()
    {
        Renderer renderer = GetComponentInChildren<Renderer>(true);
        if (renderer != null)
            _size = Mathf.Clamp(renderer.bounds.size.magnitude, 0.08f, 0.6f);

        _canvas = OfflineFx.CreateCanvas("[Offline] Wound Marker", transform, new Vector2(1000, 1000), 0.001f, 120);
        _canvas.localPosition = Vector3.zero;
        Vector3 lossy = transform.lossyScale;
        float parentScale = Mathf.Max(0.0001f, (Mathf.Abs(lossy.x) + Mathf.Abs(lossy.y) + Mathf.Abs(lossy.z)) / 3f);
        _canvas.localScale = Vector3.one * (0.001f / parentScale);
        _canvas.gameObject.AddComponent<OfflineBillboard>();

        float px = _size * 1000f;
        _glow = OfflineFx.AddImage(_canvas, "Glow", OfflineFx.Soft, new Color(1f, 0.3f, 0.25f, 0.5f), new Vector2(px * 2.2f, px * 2.2f));
        _ring = OfflineFx.AddImage(_canvas, "Ring", OfflineFx.Ring, new Color(1f, 0.85f, 0.3f, 0.9f), new Vector2(px * 1.6f, px * 1.6f));
        _pin = OfflineFx.AddImage(_canvas, "Pin", OfflineFx.White, new Color(1f, 0.85f, 0.3f, 1f), new Vector2(px * 0.35f, px * 0.35f));
        _pin.rectTransform.localRotation = Quaternion.Euler(0f, 0f, 45f);

        var interactable = GetComponent<XRBaseInteractable>();
        if (interactable != null)
        {
            interactable.hoverEntered.AddListener(_ => { _hover = 1f; OfflineFx.HapticAll(0.15f, 0.04f); });
            interactable.hoverExited.AddListener(_ => _hover = 0f);
        }
    }

    private void Update()
    {
        if (_canvas == null)
            return;
        float t = Time.unscaledTime;
        float pulse = 0.5f + 0.5f * Mathf.Sin(t * 4f);
        float px = _size * 1000f;
        float hoverBoost = 1f + 0.25f * _hover;

        _glow.color = new Color(1f, 0.3f, 0.25f, (0.25f + 0.3f * pulse) * hoverBoost);
        _glow.rectTransform.localScale = Vector3.one * (0.9f + 0.2f * pulse) * hoverBoost;
        _ring.rectTransform.localScale = Vector3.one * (1f + 0.15f * Mathf.Repeat(t, 1f)) * hoverBoost;
        _ring.color = new Color(1f, 0.85f, 0.3f, 0.9f * (1f - Mathf.Repeat(t, 1f)));
        _pin.rectTransform.anchoredPosition = new Vector2(0f, px * 1.3f + Mathf.Sin(t * 3f) * px * 0.15f);
    }
}
