using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Entry point of the offline mode (scene "Offline").
/// Pick the mode in the Inspector and press Play; the in-headset panel can also change or start it
/// with the VR controllers (ray or poke).
/// </summary>
public sealed class OfflineModeMenu : MonoBehaviour
{
    [Header("Mode")]
    [Tooltip("Mode started by this scene.")]
    [SerializeField] private OfflineExperienceMode _mode = OfflineExperienceMode.CombatOnly;
    [Tooltip("Start the selected mode automatically after the delay. Off (default) = the player picks on the VR panel.")]
    [SerializeField] private bool _startAutomatically;
    [Min(0f)]
    [SerializeField] private float _autoStartDelay = 5f;
    [Tooltip("Also auto start when coming back to this scene after the credits. Off = wait for a VR button.")]
    [SerializeField] private bool _autoStartAfterReturn;

    [Header("VR panel (optional)")]
    [SerializeField] private Button _combatButton;
    [SerializeField] private Button _fullExperienceButton;
    [SerializeField] private Button _quitButton;
    [SerializeField] private TMP_Text _statusText;
    [Tooltip("Panel moved in front of the headset when the scene starts.")]
    [SerializeField] private Transform _panel;
    [SerializeField] private float _panelDistance = 1.3f;
    [SerializeField] private float _panelHeightOffset = -0.15f;

    private bool _started;

    public OfflineExperienceMode Mode => _mode;

    private void Awake()
    {
        OfflineSession.PrepareEntry();

        if (_combatButton != null)
            _combatButton.onClick.AddListener(StartCombat);
        if (_fullExperienceButton != null)
            _fullExperienceButton.onClick.AddListener(StartFullExperience);
        if (_quitButton != null)
            _quitButton.onClick.AddListener(Quit);
    }

    private IEnumerator Start()
    {
        yield return PlacePanelInFrontOfPlayer();

        bool returned = OfflineSession.ReturnedFromSession;
        if (!_startAutomatically || _mode == OfflineExperienceMode.None || (returned && !_autoStartAfterReturn))
        {
            SetStatus("Escolha a modalidade com o controle.");
            yield break;
        }

        float remaining = _autoStartDelay;
        while (remaining > 0f && !_started)
        {
            SetStatus($"Iniciando {ModeLabel(_mode)} em {Mathf.CeilToInt(remaining)}...\nOu escolha outra modalidade.");
            remaining -= Time.unscaledDeltaTime;
            yield return null;
        }

        Launch(_mode);
    }

    private IEnumerator PlacePanelInFrontOfPlayer()
    {
        if (_panel == null)
            yield break;

        float waited = 0f;
        OfflinePlayerRig rig = FindAnyObjectByType<OfflinePlayerRig>();
        while (rig != null && !rig.IsPlaced && waited < 3f)
        {
            waited += Time.unscaledDeltaTime;
            yield return null;
        }

        Camera head = Camera.main;
        if (head == null)
            yield break;

        Vector3 forward = Vector3.ProjectOnPlane(head.transform.forward, Vector3.up);
        if (forward.sqrMagnitude < 0.0001f)
            forward = Vector3.forward;
        forward.Normalize();

        _panel.position = head.transform.position + forward * _panelDistance + Vector3.up * _panelHeightOffset;
        _panel.rotation = Quaternion.LookRotation(forward, Vector3.up);
    }

    public void StartCombat()
    {
        Launch(OfflineExperienceMode.CombatOnly);
    }

    public void StartFullExperience()
    {
        Launch(OfflineExperienceMode.FullExperience);
    }

    public void Quit()
    {
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }

    private void Launch(OfflineExperienceMode mode)
    {
        if (_started || mode == OfflineExperienceMode.None)
            return;

        _started = true;
        SetStatus($"Carregando {ModeLabel(mode)}...");
        OfflineSession.Start(mode);
    }

    private void SetStatus(string text)
    {
        if (_statusText != null)
            _statusText.text = text;
    }

    private static string ModeLabel(OfflineExperienceMode mode) =>
        mode == OfflineExperienceMode.CombatOnly ? "COMBATE" : "EXPERIÊNCIA COMPLETA";
}
