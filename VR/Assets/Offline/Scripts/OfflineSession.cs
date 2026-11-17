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

public static class OfflineSession
{
    public const string OfflineScenesFolder = "Assets/Offline/";

    public const string EntryScene = "Offline";
    public const string CombatScene = "Offline_Combat";
    public const string FullExperienceScene = "Offline_GloboV2";
    public const string MedicalScene = "Offline_Medical";
    public const string CreditsScene = "Offline_Credits";
    public const string DefeatScene = "Offline_Defeat";

    public const string CambirelaScene = "Offline_Cambirela";
    public const string GuardaScene = "Offline_Guarda";
    public const string PedraBrancaScene = "Offline_PedraBranca";
    public static readonly string[] MapScenes = { CambirelaScene, GuardaScene, PedraBrancaScene };

    private static readonly Dictionary<string, string> OfflineSceneByOriginal = new()
    {
        { "LoadingScene", EntryScene },
        { "Game", CombatScene },
        { "GloboV2", FullExperienceScene },
        { "MedicalQuestions", MedicalScene },
        { "Credits", CreditsScene },
    };

    private static OfflineExperienceMode _mode;
    private static string _retryScene = CombatScene;

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

    public static bool ShowResults = true;

    public static bool ReturnedFromSession { get; private set; }

    public static bool IsFullExperience => Mode == OfflineExperienceMode.FullExperience;

    public static bool IsCombatScene => SceneManager.GetActiveScene().name == CombatScene || IsMapScene;

    public static bool IsMapScene => System.Array.IndexOf(MapScenes, SceneManager.GetActiveScene().name) >= 0;

    public static bool IsOfflineScene(Scene scene) =>
        scene.IsValid() && scene.path.StartsWith(OfflineScenesFolder);

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
        PlayersLifeBar life = Object.FindAnyObjectByType<PlayersLifeBar>();
        if (life != null && life.MaxLife > 0f)
            OfflineScore.SetLifeRatio(life.CurrentLife / life.MaxLife);

        if (IsFullExperience)
            LoadScene(MedicalScene);
        else
            LoadCredits();
    }

    public static void LoadCredits()
    {
        if (ShowResults && OfflineScore.Finish())
        {
            ReturnedFromSession = true;
            LoadScene(EntryScene);
            return;
        }

        LoadScene(CreditsScene);
    }

    public static void LoadCreditsScene()
    {
        OfflineScore.Clear();
        LoadScene(CreditsScene);
    }

    public static void LoadDefeat()
    {
        string current = SceneManager.GetActiveScene().name;
        if (current == CombatScene || current == FullExperienceScene || System.Array.IndexOf(MapScenes, current) >= 0)
            _retryScene = current;
        LoadScene(DefeatScene);
    }

    public static void RetryBattle()
    {
        if (OfflineScore.Mode != OfflineExperienceMode.None)
            OfflineScore.Begin(OfflineScore.Mode);
        LoadScene(string.IsNullOrEmpty(_retryScene) ? CombatScene : _retryScene);
    }

    public static void ReturnToEntry()
    {
        ReturnedFromSession = true;
        LoadScene(EntryScene);
    }

    public static void OpenCombat()
    {
        WaitingPlayers lobby = Object.FindAnyObjectByType<WaitingPlayers>();
        if (lobby != null)
            lobby.Finish();
        else
            LoadMappedScene("Game");
    }

    public static void LoadMappedScene(string sceneName)
    {
        LoadScene(OfflineSceneByOriginal.TryGetValue(sceneName, out string offlineScene) ? offlineScene : sceneName);
    }

    public static void LoadScene(string sceneName)
    {
        if (sceneName == CombatScene || sceneName == FullExperienceScene ||
            System.Array.IndexOf(MapScenes, sceneName) >= 0)
            _retryScene = sceneName;
        ConnectionManager.isVR = true;
        if (!Application.CanStreamedLevelBeLoaded(sceneName))
        {
            Debug.LogError($"[Offline] A cena '{sceneName}' não está na build (File > Build Profiles > Scene List).");
            return;
        }
        OfflineScreenFade.FadeOutAndLoad(sceneName);
    }

    public static List<string> MissingScenes()
    {
        var missing = new List<string>();
        foreach (string scene in new[] { CombatScene, FullExperienceScene, MedicalScene, CreditsScene, DefeatScene })
            if (!Application.CanStreamedLevelBeLoaded(scene)) missing.Add(scene);
        foreach (string scene in MapScenes)
            if (!Application.CanStreamedLevelBeLoaded(scene)) missing.Add(scene);
        return missing;
    }

    private static void Begin(OfflineExperienceMode mode, SimulationMode simulationMode, string sceneName)
    {
        _mode = mode;
        OfflineScore.Begin(mode);
        OfflineTutorial.ResetForNewVisitor();
        UserCam.simulationMode = simulationMode;
        LoadScene(sceneName);
    }

    private static OfflineExperienceMode InferModeFromScene(string sceneName)
    {
        if (sceneName == CombatScene)
            return OfflineExperienceMode.CombatOnly;
        if (sceneName == FullExperienceScene || sceneName == MedicalScene || System.Array.IndexOf(MapScenes, sceneName) >= 0)
            return OfflineExperienceMode.FullExperience;
        return OfflineExperienceMode.None;
    }
}
