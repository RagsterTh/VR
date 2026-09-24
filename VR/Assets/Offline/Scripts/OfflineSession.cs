using System.Collections.Generic;
using Photon.Pun;
using UnityEngine;
using UnityEngine.SceneManagement;

public enum OfflineExperienceMode
{
    None,
    CombatOnly,
    FullExperience
}

/// <summary>
/// Keeps the selected offline flow alive while Unity changes scenes.
/// A session is offline when a mode was chosen in the Offline scene or when the
/// active scene lives in Assets/Offline (so any offline scene can be played directly).
/// </summary>
public static class OfflineSession
{
    public const string OfflineScenesFolder = "Assets/Offline/";

    public const string EntryScene = "Offline";
    public const string CombatScene = "Offline_Combat";
    public const string FullExperienceScene = "Offline_GloboV2";
    public const string MedicalScene = "Offline_Medical";
    public const string CreditsScene = "Offline_Credits";

    // Original scene name -> offline copy. Used when shared gameplay code asks for an original scene.
    private static readonly Dictionary<string, string> OfflineSceneByOriginal = new()
    {
        { "LoadingScene", EntryScene },
        { "Game", CombatScene },
        { "GloboV2", FullExperienceScene },
        { "MedicalQuestions", MedicalScene },
        { "Credits", CreditsScene },
    };

    private static OfflineExperienceMode _mode;

    public static OfflineExperienceMode Mode
    {
        get
        {
            if (_mode == OfflineExperienceMode.None && IsOfflineScene(SceneManager.GetActiveScene()))
                _mode = InferModeFromScene(SceneManager.GetActiveScene().name);
            return _mode;
        }
    }

    public static bool IsOffline =>
        _mode != OfflineExperienceMode.None || IsOfflineScene(SceneManager.GetActiveScene());

    /// <summary>True when the entry scene was reached from a finished/abandoned session (not a fresh launch).</summary>
    public static bool ReturnedFromSession { get; private set; }

    public static bool IsFullExperience => Mode == OfflineExperienceMode.FullExperience;

    public static bool IsCombatScene => SceneManager.GetActiveScene().name == CombatScene;

    public static bool IsOfflineScene(Scene scene) =>
        scene.IsValid() && scene.path.StartsWith(OfflineScenesFolder);

    /// <summary>Called by the entry scene: leaves any online session and clears the previous choice.</summary>
    public static void PrepareEntry()
    {
        _mode = OfflineExperienceMode.None;
        ConnectionManager.isVR = true;

        if (ConnectionManager.instance != null)
            Object.Destroy(ConnectionManager.instance.gameObject);

        if (PhotonNetwork.IsConnected)
            PhotonNetwork.Disconnect();
    }

    public static void Start(OfflineExperienceMode mode)
    {
        switch (mode)
        {
            case OfflineExperienceMode.CombatOnly:
                Begin(mode, SimulationMode.Shoot, CombatScene);
                break;
            case OfflineExperienceMode.FullExperience:
                Begin(mode, SimulationMode.Default, FullExperienceScene);
                break;
            default:
                Debug.LogWarning("[OfflineSession] Choose Combat or Full Experience to start.");
                break;
        }
    }

    public static void LoadAfterBattle()
    {
        LoadScene(IsFullExperience ? MedicalScene : CreditsScene);
    }

    public static void LoadCredits()
    {
        LoadScene(CreditsScene);
    }

    public static void ReturnToEntry()
    {
        ReturnedFromSession = true;
        LoadScene(EntryScene);
    }

    /// <summary>Loads the offline copy of an original scene name (or the name itself if it is already offline).</summary>
    public static void LoadMappedScene(string sceneName)
    {
        LoadScene(OfflineSceneByOriginal.TryGetValue(sceneName, out string offlineScene) ? offlineScene : sceneName);
    }

    public static void LoadScene(string sceneName)
    {
        ConnectionManager.isVR = true;
        OfflineScreenFade.FadeOutAndLoad(sceneName);
    }

    private static void Begin(OfflineExperienceMode mode, SimulationMode simulationMode, string sceneName)
    {
        _mode = mode;
        UserCam.simulationMode = simulationMode;
        LoadScene(sceneName);
    }

    private static OfflineExperienceMode InferModeFromScene(string sceneName)
    {
        if (sceneName == CombatScene)
            return OfflineExperienceMode.CombatOnly;
        if (sceneName == FullExperienceScene || sceneName == MedicalScene)
            return OfflineExperienceMode.FullExperience;
        return OfflineExperienceMode.None;
    }
}
