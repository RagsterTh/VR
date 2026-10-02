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

    [Tooltip("The panel follows the head smoothly when the player looks away (outside the dead zone).")]
    [SerializeField] private bool _followHead = true;
    [SerializeField] private float _followDeadZone = 25f;
    [SerializeField] private float _followSpeed = 3f;

    [Header("Event")]
    [Tooltip("Short shooting practice (3 targets) before the battle, once per visitor.")]
    [SerializeField] private bool _tutorial = true;
    [Tooltip("Seconds until the practice gives up and the battle starts anyway.")]
    [SerializeField] private float _tutorialTimeout = 25f;
    [Tooltip("Score, grade and Top 10 of the mode at the end of the session, before the credits.")]
    [SerializeField] private bool _resultsScreen = true;
    [Tooltip("Go back to this menu by itself when the headset is left unattended in the middle of a session.")]
    [SerializeField] private bool _kioskAutoReset = true;
    [Tooltip("Also in the Editor (off by default: a headset resting on the desk while developing would keep resetting the game).")]
    [SerializeField] private bool _kioskAutoResetInEditor;
    [Tooltip("Seconds with the headset off the face before going back to the menu.")]
    [SerializeField] private float _resetWhenUnworn = 15f;
    [Tooltip("Seconds with the headset completely still (left on a table) before going back to the menu. 0 = off.")]
    [SerializeField] private float _resetWhenStill = 60f;

    private bool _started;
    private bool _panelPlaced;
    private bool _following;

    public OfflineExperienceMode Mode => _mode;

    private void Awake()
    {
        OfflineSession.PrepareEntry();
        OfflineSession.ShowResults = _resultsScreen;
        OfflineTutorial.Enabled = _tutorial;
        OfflineTutorial.Timeout = _tutorialTimeout;
        OfflineKiosk.AutoReset = _kioskAutoReset && (!Application.isEditor || _kioskAutoResetInEditor);
        OfflineKiosk.ResetWhenUnworn = _resetWhenUnworn;
        OfflineKiosk.ResetWhenStill = _resetWhenStill;
        if (_panel != null && _panel.TryGetComponent(out RotateCanvas rotation))
            rotation.enabled = false;

        if (_combatButton != null)
            _combatButton.onClick.AddListener(StartCombat);
        if (_fullExperienceButton != null)
            _fullExperienceButton.onClick.AddListener(StartFullExperience);
        if (_quitButton != null)
            _quitButton.onClick.AddListener(Quit);
    }

    private IEnumerator Start()
    {
        // Menu backdrop: the parked ship floats instead of sitting still.
        GameObject ship = GameObject.Find("SpaceShuttle_01");
        if (ship != null && ship.GetComponent<OfflineShipIdle>() == null)
            ship.AddComponent<OfflineShipIdle>();

        yield return PlacePanelInFrontOfPlayer();

        // A session just ended: score and ranking first (on this same panel), then the credits.
        if (OfflineScore.HasResult && _panel != null)
        {
            _started = true;
            yield return OfflineResultsScreen.Show(_panel);
            OfflineSession.LoadCreditsScene();
            yield break;
        }

        bool returned = OfflineSession.ReturnedFromSession;
        if (!_startAutomatically || _mode == OfflineExperienceMode.None || (returned && !_autoStartAfterReturn))
        {
            SetStatus("SELECIONE UM MODO DE JOGO");
            yield break;
        }

        float remaining = _autoStartDelay;
        while (remaining > 0f && !_started)
        {
            SetStatus($"Iniciando {ModeLabel(_mode)} em {Mathf.CeilToInt(remaining)}...\nVocê ainda pode selecionar outro modo.");
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
        _panelPlaced = true;
    }

    private void LateUpdate()
    {
        if (!_followHead || !_panelPlaced || _panel == null)
            return;

        Camera head = Camera.main;
        if (head == null)
            return;

        Vector3 forward = Vector3.ProjectOnPlane(head.transform.forward, Vector3.up);
        if (forward.sqrMagnitude < 0.0001f)
            return;
        forward.Normalize();

        Vector3 toPanel = Vector3.ProjectOnPlane(_panel.position - head.transform.position, Vector3.up);
        float angle = Vector3.Angle(forward, toPanel);
        // Start following past the dead zone, keep going until the panel is centered again.
        if (angle > _followDeadZone)
            _following = true;
        else if (angle < 3f)
            _following = false;

        Vector3 target = head.transform.position + forward * _panelDistance + Vector3.up * _panelHeightOffset;
        if (!_following)
            target = new Vector3(_panel.position.x, target.y, _panel.position.z);

        float k = 1f - Mathf.Exp(-_followSpeed * Time.unscaledDeltaTime);
        _panel.position = Vector3.Lerp(_panel.position, target, k);
        Vector3 look = Vector3.ProjectOnPlane(_panel.position - head.transform.position, Vector3.up);
        if (look.sqrMagnitude > 0.0001f)
            _panel.rotation = Quaternion.Slerp(_panel.rotation, Quaternion.LookRotation(look, Vector3.up), k);
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
        {
            bool hasInstructions = _panel != null && _panel.Find("Instructions") != null;
            _statusText.text = !hasInstructions && text == "SELECIONE UM MODO DE JOGO"
                ? text + "\nAponte o raio do controle para uma opção e aperte o gatilho."
                : text;
        }
    }

    private static string ModeLabel(OfflineExperienceMode mode) =>
        mode == OfflineExperienceMode.CombatOnly ? "COMBATE" : "EXPERIÊNCIA COMPLETA";
}
