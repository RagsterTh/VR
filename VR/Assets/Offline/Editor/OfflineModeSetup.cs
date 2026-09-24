using System.Collections.Generic;
using System.IO;
using System.Linq;
using TMPro;
using Unity.XR.CoreUtils;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
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

    // Original scene -> offline copy.
    private static readonly (string original, string copy)[] SceneCopies =
    {
        ("Assets/Scenes/Game.unity", ScenesFolder + "/" + OfflineSession.CombatScene + ".unity"),
        ("Assets/Scenes/GloboV2.unity", ScenesFolder + "/" + OfflineSession.FullExperienceScene + ".unity"),
        ("Assets/Scenes/MedicalQuestions.unity", ScenesFolder + "/" + OfflineSession.MedicalScene + ".unity"),
        ("Assets/Scenes/Credits.unity", ScenesFolder + "/" + OfflineSession.CreditsScene + ".unity"),
    };

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
        foreach (var (original, copy) in SceneCopies)
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
        var offlinePaths = new List<string> { EntryScenePath };
        offlinePaths.AddRange(SceneCopies.Select(s => s.copy));

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
