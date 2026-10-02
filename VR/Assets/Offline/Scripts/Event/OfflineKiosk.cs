using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.XR;
using InputDevice = UnityEngine.XR.InputDevice;
using CommonUsages = UnityEngine.XR.CommonUsages;

/// <summary>
/// Event/kiosk behaviour for the offline mode. One instance lives across scenes.
/// - Headset taken off (or nobody moving) for a while during a session: back to the menu, ready for the next visitor.
/// - Headset put on again: the rig is re-aligned for the new person.
/// - Operator combos (hold 3 s): B + Y = back to the menu; both thumbsticks pressed = skip the current stage.
///   In the Editor: F1 / F2.
/// </summary>
public sealed class OfflineKiosk : MonoBehaviour
{
    private static OfflineKiosk _instance;

    // Set by OfflineModeMenu from the Inspector of the entry scene.
    /// <summary>Automatic reset when the headset is left unattended. The operator combos work either way.</summary>
    public static bool AutoReset;
    /// <summary>Seconds with the headset off the face before a running session goes back to the menu.</summary>
    public static float ResetWhenUnworn = 15f;
    /// <summary>Seconds with the headset completely still (left on a table) before going back to the menu. 0 = off.</summary>
    public static float ResetWhenStill = 60f;

    private const float ComboHold = 3f;

    private System.DateTime _pausedAt;
    private bool _paused;
    private InputAction _resetCombo;
    private InputAction _resetCombo2;
    private InputAction _skipCombo;
    private InputAction _skipCombo2;
    private float _unworn;
    private float _still;
    private float _resetHeld;
    private float _skipHeld;
    private bool _wasWorn = true;
    private Quaternion _lastHeadRotation;
    private bool _busy;

    public static void Ensure()
    {
        if (_instance != null)
            return;
        var go = new GameObject("[Offline] Kiosk");
        DontDestroyOnLoad(go);
        _instance = go.AddComponent<OfflineKiosk>();
    }

    private void OnEnable()
    {
        _resetCombo = Button("<XRController>{RightHand}/secondaryButton");
        _resetCombo2 = Button("<XRController>{LeftHand}/secondaryButton");
        _skipCombo = Button("<XRController>{RightHand}/primary2DAxisClick");
        _skipCombo2 = Button("<XRController>{LeftHand}/primary2DAxisClick");
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    private void OnDisable()
    {
        foreach (InputAction action in new[] { _resetCombo, _resetCombo2, _skipCombo, _skipCombo2 })
        {
            action?.Disable();
            action?.Dispose();
        }
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    private static InputAction Button(string binding)
    {
        var action = new InputAction(type: InputActionType.Button, binding: binding);
        action.Enable();
        return action;
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        _unworn = _still = _resetHeld = _skipHeld = 0f;
        _busy = false;
    }

    private void Update()
    {
        if (_busy || !OfflineSession.IsOffline)
            return;

        float dt = Time.unscaledDeltaTime;
        bool inSession = SceneManager.GetActiveScene().name != OfflineSession.EntryScene;

        UpdatePresence(dt, inSession);
        UpdateOperator(dt, inSession);
    }

    // ---------- Visitor presence ----------

    private void UpdatePresence(float dt, bool inSession)
    {
        if (!AutoReset)
            return;

        InputDevice head = InputDevices.GetDeviceAtXRNode(XRNode.Head);
        if (!head.isValid)
            return; // no headset (Editor without Link): nothing to watch

        bool worn = !head.TryGetFeatureValue(CommonUsages.userPresence, out bool present) || present;
        if (worn && !_wasWorn)
            Recenter();
        _wasWorn = worn;
        _unworn = worn ? 0f : _unworn + dt;

        // A head on a person never stays within half a degree for long; a headset on a table does.
        // The reference only moves when the head leaves it, so slow drift does not count as "still".
        if (head.TryGetFeatureValue(CommonUsages.deviceRotation, out Quaternion rotation))
        {
            if (Quaternion.Angle(rotation, _lastHeadRotation) > 0.5f)
            {
                _lastHeadRotation = rotation;
                _still = 0f;
            }
            else
            {
                _still += dt;
            }
        }

        if (_unworn >= ResetWhenUnworn || (ResetWhenStill > 0f && _still >= ResetWhenStill))
            Abandoned(inSession);
    }

    /// <summary>
    /// On the Quest the app is paused when the headset leaves the face (Update stops), so the time away is
    /// measured across the pause.
    /// </summary>
    private void OnApplicationPause(bool paused)
    {
        if (paused)
        {
            _pausedAt = System.DateTime.UtcNow;
            _paused = true;
            return;
        }

        if (!_paused)
            return;
        _paused = false;
        if (!AutoReset || !OfflineSession.IsOffline || _busy)
            return;

        double away = (System.DateTime.UtcNow - _pausedAt).TotalSeconds;
        _unworn = _still = 0f;
        if (away >= ResetWhenUnworn)
            Abandoned(SceneManager.GetActiveScene().name != OfflineSession.EntryScene);
        else
            Recenter();
    }

    private void Abandoned(bool inSession)
    {
        _unworn = _still = 0f;
        // On the menu there is nothing to reset, unless someone walked away from the result screen.
        if (!inSession && !OfflineScore.HasResult)
            return;
        Debug.Log("[Offline] Quiosque: sessão abandonada, voltando ao menu.");
        BackToMenu();
    }

    /// <summary>Headset back on a face: line the rig up with that person's position and heading.</summary>
    private static void Recenter()
    {
        OfflinePlayerRig rig = FindAnyObjectByType<OfflinePlayerRig>();
        if (rig != null && rig.IsPlaced)
            rig.Place();
    }

    // ---------- Operator ----------

    private void UpdateOperator(float dt, bool inSession)
    {
        Keyboard keyboard = Keyboard.current;
        bool reset = (_resetCombo.IsPressed() && _resetCombo2.IsPressed()) || (keyboard != null && keyboard.f1Key.isPressed);
        bool skip = (_skipCombo.IsPressed() && _skipCombo2.IsPressed()) || (keyboard != null && keyboard.f2Key.isPressed);

        _resetHeld = reset ? _resetHeld + dt : 0f;
        _skipHeld = skip ? _skipHeld + dt : 0f;

        if (reset || skip)
        {
            // Growing buzz so the operator feels the hold counting.
            float progress = Mathf.Max(_resetHeld, _skipHeld) / ComboHold;
            if (Time.frameCount % 10 == 0)
                OfflineFx.HapticAll(0.15f + 0.4f * progress, 0.04f);
        }

        if (_resetHeld >= ComboHold)
        {
            Debug.Log("[Offline] Operador: voltar ao menu.");
            Announce("OPERADOR: VOLTANDO AO MENU");
            BackToMenu();
        }
        else if (_skipHeld >= ComboHold && inSession)
        {
            Debug.Log("[Offline] Operador: pular etapa.");
            Announce("OPERADOR: PULANDO ETAPA");
            SkipStage();
        }
    }

    private void BackToMenu()
    {
        _busy = true;
        OfflineScore.Clear();
        Time.timeScale = 1f;
        OfflineSession.ReturnToEntry();
    }

    private void SkipStage()
    {
        _busy = true;
        Time.timeScale = 1f;
        string scene = SceneManager.GetActiveScene().name;
        if (scene == OfflineSession.FullExperienceScene)
            OfflineSession.LoadScene(OfflineSession.CambirelaScene);
        else if (OfflineSession.IsCombatScene)
            OfflineSession.LoadAfterBattle();
        else if (scene == OfflineSession.MedicalScene)
            OfflineSession.LoadCredits();
        else
            OfflineSession.ReturnToEntry();
    }

    private static void Announce(string text)
    {
        Camera head = Camera.main;
        if (head != null)
            OfflineFx.FloatingText(head.transform.position + head.transform.forward * 1.2f, text, new Color(1f, 0.85f, 0.2f), 70f, 1.5f, 0.05f);
        OfflineFx.HapticAll(0.7f, 0.2f);
    }
}
