using System.Collections.Generic;
using System.IO;
using System.Linq;
using TMPro;
using Unity.XR.CoreUtils;
using UnityEditor;
using UnityEditor.Events;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using UnityEngine.Playables;
using UnityEngine.Timeline;
using UnityEngine.TextCore.LowLevel;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using UnityEngine.XR.Interaction.Toolkit.UI;

/// <summary>
/// One-click setup of the offline mode. Safe to run again: copies original scenes only when the
/// offline copy does not exist yet, and rebuilds only the objects it owns ("[Offline] ..." / "HealAbility").
/// </summary>
public static class OfflineModeSetup
{
    private const string ScenesFolder = "Assets/Offline/Scenes";
    private const string EntryScenePath = ScenesFolder + "/Offline.unity";
    private const string PlayerPrefabPath = "Assets/Prefabs/Resources/PlayerVR V3.prefab";
    private const string FontGuid = "3b20ba306b276e448b2d4232006b7789";

    private const string BootstrapName = "[Offline] Bootstrap";
    private const string MenuPanelName = "[Offline] Menu Panel";
    private const string HealWidgetName = "HealAbility";
    private const string AutoAdvancePrefix = "[Offline] Auto Advance";
    private const string SpawnPointName = "[Offline] Spawn Point";
    private const string ReadableFontPath = "Assets/Offline/Fonts/Zekton-Regular Offline Static SDF.asset";
    // First version (dynamic, cleared on build -> invisible text on the Quest). Replaced and deleted by the setup.
    private const string OldDynamicFontPath = "Assets/Offline/Fonts/Zekton-Regular Offline SDF.asset";
    private const string ReadableFontSource = "Assets/Fnt/Zekton-Regular.otf";
    // Fonts of the medical panel: secrcode is a symbol font and Zekton SDF was baked with ASCII only (no accents).
    private static readonly string[] UnreadableFonts = { "Assets/Fnt/secrcode SDF.asset", "Assets/Fnt/Zekton-Regular SDF.asset" };

    // Original scene -> offline copy.
    private static readonly (string original, string copy)[] SceneCopies =
    {
        ("Assets/Scenes/Game.unity", ScenesFolder + "/" + OfflineSession.CombatScene + ".unity"),
        ("Assets/Scenes/GloboV2.unity", ScenesFolder + "/" + OfflineSession.FullExperienceScene + ".unity"),
        ("Assets/Scenes/MedicalQuestions.unity", ScenesFolder + "/" + OfflineSession.MedicalScene + ".unity"),
        ("Assets/Scenes/Credits.unity", ScenesFolder + "/" + OfflineSession.CreditsScene + ".unity"),
    };

    // Globe maps (scenery only) -> offline copies that receive the combat kit.
    private static readonly (string original, string copy)[] MapCopies =
    {
        ("Assets/Scenes/ScenasGlobo/Cambirela.unity", ScenesFolder + "/" + OfflineSession.CambirelaScene + ".unity"),
        ("Assets/Scenes/ScenasGlobo/Guarda.unity", ScenesFolder + "/" + OfflineSession.GuardaScene + ".unity"),
        ("Assets/Scenes/ScenasGlobo/PedraBranca.unity", ScenesFolder + "/" + OfflineSession.PedraBrancaScene + ".unity"),
    };

    private const string ShootGamePrefabPath = "Assets/Prefabs/Map/ShootGame.prefab";
    private const string CombatKitName = "[Offline] Combat (ShootGame)";
    private const string MapSelectionName = "[Offline] Map Selection";

    // Map buttons over the map picture in GloboV2 (named by their position on the picture).
    private static readonly Dictionary<string, string> MapByButtonName = new()
    {
        { "Button", OfflineSession.PedraBrancaScene },
        { "Button (1)", OfflineSession.CambirelaScene },
        { "Button (2)", OfflineSession.GuardaScene },
    };

    /// <summary>Every scene of the offline build, entry scene first.</summary>
    public static string[] AllOfflineScenePaths =>
        new[] { EntryScenePath }.Concat(SceneCopies.Select(s => s.copy)).Concat(MapCopies.Select(m => m.copy)).ToArray();

    [MenuItem("Tools/Offline/Configurar modo offline e cura", priority = 0)]
    public static void SetupAll()
    {
        if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
            return;

        SetupHealOnPlayerPrefab();
        CopyScenes(overwrite: false);

        ConfigureGameplayScene(SceneCopies[0].copy, sceneRigIsPlayer: false);
        ConfigureGameplayScene(SceneCopies[1].copy, sceneRigIsPlayer: false);
        ConfigureGameplayScene(SceneCopies[2].copy, sceneRigIsPlayer: true);
        ConfigureGameplayScene(SceneCopies[3].copy, sceneRigIsPlayer: true);
        ConfigureMapSelection(SceneCopies[1].copy);
        ConfigureSpawnPoint(SceneCopies[1].copy, "Lobby/Globe");
        ConfigureSpawnPoint(SceneCopies[2].copy, "MedicalRoom");
        ConfigureMedicalFonts(SceneCopies[2].copy);
        foreach (var (_, copy) in MapCopies)
            ConfigureMapScene(copy);
        ConfigureEntryScene();
        UpdateBuildSettings();

        EditorSceneManager.OpenScene(EntryScenePath);
        Debug.Log("[Offline] Setup concluído. Abra a cena Offline, escolha o modo no componente OfflineModeMenu e aperte Play.");
    }

    [MenuItem("Tools/Offline/Recriar cópias offline das cenas (sobrescreve)", priority = 20)]
    public static void RecreateSceneCopies()
    {
        if (!EditorUtility.DisplayDialog("Recriar cenas offline",
                "As cópias em Assets/Offline/Scenes (exceto a cena Offline) serão substituídas pelas cenas originais atuais. Continuar?",
                "Recriar", "Cancelar"))
            return;

        CopyScenes(overwrite: true);
        SetupAll();
    }

    [MenuItem("Tools/Offline/Configurar só a cura no PlayerVR V3", priority = 40)]
    public static void SetupHealOnPlayerPrefab()
    {
        GameObject root = PrefabUtility.LoadPrefabContents(PlayerPrefabPath);
        try
        {
            PlayersLifeBar lifeBar = root.GetComponentInChildren<PlayersLifeBar>(true);
            if (lifeBar == null)
            {
                Debug.LogError("[Offline] PlayersLifeBar não encontrado no PlayerVR V3.");
                return;
            }

            var canvas = (RectTransform)lifeBar.transform.parent;
            Transform old = canvas.Find(HealWidgetName);
            if (old != null)
                Object.DestroyImmediate(old.gameObject);

            HealWidget widget = BuildHealWidget(canvas, (RectTransform)lifeBar.transform);

            PlayerHeal heal = root.GetComponent<PlayerHeal>();
            if (heal == null)
                heal = root.AddComponent<PlayerHeal>();

            var serialized = new SerializedObject(heal);
            serialized.FindProperty("_lifeBar").objectReferenceValue = lifeBar;
            serialized.FindProperty("_cooldownFill").objectReferenceValue = widget.Fill;
            serialized.FindProperty("_icon").objectReferenceValue = widget.Icon;
            serialized.FindProperty("_buttonText").objectReferenceValue = widget.ButtonText;
            serialized.FindProperty("_cooldownText").objectReferenceValue = widget.CooldownText;
            serialized.ApplyModifiedPropertiesWithoutUndo();

            // Life bar + heal only while holding the guns (combat); PlayerPrefabNetwork toggles this canvas.
            var network = root.GetComponent<PlayerPrefabNetwork>();
            var serializedNetwork = new SerializedObject(network);
            serializedNetwork.FindProperty("_combatHud").objectReferenceValue = canvas.gameObject;
            serializedNetwork.ApplyModifiedPropertiesWithoutUndo();

            if (root.GetComponent<BoxCollider>() != null && root.GetComponent<PlayerDamageCollider>() == null)
                root.AddComponent<PlayerDamageCollider>();

            PrefabUtility.SaveAsPrefabAsset(root, PlayerPrefabPath);
            Debug.Log("[Offline] Cura adicionada ao PlayerVR V3 (componente PlayerHeal + HUD HealAbility).");
        }
        finally
        {
            PrefabUtility.UnloadPrefabContents(root);
        }
    }

    // ---------- Scenes ----------

    private static void CopyScenes(bool overwrite)
    {
        Directory.CreateDirectory(ScenesFolder);
        foreach (var (original, copy) in SceneCopies.Concat(MapCopies))
        {
            bool exists = File.Exists(copy);
            if (exists && !overwrite)
                continue;
            if (exists)
                AssetDatabase.DeleteAsset(copy);
            if (!AssetDatabase.CopyAsset(original, copy))
                Debug.LogError($"[Offline] Falha ao copiar {original} para {copy}.");
        }
        AssetDatabase.Refresh();
    }

    private static void ConfigureGameplayScene(string path, bool sceneRigIsPlayer)
    {
        Scene scene = EditorSceneManager.OpenScene(path, OpenSceneMode.Single);

        OfflineSceneBootstrap bootstrap = GetOrAdd<OfflineSceneBootstrap>(FindOrCreateRoot(scene, BootstrapName));
        SetObjectArray(bootstrap, "_disableOnLoad", OnlineOnlyObjects(scene));

        ConfigureAutoAdvance(scene, bootstrap);

        if (sceneRigIsPlayer)
        {
            foreach (GameObject rig in SceneRigs(scene))
                AddRigPlacement(rig);
        }
        else
        {
            // The player is spawned by GameController/SimulationController; a rig left in the scene would be a second one.
            foreach (GameObject rig in SceneRigs(scene))
                Debug.LogWarning($"[Offline] {path}: rig '{rig.name}' já está na cena além do player instanciado. Verifique se deve ficar ativo.", rig);
        }

        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
    }

    private static void ConfigureEntryScene()
    {
        Scene scene = EditorSceneManager.OpenScene(EntryScenePath, OpenSceneMode.Single);

        // The PC operator view (Screen Space Overlay menu + extra camera/listener) is not usable in the headset.
        var disable = OnlineOnlyObjects(scene);
        foreach (UserCam userCam in FindInScene<UserCam>(scene))
            disable.Add(userCam.gameObject);

        OfflineSceneBootstrap bootstrap = GetOrAdd<OfflineSceneBootstrap>(FindOrCreateRoot(scene, BootstrapName));
        SetObjectArray(bootstrap, "_disableOnLoad", disable.Distinct().ToList());

        foreach (PlayerPrefabNetwork player in FindInScene<PlayerPrefabNetwork>(scene))
            AddRigPlacement(player.gameObject);

        EnsureXRUIEventSystem(scene);

        OfflineModeMenu menu = FindInScene<OfflineModeMenu>(scene).FirstOrDefault();
        if (menu == null)
            menu = FindOrCreateRoot(scene, "[Offline] Mode Selection").AddComponent<OfflineModeMenu>();

        GameObject oldPanel = scene.GetRootGameObjects().FirstOrDefault(go => go.name == MenuPanelName);
        if (oldPanel != null)
            Object.DestroyImmediate(oldPanel);
        BuildMenuPanel(scene, menu);

        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
    }

    /// <summary>
    /// Replaces the host's clicks: each step waits for the narration that preceded the click and then
    /// calls the same method the host button called.
    /// </summary>
    private static void ConfigureAutoAdvance(Scene scene, OfflineSceneBootstrap bootstrap)
    {
        foreach (GameObject old in scene.GetRootGameObjects().Where(go => go.name.StartsWith(AutoAdvancePrefix)).ToList())
            Object.DestroyImmediate(old);

        // GloboV2 lobby: nothing to automate here any more. Eve's dialogue advances by itself (Balcony, offline)
        // and ends by showing the globe; the map choice is the player's input (see ConfigureMapSelection).

        // Battle: after "inimigos se aproximam..." (GameController.OnSceneLoaded) the host pressed StartBattle.
        foreach (GameController controller in FindInScene<GameController>(scene))
        {
            AudioSource intro = PlayedAudio(controller.OnSceneLoaded);
            bool hasSwitch = new SerializedObject(controller).FindProperty("_switch").objectReferenceValue != null;
            if (intro == null || !hasSwitch)
                continue;

            OfflineAutoAdvance step = CreateStep(scene, AutoAdvancePrefix + " - Battle", controller.gameObject, intro, 1.5f);
            UnityEventTools.AddPersistentListener(step.OnAdvance, controller.ActiveBattle);
        }

        // Credits: when the roll ends the host sent everyone back; offline returns to the Offline menu.
        var creditsFinish = typeof(TitleFunctions).GetField("OnCreditsFinish",
            System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
        foreach (TitleFunctions credits in FindInScene<TitleFunctions>(scene))
        {
            var unityEvent = (UnityEvent)creditsFinish.GetValue(credits);
            for (int i = unityEvent.GetPersistentEventCount() - 1; i >= 0; i--)
            {
                if (unityEvent.GetPersistentTarget(i) is OfflineSceneBootstrap)
                    UnityEventTools.RemovePersistentListener(unityEvent, i);
            }
            UnityEventTools.AddPersistentListener(unityEvent, bootstrap.ReturnToEntry);
            EditorUtility.SetDirty(credits);
        }
    }

    private static OfflineAutoAdvance CreateStep(Scene scene, string name, GameObject waitUntilActive, AudioSource audio, float delay)
    {
        GameObject go = FindOrCreateRoot(scene, name);
        var step = go.AddComponent<OfflineAutoAdvance>();
        var serialized = new SerializedObject(step);
        serialized.FindProperty("_waitUntilActive").objectReferenceValue = waitUntilActive;
        serialized.FindProperty("_waitForAudio").objectReferenceValue = audio;
        serialized.FindProperty("_delayAfter").floatValue = delay;
        serialized.ApplyModifiedPropertiesWithoutUndo();
        if (audio == null)
            Debug.LogWarning($"[Offline] {scene.name}: '{name}' sem narração encontrada; vai avançar só pelo tempo.", go);
        return step;
    }

    private static GameObject ActivatedObject(UnityEvent unityEvent)
    {
        for (int i = 0; i < unityEvent.GetPersistentEventCount(); i++)
        {
            if (unityEvent.GetPersistentTarget(i) is GameObject go && unityEvent.GetPersistentMethodName(i) == "SetActive" && !go.activeSelf)
                return go;
        }
        return null;
    }

    private static AudioSource PlayedAudio(UnityEvent unityEvent)
    {
        if (unityEvent == null)
            return null;

        for (int i = 0; i < unityEvent.GetPersistentEventCount(); i++)
        {
            if (unityEvent.GetPersistentTarget(i) is AudioSource audio && unityEvent.GetPersistentMethodName(i) == "Play")
                return audio;
        }
        return null;
    }

    /// <summary>GloboV2: the three buttons over the map picture load the map scenes instead of the local arena.</summary>
    private static void ConfigureMapSelection(string path)
    {
        Scene scene = EditorSceneManager.OpenScene(path, OpenSceneMode.Single);

        RelinkEveIntro(scene);
        GameObject globeForEve = null;

        GameObject old = scene.GetRootGameObjects().FirstOrDefault(go => go.name == MapSelectionName);
        if (old != null)
            Object.DestroyImmediate(old);

        List<Button> buttons = FindInScene<Button>(scene)
            .Where(b => MapByButtonName.ContainsKey(b.name) && CallsFinish(b))
            .ToList();
        if (buttons.Count == 0)
        {
            Debug.LogError($"[Offline] {scene.name}: não achei os botões de mapa (Button, Button (1), Button (2) chamando Finish) no globo.");
            return;
        }

        Canvas panelCanvas = buttons[0].GetComponentInParent<Canvas>(true);
        if (panelCanvas.GetComponent<TrackedDeviceGraphicRaycaster>() == null)
            panelCanvas.gameObject.AddComponent<TrackedDeviceGraphicRaycaster>();

        var dialogueEnd = typeof(Balcony).GetField("OnDialogueEnd",
            System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
        GameObject globe = FindInScene<Balcony>(scene)
            .Select(b => ActivatedObject((UnityEvent)dialogueEnd.GetValue(b)))
            .FirstOrDefault(go => go != null);
        if (globe == null)
            Debug.LogWarning($"[Offline] {scene.name}: não achei o globo ativado no fim do diálogo; o painel de mapas abre já no início.");
        globeForEve = globe;
        AddEveEndStep(scene, globeForEve);

        var selection = FindOrCreateRoot(scene, MapSelectionName).AddComponent<OfflineMapSelection>();
        var serialized = new SerializedObject(selection);
        serialized.FindProperty("_globe").objectReferenceValue = globe;
        serialized.FindProperty("_mapPanel").objectReferenceValue = panelCanvas.gameObject;
        SerializedProperty maps = serialized.FindProperty("_maps");
        maps.arraySize = buttons.Count;
        for (int i = 0; i < buttons.Count; i++)
        {
            SerializedProperty entry = maps.GetArrayElementAtIndex(i);
            entry.FindPropertyRelative("Button").objectReferenceValue = buttons[i];
            entry.FindPropertyRelative("Scene").stringValue = MapByButtonName[buttons[i].name];
        }
        serialized.ApplyModifiedPropertiesWithoutUndo();

        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
    }

    /// <summary>
    /// SimulationController.OnExperienceBegin should Play Eve's intro timeline, but in GloboV2 the reference is
    /// empty since the lobby was rebuilt (the director now lives on "Lobby"). Point the empty call at that director.
    /// </summary>
    private static void RelinkEveIntro(Scene scene)
    {
        SimulationController simulation = FindInScene<SimulationController>(scene).FirstOrDefault();
        PlayableDirector[] directors = FindInScene<PlayableDirector>(scene).ToArray();
        if (simulation == null || directors.Length == 0)
            return;

        PlayableDirector eve = directors.FirstOrDefault(d => d.playableAsset != null && d.playableAsset.name.Contains("EVE"))
                               ?? directors[0];

        var serialized = new SerializedObject(simulation);
        SerializedProperty calls = serialized.FindProperty("OnExperienceBegin.m_PersistentCalls.m_Calls");
        for (int i = 0; i < calls.arraySize; i++)
        {
            SerializedProperty call = calls.GetArrayElementAtIndex(i);
            bool isDirectorPlay = call.FindPropertyRelative("m_MethodName").stringValue == "Play" &&
                                  call.FindPropertyRelative("m_TargetAssemblyTypeName").stringValue.StartsWith("UnityEngine.Playables.PlayableDirector");
            SerializedProperty target = call.FindPropertyRelative("m_Target");
            if (isDirectorPlay && target.objectReferenceValue == null)
            {
                target.objectReferenceValue = eve;
                Debug.Log($"[Offline] {scene.name}: fala inicial da Eve religada ao PlayableDirector '{eve.name}' ({eve.playableAsset?.name}).", eve);
            }
        }
        serialized.ApplyModifiedPropertiesWithoutUndo();
    }

    /// <summary>
    /// Eve's timeline ends with EveLobbySignal -> SignalReceiver -> Balcony.ServiceDisable (hide the hologram, show the
    /// globe, play "Seleção de Fase"). The emitter sits on the timeline's last frame, which the director never evaluates,
    /// so the signal never fires. Offline: when the timeline ends, call the same reaction.
    /// </summary>
    private static void AddEveEndStep(Scene scene, GameObject globe)
    {
        PlayableDirector director = FindInScene<PlayableDirector>(scene)
            .FirstOrDefault(d => d.playableAsset != null && d.playableAsset.name.Contains("EVE"));
        if (director == null)
            return;

        foreach (SignalReceiver receiver in FindInScene<SignalReceiver>(scene))
        {
            for (int r = 0; r < receiver.Count(); r++)
            {
                UnityEvent reaction = receiver.GetReactionAtIndex(r);
                for (int i = 0; i < reaction.GetPersistentEventCount(); i++)
                {
                    if (reaction.GetPersistentTarget(i) is not Balcony balcony || reaction.GetPersistentMethodName(i) != "ServiceDisable")
                        continue;

                    OfflineAutoAdvance step = CreateStep(scene, AutoAdvancePrefix + " - Eve", null, null, 0.3f);
                    var serialized = new SerializedObject(step);
                    serialized.FindProperty("_waitForDirector").objectReferenceValue = director;
                    // If the signal did fire, the globe is already up: do not run the reaction twice.
                    serialized.FindProperty("_skipIfActive").objectReferenceValue = globe;
                    serialized.ApplyModifiedPropertiesWithoutUndo();
                    UnityEventTools.AddPersistentListener(step.OnAdvance, balcony.ServiceDisable);
                    return;
                }
            }
        }

        Debug.LogWarning($"[Offline] {scene.name}: não achei o SignalReceiver da Eve (Balcony.ServiceDisable); o fim da timeline não vai avançar.");
    }

    /// <summary>
    /// Offline start point in the middle of the room (bounds of the room's visible objects, standing on the floor).
    /// If the centre is on furniture (e.g. the stretcher), it slides towards the original spawn until it finds floor.
    /// An existing spawn point is kept, so it can be moved by hand.
    /// </summary>
    private static void ConfigureSpawnPoint(string path, string roomRootName)
    {
        Scene scene = EditorSceneManager.OpenScene(path, OpenSceneMode.Single);

        Renderer[] sceneRenderers = FindInScene<Renderer>(scene)
            .Where(r => r.enabled && r.gameObject.activeInHierarchy && r is MeshRenderer or SkinnedMeshRenderer)
            .Where(r => r.GetComponentInParent<PlayerPrefabNetwork>(true) == null)
            .ToArray();

        GameObject existing = scene.GetRootGameObjects().FirstOrDefault(go => go.name == SpawnPointName);
        if (existing != null)
        {
            // Keep a point that is on free floor (possibly moved by hand); redo one that landed on furniture.
            if (!InsideFurniture(sceneRenderers, existing.transform.position, existing.transform.position.y))
                return;
            Object.DestroyImmediate(existing);
        }

        GameObject room = scene.GetRootGameObjects().FirstOrDefault(go => go.name == roomRootName);
        Renderer[] renderers = room != null
            ? room.GetComponentsInChildren<Renderer>().Where(r => r.enabled && r is MeshRenderer or SkinnedMeshRenderer).ToArray()
            : new Renderer[0];
        if (renderers.Length == 0)
        {
            Debug.LogWarning($"[Offline] {scene.name}: sala '{roomRootName}' não encontrada; spawn offline não criado.");
            return;
        }

        Bounds bounds = renderers[0].bounds;
        foreach (Renderer r in renderers)
            bounds.Encapsulate(r.bounds);

        // Original start and facing of this scene.
        Vector3 originalPosition = bounds.center;
        Vector3 facing = Vector3.forward;
        PlayerPrefabNetwork sceneRig = FindInScene<PlayerPrefabNetwork>(scene).FirstOrDefault(p => p.gameObject.activeInHierarchy);
        SimulationController simulation = FindInScene<SimulationController>(scene).FirstOrDefault();
        if (sceneRig != null)
        {
            originalPosition = sceneRig.transform.position;
            facing = sceneRig.transform.forward;
        }
        else if (simulation != null && simulation.SpawnPoints != null && simulation.SpawnPoints.Length > 0 && simulation.SpawnPoints[0] != null)
        {
            originalPosition = simulation.SpawnPoints[0].position;
            facing = simulation.SpawnPoints[0].up; // same facing as the online spawn (LookRotation(up))
        }

        Physics.SyncTransforms();
        // Floor = ground under the original spawn (where the game already puts the player).
        float floor = bounds.min.y;
        if (Physics.Raycast(originalPosition + Vector3.up * 0.5f, Vector3.down, out RaycastHit floorHit, 10f))
            floor = floorHit.point.y;

        Vector3 centre = new Vector3(bounds.center.x, floor, bounds.center.z);
        Vector3 target = new Vector3(originalPosition.x, floor, originalPosition.z);
        Vector3 spawn = centre;
        for (int step = 0; step <= 40; step++)
        {
            Vector3 probe = Vector3.Lerp(centre, target, step / 40f);
            if (!Physics.Raycast(probe + Vector3.up * (bounds.size.y + 1f), Vector3.down, out RaycastHit hit, bounds.size.y + 5f))
            {
                if (InsideFurniture(sceneRenderers, probe, floor))
                    continue;
                spawn = probe;
                break;
            }
            if (hit.point.y <= floor + 0.3f && !InsideFurniture(sceneRenderers, probe, floor))
            {
                spawn = new Vector3(probe.x, hit.point.y, probe.z);
                break;
            }
        }

        // Face the middle of the room when the point had to move away from it.
        Vector3 toCentre = Vector3.ProjectOnPlane(centre - spawn, Vector3.up);
        if (toCentre.magnitude > 0.5f)
            facing = toCentre;
        facing = Vector3.ProjectOnPlane(facing, Vector3.up);
        if (facing.sqrMagnitude < 0.0001f)
            facing = Vector3.forward;

        var point = new GameObject(SpawnPointName);
        SceneManager.MoveGameObjectToScene(point, scene);
        point.transform.SetPositionAndRotation(spawn, Quaternion.LookRotation(facing.normalized, Vector3.up));
        point.AddComponent<OfflineSpawnPoint>();
        Debug.Log($"[Offline] {scene.name}: spawn offline criado em {spawn} (centro da sala {centre}). Mova '{SpawnPointName}' se quiser ajustar.", point);

        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
    }

    // Furniture without colliders (e.g. the stretcher): a small object occupying body height at this point.
    private static bool InsideFurniture(IEnumerable<Renderer> renderers, Vector3 point, float floor)
    {
        foreach (Renderer r in renderers)
        {
            Bounds b = r.bounds;
            bool small = b.size.x < 4f && b.size.z < 4f;
            bool bodyHeight = b.min.y < floor + 1.5f && b.max.y > floor + 0.3f;
            bool overPoint = point.x > b.min.x - 0.3f && point.x < b.max.x + 0.3f && point.z > b.min.z - 0.3f && point.z < b.max.z + 0.3f;
            if (small && bodyHeight && overPoint)
                return true;
        }
        return false;
    }

    /// <summary>Medical panel: swap the symbol/ASCII-only fonts for a Zekton font asset that has the Portuguese accents.</summary>
    private static void ConfigureMedicalFonts(string path)
    {
        TMP_FontAsset readable = GetOrCreateReadableFont();
        if (readable == null)
            return;

        var unreadable = UnreadableFonts.Append(OldDynamicFontPath)
            .Select(AssetDatabase.LoadAssetAtPath<TMP_FontAsset>).Where(f => f != null).ToList();
        Scene scene = EditorSceneManager.OpenScene(path, OpenSceneMode.Single);
        int changed = 0;
        foreach (TMP_Text text in FindInScene<TMP_Text>(scene))
        {
            if (!unreadable.Contains(text.font))
                continue;
            text.font = readable;
            text.fontSharedMaterial = readable.material;
            EditorUtility.SetDirty(text);
            PrefabUtility.RecordPrefabInstancePropertyModifications(text);
            changed++;
        }
        Debug.Log($"[Offline] {scene.name}: {changed} texto(s) passaram a usar '{readable.name}'.");

        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);

        if (AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(OldDynamicFontPath) != null)
            AssetDatabase.DeleteAsset(OldDynamicFontPath);
    }

    private static TMP_FontAsset GetOrCreateReadableFont()
    {
        TMP_FontAsset existing = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(ReadableFontPath);
        if (existing != null)
            return existing;

        var source = AssetDatabase.LoadAssetAtPath<Font>(ReadableFontSource);
        if (source == null)
        {
            Debug.LogError($"[Offline] Fonte {ReadableFontSource} não encontrada.");
            return null;
        }

        Directory.CreateDirectory(Path.GetDirectoryName(ReadableFontPath));
        // One 2048 atlas is enough for the Latin set at 64pt; multi-atlas textures would not be saved as sub-assets.
        TMP_FontAsset font = TMP_FontAsset.CreateFontAsset(source, 64, 6, GlyphRenderMode.SDFAA, 2048, 2048, AtlasPopulationMode.Dynamic, false);
        font.name = Path.GetFileNameWithoutExtension(ReadableFontPath);
        AssetDatabase.CreateAsset(font, ReadableFontPath);
        font.material.name = font.name + " Material";
        font.atlasTextures[0].name = font.name + " Atlas";
        AssetDatabase.AddObjectToAsset(font.material, font);
        AssetDatabase.AddObjectToAsset(font.atlasTextures[0], font);

        // Bake the Latin characters in so the build never depends on runtime glyph generation.
        const string latin = " !\"#$%&'()*+,-./0123456789:;<=>?@ABCDEFGHIJKLMNOPQRSTUVWXYZ[\\]^_`abcdefghijklmnopqrstuvwxyz{|}~" +
                             "ÀÁÂÃÄÇÈÉÊËÌÍÎÏÑÒÓÔÕÖÙÚÛÜàáâãäçèéêëìíîïñòóôõöùúûüºª°–—“”‘’…•";
        font.TryAddCharacters(latin, out string missing);
        if (!string.IsNullOrEmpty(missing.Trim('`')))
            Debug.LogWarning($"[Offline] Caracteres ausentes na Zekton: {missing}");

        // Static + keep data on build: the glyphs above are baked into the atlas that ships in the APK.
        font.atlasPopulationMode = AtlasPopulationMode.Static;
        // Not exposed as a public property in this TMP version.
        var serializedFont = new SerializedObject(font);
        serializedFont.FindProperty("m_ClearDynamicDataOnBuild").boolValue = false;
        serializedFont.ApplyModifiedPropertiesWithoutUndo();

        EditorUtility.SetDirty(font.atlasTextures[0]);
        EditorUtility.SetDirty(font.material);
        EditorUtility.SetDirty(font);
        AssetDatabase.SaveAssets();
        Debug.Log($"[Offline] Fonte legível criada: {ReadableFontPath} ({font.characterTable.Count} caracteres, estática).");
        return font;
    }

    private static bool CallsFinish(Button button)
    {
        for (int i = 0; i < button.onClick.GetPersistentEventCount(); i++)
        {
            if (button.onClick.GetPersistentTarget(i) is WaitingPlayers && button.onClick.GetPersistentMethodName(i) == "Finish")
                return true;
        }
        return false;
    }

    /// <summary>
    /// Map scene (scenery only) + the combat kit (ShootGame prefab: spawners, pools, timer, player spawn, voice).
    /// The kit goes where the map's own camera stands, dropped onto the ground; move "[Offline] Combat (ShootGame)"
    /// in the scene to fine-tune the arena position (re-running the setup keeps it).
    /// </summary>
    private static void ConfigureMapScene(string path)
    {
        Scene scene = EditorSceneManager.OpenScene(path, OpenSceneMode.Single);

        GameObject kit = scene.GetRootGameObjects().FirstOrDefault(go => go.name == CombatKitName);
        if (kit == null)
        {
            Camera mapCamera = FindInScene<Camera>(scene).FirstOrDefault();
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(ShootGamePrefabPath);
            kit = (GameObject)PrefabUtility.InstantiatePrefab(prefab, scene);
            kit.name = CombatKitName;

            Vector3 anchor = Vector3.zero;
            float yaw = 0f;
            if (mapCamera != null)
            {
                anchor = mapCamera.transform.position;
                yaw = mapCamera.transform.eulerAngles.y;
                Physics.SyncTransforms();
                if (Physics.Raycast(anchor + Vector3.up * 200f, Vector3.down, out RaycastHit hit, 2000f))
                    anchor = hit.point;
            }
            kit.transform.SetPositionAndRotation(anchor, Quaternion.Euler(0f, yaw, 0f));
            Debug.Log($"[Offline] {scene.name}: kit de combate colocado em {anchor}. Ajuste '{CombatKitName}' se a arena não estiver no lugar ideal.", kit);
        }
        kit.SetActive(true);

        // The map keeps its own light, sky and post-processing.
        foreach (Transform child in kit.GetComponentsInChildren<Transform>(true))
        {
            if (child.name == "Lighting" || child.name == "Global Volume")
                child.gameObject.SetActive(false);
        }

        OfflineSceneBootstrap bootstrap = GetOrAdd<OfflineSceneBootstrap>(FindOrCreateRoot(scene, BootstrapName));
        List<GameObject> disable = OnlineOnlyObjects(scene);
        // The map's preview camera would be a second camera/listener next to the VR rig.
        foreach (Camera camera in FindInScene<Camera>(scene).Where(c => !c.transform.IsChildOf(kit.transform)))
            disable.Add(camera.gameObject);
        SetObjectArray(bootstrap, "_disableOnLoad", disable.Distinct().ToList());

        ConfigureAutoAdvance(scene, bootstrap);

        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
    }

    private static List<GameObject> OnlineOnlyObjects(Scene scene)
    {
        var list = new List<GameObject>();
        list.AddRange(FindInScene<ConnectionManager>(scene).Select(c => c.gameObject));
        list.AddRange(FindInScene<VRSystemController>(scene).Select(c => c.gameObject));
        list.AddRange(FindInScene<PlayerList>(scene).Select(c => c.gameObject));
        return list.Distinct().ToList();
    }

    private static IEnumerable<GameObject> SceneRigs(Scene scene)
    {
        var rigs = new HashSet<GameObject>();
        foreach (XROrigin origin in FindInScene<XROrigin>(scene))
            rigs.Add(RigRoot(origin.transform));
        foreach (MonoBehaviour behaviour in FindInScene<MonoBehaviour>(scene))
        {
            if (behaviour != null && behaviour.GetType().Name == "OVRCameraRig" && behaviour.gameObject.activeInHierarchy)
                rigs.Add(RigRoot(behaviour.transform));
        }
        return rigs.Where(r => r.activeInHierarchy);
    }

    private static GameObject RigRoot(Transform t)
    {
        PlayerPrefabNetwork player = t.GetComponentInParent<PlayerPrefabNetwork>(true);
        return player != null ? player.gameObject : t.root.gameObject;
    }

    private static void AddRigPlacement(GameObject rig)
    {
        OfflinePlayerRig placement = GetOrAdd<OfflinePlayerRig>(rig);
        // Rigs that used ResetPosition keep its target (headset at the world origin); others start where they are placed.
        bool usedReset = rig.GetComponentInChildren<ResetPosition>(true) != null;
        var serialized = new SerializedObject(placement);
        serialized.FindProperty("_headTarget").enumValueIndex = usedReset
            ? (int)OfflinePlayerRig.HeadTarget.ProjectDefaultOrigin
            : (int)OfflinePlayerRig.HeadTarget.StartPointFloor;
        serialized.ApplyModifiedPropertiesWithoutUndo();
    }

    private static void EnsureXRUIEventSystem(Scene scene)
    {
        EventSystem eventSystem = FindInScene<EventSystem>(scene).FirstOrDefault();
        if (eventSystem == null)
        {
            eventSystem = FindOrCreateRoot(scene, "EventSystem").AddComponent<EventSystem>();
        }

        foreach (BaseInputModule module in eventSystem.GetComponents<BaseInputModule>())
        {
            if (module is not XRUIInputModule)
                module.enabled = false;
        }

        if (eventSystem.GetComponent<XRUIInputModule>() == null)
            eventSystem.gameObject.AddComponent<XRUIInputModule>();
    }

    // ---------- UI builders ----------

    private static void BuildMenuPanel(Scene scene, OfflineModeMenu menu)
    {
        var panel = new GameObject(MenuPanelName, typeof(RectTransform));
        SceneManager.MoveGameObjectToScene(panel, scene);

        var canvas = panel.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.WorldSpace;
        var scaler = panel.AddComponent<CanvasScaler>();
        scaler.dynamicPixelsPerUnit = 10f;
        panel.AddComponent<GraphicRaycaster>();
        panel.AddComponent<TrackedDeviceGraphicRaycaster>();

        var rect = (RectTransform)panel.transform;
        rect.sizeDelta = new Vector2(900f, 640f);
        rect.localScale = Vector3.one * 0.001f;
        rect.position = new Vector3(0f, 1.4f, 1.3f);

        Image background = AddImage(panel.transform, "Background", Vector2.zero, rect.sizeDelta, new Color(0.03f, 0.06f, 0.12f, 0.92f), UiSprite());
        background.type = Image.Type.Sliced;
        Stretch(background.rectTransform);

        AddText(panel.transform, "Title", "MODO OFFLINE", new Vector2(0f, 250f), new Vector2(820f, 90f), 64f, FontStyles.Bold);
        TMP_Text status = AddText(panel.transform, "Status", "Escolha a modalidade com o controle.", new Vector2(0f, 150f), new Vector2(820f, 110f), 34f, FontStyles.Normal);

        Button combat = AddButton(panel.transform, "Combat Button", "COMBATE", new Vector2(0f, 20f), new Color(0.85f, 0.25f, 0.2f, 1f));
        Button full = AddButton(panel.transform, "Full Experience Button", "EXPERIÊNCIA COMPLETA", new Vector2(0f, -110f), new Color(0.15f, 0.55f, 0.9f, 1f));
        Button quit = AddButton(panel.transform, "Quit Button", "SAIR", new Vector2(0f, -240f), new Color(0.3f, 0.3f, 0.35f, 1f));

        var serialized = new SerializedObject(menu);
        serialized.FindProperty("_combatButton").objectReferenceValue = combat;
        serialized.FindProperty("_fullExperienceButton").objectReferenceValue = full;
        serialized.FindProperty("_quitButton").objectReferenceValue = quit;
        serialized.FindProperty("_statusText").objectReferenceValue = status;
        serialized.FindProperty("_panel").objectReferenceValue = panel.transform;
        serialized.FindProperty("_startAutomatically").boolValue = false;
        serialized.ApplyModifiedPropertiesWithoutUndo();
    }

    private readonly struct HealWidget
    {
        public readonly Image Fill;
        public readonly CanvasGroup Icon;
        public readonly TMP_Text ButtonText;
        public readonly TMP_Text CooldownText;

        public HealWidget(Image fill, CanvasGroup icon, TMP_Text buttonText, TMP_Text cooldownText)
        {
            Fill = fill;
            Icon = icon;
            ButtonText = buttonText;
            CooldownText = cooldownText;
        }
    }

    private static HealWidget BuildHealWidget(RectTransform canvas, RectTransform lifeBar)
    {
        var widget = new GameObject(HealWidgetName, typeof(RectTransform));
        var rect = (RectTransform)widget.transform;
        rect.SetParent(canvas, false);
        rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.sizeDelta = new Vector2(160f, 200f);
        // Same pixel scale as the life bar, a bit above it.
        rect.localScale = lifeBar.localScale;
        rect.anchoredPosition = lifeBar.anchoredPosition + new Vector2(0f, 0.3f);
        // The HUD canvas is turned towards the camera; face exactly like the camera so text is never mirrored.
        rect.localRotation = Quaternion.Inverse(canvas.localRotation);

        Sprite circle = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/Knob.psd");

        AddImage(rect, "Backdrop", new Vector2(0f, -10f), new Vector2(136f, 136f), new Color(0f, 0f, 0f, 0.6f), circle);
        Image fill = AddImage(rect, "CooldownFill", new Vector2(0f, -10f), new Vector2(128f, 128f), new Color(0.2f, 1f, 0.45f, 1f), circle);
        fill.type = Image.Type.Filled;
        fill.fillMethod = Image.FillMethod.Radial360;
        fill.fillOrigin = (int)Image.Origin360.Top;
        fill.fillClockwise = true;
        AddImage(rect, "Inner", new Vector2(0f, -10f), new Vector2(104f, 104f), new Color(0.05f, 0.08f, 0.1f, 0.95f), circle);

        var icon = new GameObject("Icon", typeof(RectTransform));
        var iconRect = (RectTransform)icon.transform;
        iconRect.SetParent(rect, false);
        iconRect.anchoredPosition = new Vector2(0f, -10f);
        var iconGroup = icon.AddComponent<CanvasGroup>();
        iconGroup.interactable = false;
        iconGroup.blocksRaycasts = false;
        AddImage(iconRect, "CrossVertical", Vector2.zero, new Vector2(18f, 60f), new Color(0.3f, 1f, 0.5f, 1f), UiSprite());
        AddImage(iconRect, "CrossHorizontal", Vector2.zero, new Vector2(60f, 18f), new Color(0.3f, 1f, 0.5f, 1f), UiSprite());

        TMP_Text cooldownText = AddText(rect, "CooldownText", string.Empty, new Vector2(0f, -10f), new Vector2(120f, 80f), 52f, FontStyles.Bold);

        // Button to press, drawn on top of the icon.
        Image keycap = AddImage(rect, "ButtonBadge", new Vector2(0f, 72f), new Vector2(120f, 46f), new Color(0.95f, 0.95f, 0.95f, 1f), UiSprite());
        keycap.type = Image.Type.Sliced;
        TMP_Text buttonText = AddText(keycap.transform, "ButtonText", "A / X", Vector2.zero, new Vector2(120f, 46f), 30f, FontStyles.Bold);
        buttonText.color = new Color(0.08f, 0.08f, 0.08f, 1f);

        return new HealWidget(fill, iconGroup, buttonText, cooldownText);
    }

    private static Button AddButton(Transform parent, string name, string label, Vector2 position, Color color)
    {
        Image image = AddImage(parent, name, position, new Vector2(720f, 100f), color, UiSprite());
        image.type = Image.Type.Sliced;
        image.raycastTarget = true;
        var button = image.gameObject.AddComponent<Button>();
        ColorBlock colors = button.colors;
        colors.highlightedColor = new Color(1.15f, 1.15f, 1.15f, 1f);
        colors.pressedColor = new Color(0.7f, 0.7f, 0.7f, 1f);
        button.colors = colors;
        AddText(image.transform, "Label", label, Vector2.zero, new Vector2(700f, 100f), 44f, FontStyles.Bold);
        return button;
    }

    private static Image AddImage(Transform parent, string name, Vector2 position, Vector2 size, Color color, Sprite sprite)
    {
        var go = new GameObject(name, typeof(RectTransform));
        var rect = (RectTransform)go.transform;
        rect.SetParent(parent, false);
        rect.anchoredPosition = position;
        rect.sizeDelta = size;
        var image = go.AddComponent<Image>();
        image.sprite = sprite;
        image.color = color;
        image.raycastTarget = false;
        return image;
    }

    private static TMP_Text AddText(Transform parent, string name, string text, Vector2 position, Vector2 size, float fontSize, FontStyles style)
    {
        var go = new GameObject(name, typeof(RectTransform));
        var rect = (RectTransform)go.transform;
        rect.SetParent(parent, false);
        rect.anchoredPosition = position;
        rect.sizeDelta = size;
        var tmp = go.AddComponent<TextMeshProUGUI>();
        TMP_FontAsset font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(AssetDatabase.GUIDToAssetPath(FontGuid));
        if (font != null)
            tmp.font = font;
        tmp.text = text;
        tmp.fontSize = fontSize;
        tmp.fontStyle = style;
        tmp.alignment = TextAlignmentOptions.Center;
        tmp.color = Color.white;
        tmp.raycastTarget = false;
        return tmp;
    }

    private static void Stretch(RectTransform rect)
    {
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;
    }

    private static Sprite UiSprite() => AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/UISprite.psd");

    // ---------- Helpers ----------

    private static void UpdateBuildSettings()
    {
        var offlinePaths = AllOfflineScenePaths.ToList();

        List<EditorBuildSettingsScene> scenes = EditorBuildSettings.scenes
            .Where(s => s.path != "Assets/Scenes/Offline.unity" && !offlinePaths.Contains(s.path))
            .ToList();
        scenes.AddRange(offlinePaths.Select(p => new EditorBuildSettingsScene(p, true)));
        EditorBuildSettings.scenes = scenes.ToArray();
    }

    private static GameObject FindOrCreateRoot(Scene scene, string name)
    {
        GameObject existing = scene.GetRootGameObjects().FirstOrDefault(go => go.name == name);
        if (existing != null)
            return existing;

        var created = new GameObject(name);
        SceneManager.MoveGameObjectToScene(created, scene);
        return created;
    }

    // Explicit null check: in the Editor a missing component is a "fake null" that ?? does not catch.
    private static T GetOrAdd<T>(GameObject go) where T : Component
    {
        T component = go.GetComponent<T>();
        if (component == null)
            component = go.AddComponent<T>();
        return component;
    }

    private static IEnumerable<T> FindInScene<T>(Scene scene) where T : Component =>
        scene.GetRootGameObjects().SelectMany(root => root.GetComponentsInChildren<T>(true));

    private static void SetObjectArray(Object target, string property, IList<GameObject> values)
    {
        var serialized = new SerializedObject(target);
        SerializedProperty array = serialized.FindProperty(property);
        array.arraySize = values.Count;
        for (int i = 0; i < values.Count; i++)
            array.GetArrayElementAtIndex(i).objectReferenceValue = values[i];
        serialized.ApplyModifiedPropertiesWithoutUndo();
    }
}
