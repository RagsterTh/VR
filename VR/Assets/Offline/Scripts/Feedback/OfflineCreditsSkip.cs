using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

/// <summary>
/// Skip the credits (offline): hold A / X or a trigger for a moment. A floating prompt with a filling ring
/// stays in front of the player. Uses controller buttons because the credits rig (Meta) does not click
/// world-space canvases like the XRI rig does. Skipping does what the end of the credits does.
/// </summary>
public sealed class OfflineCreditsSkip : MonoBehaviour
{
    [SerializeField] private float _holdTime = 1f;
    [Tooltip("Seconds before the prompt appears (avoids skipping by accident while the scene loads).")]
    [SerializeField] private float _showAfter = 1.5f;
    [SerializeField] private float _distance = 1.3f;
    [SerializeField] private float _heightOffset = -0.8f;

    private InputAction _skip;
    private RectTransform _prompt;
    private Image _fill;
    private TextMeshProUGUI _label;
    private float _held;
    private float _elapsed;
    private bool _skipped;

    private void OnEnable()
    {
        _skip = new InputAction("Skip Credits", InputActionType.Button);
        _skip.AddBinding("<XRController>{RightHand}/primaryButton");
        _skip.AddBinding("<XRController>{LeftHand}/primaryButton");
        _skip.AddBinding("<XRController>{RightHand}/triggerPressed");
        _skip.AddBinding("<XRController>{LeftHand}/triggerPressed");
        _skip.AddBinding("<Keyboard>/space");
        _skip.Enable();
    }

    private void OnDisable()
    {
        _skip?.Disable();
        _skip?.Dispose();
        _skip = null;
    }

    private void Start()
    {
        // Big enough to read at arm's length, below the credits text.
        _prompt = OfflineFx.CreateCanvas("[Offline] Skip Credits", null, new Vector2(900, 200), 0.0016f, 180);
        Image back = OfflineFx.AddImage(_prompt, "Back", OfflineFx.White, new Color(0.08f, 0.1f, 0.14f, 0.85f), new Vector2(900, 170));
        back.material = OfflineFx.OverlayMaterial;
        Image ring = OfflineFx.AddImage(_prompt, "Ring", OfflineFx.Ring, new Color(1f, 1f, 1f, 0.25f), new Vector2(120, 120), new Vector2(-360f, 0f));
        ring.material = OfflineFx.OverlayMaterial;
        _fill = OfflineFx.AddImage(_prompt, "Fill", OfflineFx.Ring, new Color(0.3f, 1f, 0.5f, 1f), new Vector2(120, 120), new Vector2(-360f, 0f));
        _fill.material = OfflineFx.OverlayMaterial;
        _fill.type = Image.Type.Filled;
        _fill.fillMethod = Image.FillMethod.Radial360;
        _fill.fillOrigin = (int)Image.Origin360.Top;
        _fill.fillAmount = 0f;
        _label = OfflineFx.AddText(_prompt, "Label", "SEGURE  A / X  OU O GATILHO\nPARA PULAR OS CRÉDITOS", 44f, Color.white, new Vector2(700, 150), new Vector2(70f, 0f));
        _label.enableWordWrapping = false;
        _prompt.gameObject.SetActive(false);
    }

    private void Update()
    {
        if (_skipped || _prompt == null)
            return;

        _elapsed += Time.unscaledDeltaTime;
        if (_elapsed < _showAfter)
            return;

        if (!_prompt.gameObject.activeSelf)
        {
            _prompt.gameObject.SetActive(true);
            StartCoroutine(OfflineFx.Punch(_prompt, 0.2f, 0.25f));
        }

        FollowHead();

        bool holding = _skip != null && _skip.IsPressed();
        _held = holding ? _held + Time.unscaledDeltaTime : Mathf.MoveTowards(_held, 0f, Time.unscaledDeltaTime * 2f);
        _fill.fillAmount = Mathf.Clamp01(_held / _holdTime);

        if (holding && Time.frameCount % 6 == 0)
            OfflineFx.HapticAll(0.1f + 0.3f * _fill.fillAmount, 0.03f);

        if (_held >= _holdTime)
        {
            _skipped = true;
            _label.text = "PULANDO...";
            OfflineFx.HapticAll(0.6f, 0.12f);
            OfflineSession.ReturnToEntry();
        }
    }

    private void FollowHead()
    {
        Camera head = Camera.main;
        if (head == null)
            return;

        Vector3 forward = Vector3.ProjectOnPlane(head.transform.forward, Vector3.up);
        if (forward.sqrMagnitude < 0.0001f)
            forward = Vector3.forward;
        forward.Normalize();

        Vector3 target = head.transform.position + forward * _distance + Vector3.up * _heightOffset;
        // Lazy follow: stays readable without being glued to the face.
        _prompt.position = Vector3.Lerp(_prompt.position == Vector3.zero ? target : _prompt.position, target, Time.unscaledDeltaTime * 3f);
        _prompt.rotation = Quaternion.LookRotation(_prompt.position - head.transform.position, Vector3.up);
    }
}
