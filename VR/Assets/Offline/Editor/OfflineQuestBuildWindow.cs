using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEngine;

/// <summary>
/// Builds the offline mode straight to the Quest with its own scene list (Offline first),
/// without touching the Build Settings used by the online build (LoadingScene must stay index 0).
/// </summary>
public sealed class OfflineQuestBuildWindow : EditorWindow
{
    private static string[] OfflineScenes => OfflineModeSetup.AllOfflineScenePaths;

    private Vector2 _scroll;

    [MenuItem("Tools/Offline/Build APK offline (Quest)...", priority = 60)]
    public static void Open()
    {
        GetWindow<OfflineQuestBuildWindow>("Offline Quest Build").minSize = new Vector2(420f, 360f);
    }

    private void OnGUI()
    {
        OfflineQuestBuildSettings settings = OfflineQuestBuildSettings.instance;
        _scroll = EditorGUILayout.BeginScrollView(_scroll);

        EditorGUILayout.HelpBox(
            "Gera o APK só com as cenas offline (a cena Offline abre primeiro). O Build Settings do online não é alterado.",
            MessageType.Info);

        EditorGUI.BeginChangeCheck();

        EditorGUILayout.LabelField("Saída", EditorStyles.boldLabel);
        using (new EditorGUILayout.HorizontalScope())
        {
            settings.OutputPath = EditorGUILayout.TextField("APK", settings.OutputPath);
            if (GUILayout.Button("...", GUILayout.Width(28f)))
            {
                string picked = EditorUtility.SaveFilePanel("APK offline", Path.GetDirectoryName(FullOutputPath(settings)), Path.GetFileName(settings.OutputPath), "apk");
                if (!string.IsNullOrEmpty(picked))
                    settings.OutputPath = ToProjectRelative(picked);
            }
        }

        EditorGUILayout.Space();
        EditorGUILayout.LabelField("Opções", EditorStyles.boldLabel);
        settings.DevelopmentBuild = EditorGUILayout.Toggle(new GUIContent("Development Build", "Necessário para Patch And Run."), settings.DevelopmentBuild);
        using (new EditorGUI.DisabledScope(!settings.DevelopmentBuild))
        {
            settings.ScriptDebugging = EditorGUILayout.Toggle("Script Debugging", settings.ScriptDebugging);
            settings.AutoconnectProfiler = EditorGUILayout.Toggle("Autoconnect Profiler", settings.AutoconnectProfiler);
        }
        settings.CleanBuild = EditorGUILayout.Toggle(new GUIContent("Clean Build", "Ignora o cache de build (mais lento)."), settings.CleanBuild);

        if (EditorGUI.EndChangeCheck())
            settings.SaveSettings();

        EditorGUILayout.Space();
        EditorGUILayout.LabelField("Cenas incluídas", EditorStyles.boldLabel);
        foreach (string scene in OfflineScenes)
        {
            bool exists = File.Exists(scene);
            EditorGUILayout.LabelField((exists ? "✔ " : "✖ ") + scene);
        }

        bool scenesReady = OfflineScenes.All(File.Exists);
        if (!scenesReady)
        {
            EditorGUILayout.HelpBox("Faltam cenas offline. Rode Tools > Offline > Configurar modo offline e cura.", MessageType.Error);
            if (GUILayout.Button("Configurar modo offline agora"))
                EditorApplication.delayCall += OfflineModeSetup.SetupAll;
        }

        if (EditorUserBuildSettings.activeBuildTarget != BuildTarget.Android)
            EditorGUILayout.HelpBox("A plataforma ativa não é Android. O Unity vai trocar para Android antes do build (pode demorar na primeira vez).", MessageType.Warning);

        EditorGUILayout.Space();
        using (new EditorGUI.DisabledScope(!scenesReady))
        {
            using (new EditorGUILayout.HorizontalScope())
            {
                if (GUILayout.Button("Build", GUILayout.Height(32f)))
                    Schedule(run: false, patch: false);
                if (GUILayout.Button("Build And Run", GUILayout.Height(32f)))
                    Schedule(run: true, patch: false);
            }

            using (new EditorGUI.DisabledScope(!settings.DevelopmentBuild || !File.Exists(FullOutputPath(settings))))
            {
                if (GUILayout.Button(new GUIContent("Patch And Run",
                        "Envia só o que mudou (scripts/assets) para o APK já instalado. Exige Development Build e um Build And Run completo antes com o mesmo caminho."),
                        GUILayout.Height(32f)))
                    Schedule(run: true, patch: true);
            }
        }

        EditorGUILayout.EndScrollView();
    }

    // Building from inside OnGUI breaks the IMGUI layout; run it after this GUI pass.
    private static void Schedule(bool run, bool patch)
    {
        EditorApplication.delayCall += () => Build(OfflineQuestBuildSettings.instance, run, patch);
    }

    private static void Build(OfflineQuestBuildSettings settings, bool run, bool patch)
    {
        if (EditorUserBuildSettings.activeBuildTarget != BuildTarget.Android &&
            !EditorUserBuildSettings.SwitchActiveBuildTarget(BuildTargetGroup.Android, BuildTarget.Android))
        {
            Debug.LogError("[Offline] Não foi possível trocar a plataforma para Android. O módulo Android está instalado?");
            return;
        }

        string output = FullOutputPath(settings);
        Directory.CreateDirectory(Path.GetDirectoryName(output));

        BuildOptions options = BuildOptions.None;
        if (settings.DevelopmentBuild || patch)
        {
            options |= BuildOptions.Development;
            if (settings.ScriptDebugging)
                options |= BuildOptions.AllowDebugging;
            if (settings.AutoconnectProfiler)
                options |= BuildOptions.ConnectWithProfiler;
        }
        if (run)
            options |= BuildOptions.AutoRunPlayer;
        if (patch)
            options |= BuildOptions.PatchPackage;
        if (settings.CleanBuild && !patch)
            options |= BuildOptions.CleanBuildCache;

        // An .aab cannot be installed/patched directly; build a plain APK and restore the user's choice afterwards.
        bool wasAppBundle = EditorUserBuildSettings.buildAppBundle;
        bool wasExport = EditorUserBuildSettings.exportAsGoogleAndroidProject;
        EditorUserBuildSettings.buildAppBundle = false;
        EditorUserBuildSettings.exportAsGoogleAndroidProject = false;

        try
        {
            BuildReport report = BuildPipeline.BuildPlayer(new BuildPlayerOptions
            {
                scenes = OfflineScenes,
                locationPathName = output,
                target = BuildTarget.Android,
                targetGroup = BuildTargetGroup.Android,
                options = options,
            });

            BuildSummary summary = report.summary;
            if (summary.result == BuildResult.Succeeded)
                Debug.Log($"[Offline] Build {(patch ? "patch " : string.Empty)}ok: {output} ({summary.totalSize / (1024f * 1024f):0.0} MB, {summary.totalTime.TotalSeconds:0}s).");
            else if (summary.result == BuildResult.Cancelled)
                Debug.LogWarning("[Offline] Build cancelado (botão Cancel da barra de progresso ou o Quest desconectou/dormiu no fim). " +
                                 "Obs.: \"Method 'Init' is in a generic class\" vem do Meta XR SDK (Immersive Debugger) e aparece em todo build; pode ignorar.");
            else
                Debug.LogError($"[Offline] Build falhou ({summary.result}) com {summary.totalErrors} erro(s). Veja o Console. " +
                               "Ignore \"Method 'Init' is in a generic class\" (aviso do Meta XR SDK); procure o outro erro.");
        }
        finally
        {
            EditorUserBuildSettings.buildAppBundle = wasAppBundle;
            EditorUserBuildSettings.exportAsGoogleAndroidProject = wasExport;
        }
    }

    private static string FullOutputPath(OfflineQuestBuildSettings settings)
    {
        string path = string.IsNullOrWhiteSpace(settings.OutputPath) ? OfflineQuestBuildSettings.DefaultOutput : settings.OutputPath;
        return Path.GetFullPath(Path.IsPathRooted(path) ? path : Path.Combine(ProjectRoot, path));
    }

    private static string ToProjectRelative(string absolute)
    {
        string full = Path.GetFullPath(absolute);
        string root = Path.GetFullPath(ProjectRoot) + Path.DirectorySeparatorChar;
        return full.StartsWith(root) ? full.Substring(root.Length).Replace('\\', '/') : full;
    }

    private static string ProjectRoot => Path.GetDirectoryName(Application.dataPath);
}

/// <summary>Per-user build options (UserSettings/ is not versioned).</summary>
[FilePath("UserSettings/OfflineQuestBuild.asset", FilePathAttribute.Location.ProjectFolder)]
public sealed class OfflineQuestBuildSettings : ScriptableSingleton<OfflineQuestBuildSettings>
{
    public const string DefaultOutput = "Builds/QuestOffline/VR_Offline.apk";

    public string OutputPath = DefaultOutput;
    public bool DevelopmentBuild = true;
    public bool ScriptDebugging;
    public bool AutoconnectProfiler;
    public bool CleanBuild;

    public void SaveSettings() => Save(true);
}
