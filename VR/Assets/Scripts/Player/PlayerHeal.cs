using Photon.Pun;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;
using UnityEngine.XR.Interaction.Toolkit.Inputs.Haptics;

/// <summary>
/// Heal ability of the VR player: press the controller button to recover life, then wait for the cooldown.
/// The HUD widget shows the button to press on top of a radial cooldown.
/// Works offline and online (the heal goes through PlayersLifeBar, synced like damage).
/// </summary>
public class PlayerHeal : MonoBehaviour
{
    [Header("Ability")]
    [Min(0f)]
    [SerializeField] private float _healAmount = 25f;
    [Tooltip("Seconds before the heal can be used again.")]
    [Min(0f)]
    [SerializeField] private float _cooldown = 20f;
    [Tooltip("Pressing with full life does nothing and does not start the cooldown.")]
    [SerializeField] private bool _blockWhenFull = true;

    [Header("Input")]
    [Tooltip("A (right controller) or X (left controller). H on the keyboard for Editor tests.")]
    [SerializeField] private InputAction _healAction = CreateDefaultAction();
    [Tooltip("Text shown over the icon: the button the player has to press.")]
    [SerializeField] private string _buttonLabel = "A / X";

    [Header("HUD")]
    [SerializeField] private PlayersLifeBar _lifeBar;
    [Tooltip("Radial Image (Filled) that empties while the ability recharges.")]
    [SerializeField] private Image _cooldownFill;
    [Tooltip("Icon faded while the ability recharges.")]
    [SerializeField] private CanvasGroup _icon;
    [SerializeField] private TMP_Text _buttonText;
    [SerializeField] private TMP_Text _cooldownText;
    [SerializeField] private Color _readyColor = new(0.2f, 1f, 0.45f, 1f);
    [SerializeField] private Color _coolingColor = new(0.45f, 0.45f, 0.45f, 0.9f);

    [Header("Feedback")]
    [SerializeField] private AudioSource _audioSource;
    [SerializeField] private AudioClip _healClip;
    [Range(0f, 1f)]
    [SerializeField] private float _hapticAmplitude = 0.6f;
    [SerializeField] private float _hapticDuration = 0.2f;

    private float _cooldownEndTime;
    private HapticImpulsePlayer[] _haptics;

    public float CooldownRemaining => Mathf.Max(0f, _cooldownEndTime - Time.time);
    public bool IsReady => CooldownRemaining <= 0f;

    private void Awake()
    {
        PhotonView view = GetComponentInParent<PhotonView>();
        if (!OfflineSession.IsOffline && view != null && !view.IsMine)
        {
            // Remote copies of a player never read input; their HUD is hidden anyway.
            enabled = false;
            return;
        }

        if (_lifeBar == null)
            _lifeBar = GetComponentInChildren<PlayersLifeBar>(true);
        _haptics = GetComponentsInChildren<HapticImpulsePlayer>(true);

        if (_buttonText != null)
            _buttonText.text = _buttonLabel;
    }

    private void OnEnable()
    {
        _healAction.performed += OnHealPerformed;
        _healAction.Enable();
    }

    private void OnDisable()
    {
        _healAction.performed -= OnHealPerformed;
        _healAction.Disable();
    }

    private void Update()
    {
        UpdateHud();
    }

    private void OnHealPerformed(InputAction.CallbackContext context)
    {
        TryHeal();
    }

    public bool TryHeal()
    {
        // Hidden HUD = not in combat: the ability is not available.
        if (!IsReady || _lifeBar == null || !_lifeBar.isActiveAndEnabled)
            return false;

        if (_blockWhenFull && _lifeBar.IsFull)
        {
            Pulse(_hapticAmplitude * 0.3f, 0.05f);
            return false;
        }

        _lifeBar.Heal(_healAmount);
        _cooldownEndTime = Time.time + _cooldown;

        if (_audioSource != null && _healClip != null)
            _audioSource.PlayOneShot(_healClip);
        Pulse(_hapticAmplitude, _hapticDuration);
        return true;
    }

    private void UpdateHud()
    {
        float remaining = CooldownRemaining;
        bool ready = remaining <= 0f;

        if (_cooldownFill != null)
        {
            _cooldownFill.fillAmount = ready || _cooldown <= 0f ? 1f : 1f - remaining / _cooldown;
            _cooldownFill.color = ready ? _readyColor : _coolingColor;
        }

        if (_icon != null)
            _icon.alpha = ready ? 1f : 0.2f;

        if (_cooldownText != null)
            _cooldownText.text = ready ? string.Empty : Mathf.CeilToInt(remaining).ToString();
    }

    private void Pulse(float amplitude, float duration)
    {
        if (_haptics == null)
            return;

        foreach (HapticImpulsePlayer haptic in _haptics)
        {
            if (haptic != null && haptic.isActiveAndEnabled)
                haptic.SendHapticImpulse(amplitude, duration);
        }
    }

    private static InputAction CreateDefaultAction()
    {
        var action = new InputAction("Heal", InputActionType.Button);
        action.AddBinding("<XRController>{RightHand}/primaryButton");
        action.AddBinding("<XRController>{LeftHand}/primaryButton");
        action.AddBinding("<Keyboard>/h");
        return action;
    }
}
