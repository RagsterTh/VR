using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Quick shooting practice before the battle (offline combat scenes): three holographic targets in front of the
/// visitor and a panel telling which button shoots. Ends when the targets are down or after a timeout, so the
/// queue of an event never waits for someone who did not get it. While it runs the battle does not start
/// (OfflineAutoAdvance waits for <see cref="Blocking"/>) and shots do not count for the score.
/// </summary>
public sealed class OfflineTutorial : MonoBehaviour
{
    /// <summary>Set by OfflineModeMenu from the Inspector.</summary>
    public static bool Enabled = true;
    /// <summary>Seconds before the practice gives up and the battle starts anyway.</summary>
    public static float Timeout = 25f;

    /// <summary>True while the practice is pending or running in the current scene.</summary>
    public static bool Blocking { get; private set; }

    private static bool _doneThisVisit;

    private const int TargetCount = 3;
    private static readonly Color Cyan = new(0.45f, 0.9f, 1f);
    private static readonly Color Gold = new(1f, 0.82f, 0.25f);
    private static readonly Color Green = new(0.35f, 1f, 0.5f);

    private readonly List<OfflineTutorialTarget> _targets = new();
    private RectTransform _panel;
    private TextMeshProUGUI _title;
    private TextMeshProUGUI _line;
    private TextMeshProUGUI _hint;
    private TextMeshProUGUI _progress;
    private int _hits;

    /// <summary>New visitor (a mode was chosen on the menu): the practice is shown again.</summary>
    public static void ResetForNewVisitor() => _doneThisVisit = false;

    /// <summary>Called by the scene bootstrap, in Awake, for combat scenes.</summary>
    public static void Prepare(GameObject host)
    {
        // Once per visitor: a retry after a defeat goes straight to the battle.
        if (!Enabled || _doneThisVisit || host.GetComponent<OfflineTutorial>() != null)
            return;
        Blocking = true;
        OfflineScore.Counting = false;
        host.AddComponent<OfflineTutorial>();
    }

    private IEnumerator Start()
    {
        // The player (and the guns) are spawned by GameController; wait until the rig is in place.
        float waited = 0f;
        while (waited < 8f && !PlayerReady())
        {
            waited += Time.unscaledDeltaTime;
            yield return null;
        }

        Camera head = Camera.main;
        if (head == null || !PlayerReady())
        {
            Finish();
            yield break;
        }

        // Let the fade-in finish before anything appears.
        yield return new WaitForSecondsRealtime(1.2f);
        Build(head.transform);

        float elapsed = 0f;
        while (_hits < TargetCount && elapsed < Timeout)
        {
            elapsed += Time.unscaledDeltaTime;
            // Last seconds: tell the visitor the battle is about to start anyway.
            float left = Timeout - elapsed;
            if (left < 5f && _hint != null)
                _hint.text = $"O COMBATE COMEÇA EM {Mathf.CeilToInt(left)}...";
            yield return null;
        }

        bool cleared = _hits >= TargetCount;
        foreach (OfflineTutorialTarget target in _targets)
            if (target != null) target.Dismiss();

        if (cleared)
        {
            _title.text = "PERFEITO!";
            _title.color = Green;
            _line.text = "VIDA BAIXA?";
            _hint.text = "APERTE  A  ou  X  PARA SE CURAR";
            _progress.text = string.Empty;
            StartCoroutine(OfflineFx.Punch(_title.transform, 0.3f, 0.3f));
            OfflineFx.Burst(_panel.position, Green, 50, 2f, 0.05f, 1.2f, 0.2f, 0.8f);
            OfflineFx.HapticAll(0.5f, 0.2f);
            yield return new WaitForSecondsRealtime(3.5f);
        }
        else
        {
            _title.text = "PREPARE-SE!";
            _title.color = Gold;
            _line.text = "BOTÃO LATERAL (GRIP) ATIRA";
            _hint.text = "A  ou  X  CURA";
            _progress.text = string.Empty;
            yield return new WaitForSecondsRealtime(2.5f);
        }

        yield return FadePanel();
        Finish();
    }

    private static bool PlayerReady()
    {
        if (Camera.main == null)
            return false;
        OfflinePlayerRig rig = FindAnyObjectByType<OfflinePlayerRig>();
        return rig != null && rig.IsPlaced && FindAnyObjectByType<Gun>() != null;
    }

    private void Finish()
    {
        _doneThisVisit = true;
        Blocking = false;
        OfflineScore.Counting = true;
        if (_panel != null)
            Destroy(_panel.gameObject);
        Destroy(this);
    }

    private void OnDestroy()
    {
        // Scene left in the middle of the practice (operator skip, kiosk reset).
        Blocking = false;
        OfflineScore.Counting = true;
        foreach (OfflineTutorialTarget target in _targets)
            if (target != null) Destroy(target.gameObject);
        if (_panel != null)
            Destroy(_panel.gameObject);
    }

    // ---------- Scene ----------

    private void Build(Transform head)
    {
        Vector3 forward = Vector3.ProjectOnPlane(head.forward, Vector3.up);
        if (forward.sqrMagnitude < 0.0001f)
            forward = Vector3.forward;
        forward.Normalize();
        Vector3 right = Vector3.Cross(Vector3.up, forward);
        Vector3 eye = head.position;

        _panel = OfflineFx.CreateCanvas("[Offline] Tutorial Panel", null, new Vector2(1100, 400), 0.0025f, 160);
        _panel.position = eye + forward * 4.5f + Vector3.up * 1.3f;
        _panel.rotation = Quaternion.LookRotation(_panel.position - eye, Vector3.up);
        _panel.gameObject.AddComponent<CanvasGroup>();
        OfflineFx.AddImage(_panel, "Back", OfflineFx.White, new Color(0.03f, 0.06f, 0.12f, 0.93f), new Vector2(1100, 400));
        OfflineFx.AddImage(_panel, "Line", OfflineFx.White, Cyan, new Vector2(1100, 6), new Vector2(0, 197));
        _title = OfflineFx.AddText(_panel, "Title", "TREINAMENTO", 46f, Cyan, new Vector2(1050, 60), new Vector2(0, 150));
        _line = OfflineFx.AddText(_panel, "Instruction", "APONTE O LASER NOS ALVOS", 66f, Color.white, new Vector2(1050, 80), new Vector2(0, 70));
        _hint = OfflineFx.AddText(_panel, "Button", "ATIRE COM O BOTÃO LATERAL (GRIP)", 50f, Gold, new Vector2(1050, 70), new Vector2(0, -15));
        _progress = OfflineFx.AddText(_panel, "Progress", ProgressLabel(), 56f, Color.white, new Vector2(1050, 70), new Vector2(0, -125));
        foreach (TextMeshProUGUI text in new[] { _title, _line, _hint, _progress })
            text.enableWordWrapping = false;
        StartCoroutine(PopIn(_panel));

        // Three targets spread in front, the middle one a little higher.
        float[] side = { -1.6f, 0f, 1.6f };
        float[] height = { -0.15f, 0.3f, -0.15f };
        for (int i = 0; i < TargetCount; i++)
        {
            Vector3 position = eye + forward * 5f + right * side[i] + Vector3.up * height[i];
            position = PullInFrontOfWalls(eye, position, head.root);
            _targets.Add(OfflineTutorialTarget.Create(this, position, eye, 0.25f + i * 0.2f));
        }

        OfflineFx.HapticAll(0.3f, 0.1f);
    }

    /// <summary>Scenery between the player and the target: bring the target in front of it.</summary>
    private static Vector3 PullInFrontOfWalls(Vector3 eye, Vector3 target, Transform playerRoot)
    {
        Vector3 direction = target - eye;
        float distance = direction.magnitude;
        direction /= distance;
        float nearest = distance;
        foreach (RaycastHit hit in Physics.RaycastAll(eye, direction, distance, ~0, QueryTriggerInteraction.Ignore))
        {
            if (hit.collider.transform.IsChildOf(playerRoot))
                continue;
            nearest = Mathf.Min(nearest, hit.distance - 0.5f);
        }
        return eye + direction * Mathf.Max(2f, nearest);
    }

    public void TargetHit(OfflineTutorialTarget target)
    {
        _hits++;
        if (_progress != null)
        {
            _progress.text = ProgressLabel();
            _progress.color = Green;
            StartCoroutine(OfflineFx.Punch(_progress.transform, 0.35f, 0.25f));
        }
    }

    private string ProgressLabel() => $"ALVOS  {_hits}/{TargetCount}";

    private static IEnumerator PopIn(RectTransform panel)
    {
        Vector3 scale = panel.localScale;
        float t = 0f;
        while (t < 0.35f && panel != null)
        {
            t += Time.unscaledDeltaTime;
            panel.localScale = scale * OfflineFx.EaseOutBack(t / 0.35f);
            yield return null;
        }
        if (panel != null)
            panel.localScale = scale;
    }

    private IEnumerator FadePanel()
    {
        if (_panel == null)
            yield break;
        CanvasGroup group = _panel.GetComponent<CanvasGroup>();
        float t = 0f;
        while (t < 0.4f)
        {
            t += Time.unscaledDeltaTime;
            group.alpha = 1f - t / 0.4f;
            yield return null;
        }
    }
}

/// <summary>Holographic practice target. Hit by the player's bullets like any other IShootable.</summary>
public sealed class OfflineTutorialTarget : MonoBehaviour, IShootable
{
    private static readonly Color Orange = new(1f, 0.5f, 0.15f);

    private OfflineTutorial _owner;
    private RectTransform _face;
    private Image _ring;
    private Vector3 _basePosition;
    private float _delay;
    private float _age;
    private float _seed;
    private bool _down;

    public static OfflineTutorialTarget Create(OfflineTutorial owner, Vector3 position, Vector3 eye, float delay)
    {
        var go = new GameObject("[Offline] Tutorial Target");
        go.transform.position = position;
        // +Z away from the player: the face is seen from the front and the hit volume extends behind it.
        go.transform.rotation = Quaternion.LookRotation(position - eye, Vector3.up);

        var target = go.AddComponent<OfflineTutorialTarget>();
        target._owner = owner;
        target._basePosition = position;
        target._delay = delay;
        target._seed = Random.value * 10f;

        target._face = OfflineFx.CreateCanvas("Face", go.transform, new Vector2(900, 900), 0.001f, 140);
        OfflineFx.AddImage(target._face, "Glow", OfflineFx.Soft, new Color(Orange.r, Orange.g, Orange.b, 0.45f), new Vector2(900, 900));
        target._ring = OfflineFx.AddImage(target._face, "Outer", OfflineFx.Ring, Color.white, new Vector2(640, 640));
        OfflineFx.AddImage(target._face, "Disc", OfflineFx.Circle, new Color(Orange.r, Orange.g, Orange.b, 0.85f), new Vector2(460, 460));
        OfflineFx.AddImage(target._face, "Inner", OfflineFx.Ring, Color.white, new Vector2(330, 330));
        OfflineFx.AddImage(target._face, "Center", OfflineFx.Circle, Color.white, new Vector2(120, 120));
        target._face.localScale = Vector3.zero;

        // Bullets are fast: a deep volume behind the face so none of them skips over it between physics steps.
        var box = go.AddComponent<BoxCollider>();
        box.isTrigger = true;
        box.size = new Vector3(0.75f, 0.75f, 3f);
        box.center = new Vector3(0f, 0f, 1.5f);
        box.enabled = false;
        return target;
    }

    private void Update()
    {
        _age += Time.unscaledDeltaTime;
        float shown = _age - _delay;
        if (shown < 0f || _down)
            return;

        if (shown < 0.4f)
        {
            _face.localScale = Vector3.one * (0.001f * OfflineFx.EaseOutBack(shown / 0.4f));
        }
        else
        {
            GetComponent<BoxCollider>().enabled = true;
            float pulse = 1f + 0.05f * Mathf.Sin((_age + _seed) * 4f);
            _face.localScale = Vector3.one * (0.001f * pulse);
        }

        transform.position = _basePosition + Vector3.up * (0.06f * Mathf.Sin((_age + _seed) * 1.6f));
        _ring.rectTransform.localRotation = Quaternion.Euler(0f, 0f, _age * 40f);
    }

    public void Hit()
    {
        if (_down)
            return;
        _down = true;

        Vector3 center = transform.position;
        OfflineFx.Burst(center, Orange, 40, 3.5f, 0.07f, 0.7f, 0.3f, 0.2f);
        OfflineFx.Burst(center, Color.white, 16, 5f, 0.04f, 0.35f);
        OfflineFx.FloatingText(center + Vector3.up * 0.3f, "ACERTOU!", new Color(0.35f, 1f, 0.5f), 120f, 0.9f, 0.3f);
        OfflineFx.HapticAll(0.4f, 0.08f);
        if (_owner != null)
            _owner.TargetHit(this);
        Destroy(gameObject);
    }

    /// <summary>Practice over without this target being hit: just disappear.</summary>
    public void Dismiss()
    {
        if (_down)
            return;
        _down = true;
        OfflineFx.Burst(transform.position, new Color(0.45f, 0.9f, 1f), 14, 1.2f, 0.04f, 0.5f);
        Destroy(gameObject);
    }
}
