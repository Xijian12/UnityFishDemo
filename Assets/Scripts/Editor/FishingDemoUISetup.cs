#if UNITY_EDITOR
using System.Collections.Generic;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 菜单：FishingDemo / Setup Game UI — 从预制体实例化 GameUI 并绑定引用。
/// 预制体由 FishingDemo / Build UI Panel Prefabs 生成。
/// </summary>
public static class FishingDemoUISetup
{
    private const string MenuPath = "FishingDemo/Setup Game UI";

    [MenuItem(MenuPath)]
    public static void SetupGameUI()
    {
        if (!FishingDemoUIPrefabBuilder.AllPanelPrefabsExist())
            FishingDemoUIPrefabBuilder.BuildAllPanelPrefabs();

        Canvas canvas = Object.FindFirstObjectByType<Canvas>();
        if (canvas == null)
        {
            var canvasGo = new GameObject("Canvas", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            canvas = canvasGo.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            var scaler = canvasGo.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
        }

        if (Object.FindFirstObjectByType<UnityEngine.EventSystems.EventSystem>() == null)
        {
            new GameObject("EventSystem",
                typeof(UnityEngine.EventSystems.EventSystem),
                typeof(UnityEngine.EventSystems.StandaloneInputModule));
        }

        Transform existingRoot = canvas.transform.Find("GameUI");
        if (existingRoot != null)
            Object.DestroyImmediate(existingRoot.gameObject);

        canvas.sortingOrder = 100;

        var uiRoot = new GameObject("GameUI", typeof(RectTransform));
        uiRoot.transform.SetParent(canvas.transform, false);
        uiRoot.transform.SetAsLastSibling();
        StretchFullPublic(uiRoot.GetComponent<RectTransform>());

        var uiManager = uiRoot.AddComponent<UIManager>();

        var mainMenu = FishingDemoUIPrefabBuilder.InstantiatePanel<MainMenuPanel>(
            uiRoot.transform, FishingDemoUIPrefabBuilder.MainMenuPrefab, active: true);
        var levelSelect = FishingDemoUIPrefabBuilder.InstantiatePanel<LevelSelectPanel>(
            uiRoot.transform, FishingDemoUIPrefabBuilder.LevelSelectPrefab, active: false);
        var hud = FishingDemoUIPrefabBuilder.InstantiatePanel<GameplayHudPanel>(
            uiRoot.transform, FishingDemoUIPrefabBuilder.GameplayHudPrefab, active: false);
        var pause = FishingDemoUIPrefabBuilder.InstantiatePanel<PausePanel>(
            uiRoot.transform, FishingDemoUIPrefabBuilder.PausePrefab, active: false);
        var result = FishingDemoUIPrefabBuilder.InstantiatePanel<LevelResultPanel>(
            uiRoot.transform, FishingDemoUIPrefabBuilder.LevelResultPrefab, active: false);

        LevelCatalogConfig catalog = FindOrCreateCatalog();
        SyncCatalogLevels(catalog);
        LevelManager levelManager = Object.FindFirstObjectByType<LevelManager>();
        CannonController cannon = Object.FindFirstObjectByType<CannonController>();

        BindUIManager(uiManager, catalog, levelManager, cannon, mainMenu, levelSelect, hud, pause, result);

        if (levelManager != null)
        {
            var so = new SerializedObject(levelManager);
            so.FindProperty("waitForUI").boolValue = true;
            so.FindProperty("runLevelOnStart").boolValue = false;
            so.FindProperty("levelCatalog").objectReferenceValue = catalog;
            so.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(levelManager);
        }

        Selection.activeGameObject = uiRoot;
        Debug.Log("FishingDemo UI created from panel prefabs under Assets/Prefabs/UI/Panels/.");
    }

    #region Panel builders (used by FishingDemoUIPrefabBuilder)

    public static MainMenuPanel CreateMainMenuPanel(Transform parent)
    {
        var panel = CreatePanel<MainMenuPanel>(parent, "MainMenuPanel", new Color(0.05f, 0.12f, 0.22f, 0.92f));
        CreateTmp(panel.transform, "Title", "Fish Battle", 72, new Vector2(0.5f, 0.72f), new Vector2(800, 120));
        var startBtn = CreateButton(panel.transform, "BtnStart", "Start Game", new Vector2(0.5f, 0.42f));
        WireMainMenu(panel, startBtn);
        return panel;
    }

    public static LevelSelectPanel CreateLevelSelectPanel(Transform parent)
    {
        var panel = CreatePanel<LevelSelectPanel>(parent, "LevelSelectPanel", new Color(0.04f, 0.1f, 0.18f, 0.95f));
        panel.gameObject.SetActive(false);
        CreateTmp(panel.transform, "Title", "Select Level", 56, new Vector2(0.5f, 0.88f), new Vector2(600, 80));

        var backBtn = CreateButton(panel.transform, "BtnBack", "Back", new Vector2(0.5f, 0.08f));
        backBtn.transform.SetAsFirstSibling();

        var scrollGo = new GameObject("ScrollView", typeof(RectTransform), typeof(Image), typeof(ScrollRect));
        scrollGo.transform.SetParent(panel.transform, false);
        var scrollRt = scrollGo.GetComponent<RectTransform>();
        scrollRt.anchorMin = new Vector2(0.15f, 0.2f);
        scrollRt.anchorMax = new Vector2(0.85f, 0.78f);
        scrollRt.offsetMin = scrollRt.offsetMax = Vector2.zero;
        scrollGo.GetComponent<Image>().color = new Color(0f, 0f, 0f, 0.25f);

        var viewport = new GameObject("Viewport", typeof(RectTransform), typeof(Image), typeof(Mask));
        viewport.transform.SetParent(scrollGo.transform, false);
        var viewportRt = viewport.GetComponent<RectTransform>();
        StretchFullPublic(viewportRt);
        var viewportImg = viewport.GetComponent<Image>();
        viewportImg.color = new Color(1f, 1f, 1f, 0.01f);
        viewport.GetComponent<Mask>().showMaskGraphic = false;

        var content = new GameObject("Content", typeof(RectTransform), typeof(VerticalLayoutGroup), typeof(ContentSizeFitter));
        content.transform.SetParent(viewport.transform, false);
        var contentRt = content.GetComponent<RectTransform>();
        contentRt.anchorMin = new Vector2(0f, 1f);
        contentRt.anchorMax = new Vector2(1f, 1f);
        contentRt.pivot = new Vector2(0.5f, 1f);
        contentRt.anchoredPosition = Vector2.zero;
        contentRt.sizeDelta = new Vector2(0, 0);
        var vlg = content.GetComponent<VerticalLayoutGroup>();
        vlg.spacing = 12;
        vlg.padding = new RectOffset(16, 16, 16, 16);
        vlg.childControlHeight = true;
        vlg.childControlWidth = true;
        vlg.childForceExpandHeight = false;
        vlg.childForceExpandWidth = true;
        content.GetComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;

        var scroll = scrollGo.GetComponent<ScrollRect>();
        scroll.viewport = viewportRt;
        scroll.content = contentRt;
        scroll.horizontal = false;
        scroll.vertical = true;

        var template = CreateListButton(content.transform, "LevelButtonTemplate", "Level 1");
        template.gameObject.SetActive(false);

        WireLevelSelect(panel, content.transform, template, backBtn);
        return panel;
    }

    public static GameplayHudPanel CreateGameplayHudPanel(Transform parent)
    {
        var panel = CreatePanel<GameplayHudPanel>(parent, "GameplayHudPanel", new Color(0f, 0f, 0f, 0f));
        panel.gameObject.SetActive(false);
        var hudImage = panel.GetComponent<Image>();
        hudImage.raycastTarget = false;
        hudImage.color = new Color(0f, 0f, 0f, 0f);

        CreateTmp(panel.transform, "LevelTitle", "Level 1", 36, new Vector2(0.5f, 0.95f), new Vector2(600, 50));
        var timer = CreateTmp(panel.transform, "Timer", "120", 48, new Vector2(0.5f, 0.88f), new Vector2(200, 60));
        timer.alignment = TextAlignmentOptions.Center;
        timer.fontStyle = FontStyles.Bold;

        WireGameplayHud(panel, timer, null);
        return panel;
    }

    public static void WireGameplayHud(GameplayHudPanel panel, ScorePanelView scorePanel)
    {
        WireGameplayHud(panel, panel.transform.Find("Timer")?.GetComponent<TextMeshProUGUI>(), scorePanel);
    }

    public static void WireGameplayHud(GameplayHudPanel panel, TextMeshProUGUI timer, ScorePanelView scorePanel)
    {
        var so = new SerializedObject(panel);
        so.FindProperty("timerText").objectReferenceValue = timer;
        so.FindProperty("levelTitleText").objectReferenceValue = panel.transform.Find("LevelTitle")?.GetComponent<TextMeshProUGUI>();
        so.FindProperty("scorePanel").objectReferenceValue = scorePanel;
        so.ApplyModifiedPropertiesWithoutUndo();
    }

    public static PausePanel CreatePausePanelInstance(Transform parent)
    {
        var panel = CreatePanel<PausePanel>(parent, "PausePanel", new Color(0f, 0f, 0f, 0.75f));
        panel.gameObject.SetActive(false);
        CreateTmp(panel.transform, "Title", "Pause", 56, new Vector2(0.5f, 0.72f), new Vector2(400, 80));
        var canon = CreateTmp(panel.transform, "CanonLevel", "Single Cannon Lv.1", 32, new Vector2(0.5f, 0.58f), new Vector2(500, 50));
        var bullet = CreateTmp(panel.transform, "BulletType", "Bullet: SmallBullet", 24, new Vector2(0.5f, 0.5f), new Vector2(500, 40));
        var resume = CreateButton(panel.transform, "BtnResume", "Continue", new Vector2(0.5f, 0.36f));
        var restart = CreateButton(panel.transform, "BtnRestart", "Restart", new Vector2(0.5f, 0.26f));
        var menu = CreateButton(panel.transform, "BtnMainMenu", "Main Menu", new Vector2(0.5f, 0.16f));
        WirePause(panel, canon, bullet, resume, restart, menu);
        return panel;
    }

    public static LevelResultPanel CreateLevelResultPanelInstance(Transform parent)
    {
        var panel = CreatePanel<LevelResultPanel>(parent, "LevelResultPanel", new Color(0f, 0f, 0f, 0.82f));
        panel.gameObject.SetActive(false);
        var title = CreateTmp(panel.transform, "Title", "Victory!", 64, new Vector2(0.5f, 0.65f), new Vector2(500, 90));
        var score = CreateTmp(panel.transform, "Score", "Score: 0", 40, new Vector2(0.5f, 0.52f), new Vector2(500, 60));
        var detail = CreateTmp(panel.transform, "Detail", "", 28, new Vector2(0.5f, 0.44f), new Vector2(600, 50));
        var retry = CreateButton(panel.transform, "BtnRetry", "Retry", new Vector2(0.5f, 0.28f));
        var menu = CreateButton(panel.transform, "BtnMainMenu", "Main Menu", new Vector2(0.5f, 0.18f));
        WireResult(panel, title, score, detail, retry, menu);
        return panel;
    }

    public static void StretchFullPublic(RectTransform rt)
    {
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = rt.offsetMax = Vector2.zero;
    }

    #endregion

    private static T CreatePanel<T>(Transform parent, string name, Color bg) where T : Component
    {
        var go = new GameObject(name, typeof(RectTransform), typeof(Image), typeof(T));
        go.transform.SetParent(parent, false);
        StretchFullPublic(go.GetComponent<RectTransform>());
        go.GetComponent<Image>().color = bg;
        return go.GetComponent<T>();
    }

    private static TextMeshProUGUI CreateTmp(Transform parent, string name, string text, float fontSize,
        Vector2 anchor, Vector2 size)
    {
        var go = new GameObject(name, typeof(RectTransform), typeof(TextMeshProUGUI));
        go.transform.SetParent(parent, false);
        var rt = go.GetComponent<RectTransform>();
        rt.anchorMin = rt.anchorMax = anchor;
        rt.pivot = new Vector2(0.5f, 0.5f);
        rt.sizeDelta = size;
        rt.anchoredPosition = Vector2.zero;
        var tmp = go.GetComponent<TextMeshProUGUI>();
        tmp.text = text;
        tmp.fontSize = fontSize;
        tmp.color = Color.white;
        tmp.alignment = TextAlignmentOptions.Center;
        return tmp;
    }

    private static Button CreateButton(Transform parent, string name, string label, Vector2 anchor)
    {
        var go = new GameObject(name, typeof(RectTransform), typeof(Image), typeof(Button));
        go.transform.SetParent(parent, false);
        var rt = go.GetComponent<RectTransform>();
        rt.anchorMin = rt.anchorMax = anchor;
        rt.pivot = new Vector2(0.5f, 0.5f);
        rt.sizeDelta = new Vector2(320, 64);
        rt.anchoredPosition = Vector2.zero;
        go.GetComponent<Image>().color = new Color(0.15f, 0.45f, 0.85f, 1f);

        var text = CreateTmp(go.transform, "Text", label, 28, new Vector2(0.5f, 0.5f), new Vector2(300, 50));
        text.raycastTarget = false;

        return go.GetComponent<Button>();
    }

    private static Button CreateListButton(Transform parent, string name, string label)
    {
        var btn = CreateButton(parent, name, label, new Vector2(0.5f, 0.5f));
        var rt = btn.GetComponent<RectTransform>();
        rt.anchorMin = new Vector2(0f, 1f);
        rt.anchorMax = new Vector2(1f, 1f);
        rt.pivot = new Vector2(0.5f, 1f);
        rt.sizeDelta = new Vector2(0f, 72f);

        var le = btn.gameObject.AddComponent<LayoutElement>();
        le.preferredHeight = 72f;
        le.minHeight = 72f;
        le.flexibleWidth = 1f;
        return btn;
    }

    private static LevelCatalogConfig FindOrCreateCatalog()
    {
        const string path = "Assets/Prefabs/Config/Level/LevelCatalog.asset";
        var catalog = AssetDatabase.LoadAssetAtPath<LevelCatalogConfig>(path);
        if (catalog != null) return catalog;

        if (!AssetDatabase.IsValidFolder("Assets/Prefabs/Config/Level"))
        {
            if (!AssetDatabase.IsValidFolder("Assets/Prefabs/Config"))
                AssetDatabase.CreateFolder("Assets/Prefabs", "Config");
            AssetDatabase.CreateFolder("Assets/Prefabs/Config", "Level");
        }

        catalog = ScriptableObject.CreateInstance<LevelCatalogConfig>();
        AssetDatabase.CreateAsset(catalog, path);
        AssetDatabase.SaveAssets();
        SyncCatalogLevels(catalog);
        return catalog;
    }

    private static void SyncCatalogLevels(LevelCatalogConfig catalog)
    {
        if (catalog == null) return;

        catalog.levels.Clear();
        string[] guids = AssetDatabase.FindAssets("t:LevelConfig", new[] { "Assets/Prefabs/Level" });
        var loaded = new List<LevelConfig>(guids.Length);

        foreach (string guid in guids)
        {
            var cfg = AssetDatabase.LoadAssetAtPath<LevelConfig>(AssetDatabase.GUIDToAssetPath(guid));
            if (cfg != null)
                loaded.Add(cfg);
        }

        loaded.Sort((a, b) => a.levelIndex.CompareTo(b.levelIndex));
        catalog.levels.AddRange(loaded);
        EditorUtility.SetDirty(catalog);
        AssetDatabase.SaveAssets();
    }

    private static void BindUIManager(UIManager mgr, LevelCatalogConfig catalog, LevelManager lm,
        CannonController cannon, MainMenuPanel main, LevelSelectPanel select, GameplayHudPanel hud,
        PausePanel pause, LevelResultPanel result)
    {
        var so = new SerializedObject(mgr);
        so.FindProperty("levelCatalog").objectReferenceValue = catalog;
        so.FindProperty("levelManager").objectReferenceValue = lm;
        if (lm != null)
        {
            var lmSo = new SerializedObject(lm);
            lmSo.FindProperty("levelCatalog").objectReferenceValue = catalog;
            lmSo.ApplyModifiedPropertiesWithoutUndo();
        }
        so.FindProperty("cannonController").objectReferenceValue = cannon;
        so.FindProperty("mainMenuPanel").objectReferenceValue = main;
        so.FindProperty("levelSelectPanel").objectReferenceValue = select;
        so.FindProperty("gameplayHudPanel").objectReferenceValue = hud;
        so.FindProperty("pausePanel").objectReferenceValue = pause;
        so.FindProperty("levelResultPanel").objectReferenceValue = result;
        so.ApplyModifiedPropertiesWithoutUndo();
    }

    private static void WireMainMenu(MainMenuPanel panel, Button startBtn)
    {
        var so = new SerializedObject(panel);
        so.FindProperty("startButton").objectReferenceValue = startBtn;
        so.FindProperty("titleText").objectReferenceValue = panel.transform.Find("Title")?.GetComponent<TextMeshProUGUI>();
        so.ApplyModifiedPropertiesWithoutUndo();
    }

    private static void WireLevelSelect(LevelSelectPanel panel, Transform container, Button template, Button back)
    {
        var so = new SerializedObject(panel);
        so.FindProperty("levelButtonContainer").objectReferenceValue = container;
        so.FindProperty("levelButtonTemplate").objectReferenceValue = template;
        so.FindProperty("backButton").objectReferenceValue = back;
        so.FindProperty("titleText").objectReferenceValue = panel.transform.Find("Title")?.GetComponent<TextMeshProUGUI>();
        so.ApplyModifiedPropertiesWithoutUndo();
    }

    private static void WirePause(PausePanel panel, TextMeshProUGUI canon, TextMeshProUGUI bullet,
        Button resume, Button restart, Button menu)
    {
        var so = new SerializedObject(panel);
        so.FindProperty("canonLevelText").objectReferenceValue = canon;
        so.FindProperty("bulletTypeText").objectReferenceValue = bullet;
        so.FindProperty("resumeButton").objectReferenceValue = resume;
        so.FindProperty("restartButton").objectReferenceValue = restart;
        so.FindProperty("mainMenuButton").objectReferenceValue = menu;
        so.ApplyModifiedPropertiesWithoutUndo();
    }

    private static void WireResult(LevelResultPanel panel, TextMeshProUGUI title, TextMeshProUGUI score,
        TextMeshProUGUI detail, Button retry, Button menu)
    {
        var so = new SerializedObject(panel);
        so.FindProperty("titleText").objectReferenceValue = title;
        so.FindProperty("scoreText").objectReferenceValue = score;
        so.FindProperty("detailText").objectReferenceValue = detail;
        so.FindProperty("retryButton").objectReferenceValue = retry;
        so.FindProperty("mainMenuButton").objectReferenceValue = menu;
        so.ApplyModifiedPropertiesWithoutUndo();
    }
}
#endif
