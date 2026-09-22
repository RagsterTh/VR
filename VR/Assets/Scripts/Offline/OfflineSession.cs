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
/// No network object is required because the state belongs only to this process.
/// </summary>
public static class OfflineSession
{
    public const string EntryScene = "Offline";
    public const string CombatScene = "Game";
    public const string FullExperienceScene = "GloboV2";
    public const string MedicalScene = "MedicalQuestions";
    public const string CreditsScene = "Credits";

    public static OfflineExperienceMode Mode { get; private set; }

    public static bool IsOffline =>
        Mode != OfflineExperienceMode.None ||
        SceneManager.GetActiveScene().name == EntryScene;

    public static bool IsFullExperience => Mode == OfflineExperienceMode.FullExperience;

    public static void PrepareMenu()
    {
        Mode = OfflineExperienceMode.None;
        ConnectionManager.isVR = true;

        if (ConnectionManager.instance != null)
            Object.Destroy(ConnectionManager.instance.gameObject);

        if (PhotonNetwork.IsConnected)
            PhotonNetwork.Disconnect();
    }

    public static void StartCombatOnly()
    {
        Begin(OfflineExperienceMode.CombatOnly, SimulationMode.Shoot, CombatScene);
    }

    public static void StartFullExperience()
    {
        Begin(OfflineExperienceMode.FullExperience, SimulationMode.Default, FullExperienceScene);
    }

    public static void LoadAfterBattle()
    {
        SceneManager.LoadScene(IsFullExperience ? MedicalScene : CreditsScene);
    }

    public static void LoadCredits()
    {
        SceneManager.LoadScene(CreditsScene);
    }

    public static void ReturnToEntry()
    {
        SceneManager.LoadScene(EntryScene);
    }

    private static void Begin(OfflineExperienceMode mode, SimulationMode simulationMode, string sceneName)
    {
        Mode = mode;
        ConnectionManager.isVR = true;
        UserCam.simulationMode = simulationMode;
        SceneManager.LoadScene(sceneName);
    }
}
