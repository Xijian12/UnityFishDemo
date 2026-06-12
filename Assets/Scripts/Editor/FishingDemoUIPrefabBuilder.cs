#if UNITY_EDITOR
using System.IO;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 将 GameUI 五个界面生成为预制体；GameplayHudPanel 内嵌 ScoreBoard（原 ScorePanel）。
/// </summary>
public static class FishingDemoUIPrefabBuilder
{
    public const string PanelsFolder = "Assets/Prefabs/UI/Panels";
    private const string LegacyScorePanelPath = "Assets/Prefabs/UI/Score/ScorePanel.prefab";

    public const string MainMenuPrefab = "MainMenuPanel.prefab";
    public const string LevelSelectPrefab = "LevelSelectPanel.prefab";
    public const string GameplayHudPrefab = "GameplayHudPanel.prefab";
    public const string PausePrefab = "PausePanel.prefab";
    public const string LevelResultPrefab = "LevelResultPanel.prefab";

    public static string GetPanelPath(string fileName) => $"{PanelsFolder}/{fileName}";

    [MenuItem("FishingDemo/Build UI Panel Prefabs")]
    public static void BuildAllPanelPrefabs()
    {
        EnsureFolder(PanelsFolder);

        var tempRoot = new GameObject("_UIPrefabBuildRoot", typeof(RectTransform));
        var tempRt = tempRoot.GetComponent<RectTransform>();
        FishingDemoUISetup.StretchFullPublic(tempRt);

        try
        {
            SavePanel(CreateMainMenuForBuild(tempRoot.transform), MainMenuPrefab);
            SavePanel(CreateLevelSelectForBuild(tempRoot.transform), LevelSelectPrefab);
            SavePanel(CreateGameplayHudForBuild(tempRoot.transform), GameplayHudPrefab);
            SavePanel(CreatePauseForBuild(tempRoot.transform), PausePrefab);
            SavePanel(CreateResultForBuild(tempRoot.transform), LevelResultPrefab);
        }
        finally
        {
            Object.DestroyImmediate(tempRoot);
        }

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log($"UI panel prefabs saved under {PanelsFolder}. GameplayHudPanel includes embedded ScoreBoard.");
    }

    public static bool AllPanelPrefabsExist()
    {
        return File.Exists(GetPanelPath(MainMenuPrefab))
               && File.Exists(GetPanelPath(LevelSelectPrefab))
               && File.Exists(GetPanelPath(GameplayHudPrefab))
               && File.Exists(GetPanelPath(PausePrefab))
               && File.Exists(GetPanelPath(LevelResultPrefab));
    }

    public static T InstantiatePanel<T>(Transform parent, string prefabFileName, bool active = true) where T : Component
    {
        string path = GetPanelPath(prefabFileName);
        var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
        if (prefab == null)
        {
            Debug.LogError($"FishingDemo UI: prefab not found at {path}. Run FishingDemo/Build UI Panel Prefabs.");
            return null;
        }

        var instance = (GameObject)PrefabUtility.InstantiatePrefab(prefab, parent);
        instance.SetActive(active);
        return instance.GetComponent<T>();
    }

    private static MainMenuPanel CreateMainMenuForBuild(Transform parent) =>
        FishingDemoUISetup.CreateMainMenuPanel(parent);

    private static LevelSelectPanel CreateLevelSelectForBuild(Transform parent) =>
        FishingDemoUISetup.CreateLevelSelectPanel(parent);

    private static GameplayHudPanel CreateGameplayHudForBuild(Transform parent)
    {
        GameplayHudPanel hud = FishingDemoUISetup.CreateGameplayHudPanel(parent);
        IntegrateScoreBoard(hud);
        return hud;
    }

    private static PausePanel CreatePauseForBuild(Transform parent) =>
        FishingDemoUISetup.CreatePausePanelInstance(parent);

    private static LevelResultPanel CreateResultForBuild(Transform parent) =>
        FishingDemoUISetup.CreateLevelResultPanelInstance(parent);

    private static void IntegrateScoreBoard(GameplayHudPanel hud)
    {
        if (hud == null) return;

        var existing = hud.GetComponentInChildren<ScorePanelView>(true);
        if (existing != null)
        {
            FishingDemoUISetup.WireGameplayHud(hud, existing);
            return;
        }

        var scorePrefab = AssetDatabase.LoadAssetAtPath<GameObject>(LegacyScorePanelPath);
        if (scorePrefab == null)
        {
            Debug.LogWarning("ScorePanel.prefab not found; GameplayHudPanel saved without ScoreBoard.");
            return;
        }

        GameObject scoreGo = (GameObject)PrefabUtility.InstantiatePrefab(scorePrefab, hud.transform);
        scoreGo.name = "ScoreBoard";
        PrefabUtility.UnpackPrefabInstance(scoreGo, PrefabUnpackMode.Completely, InteractionMode.AutomatedAction);

        StripNestedCanvasStack(scoreGo);

        var rt = scoreGo.GetComponent<RectTransform>();
        if (rt != null)
        {
            FishingDemoUISetup.StretchFullPublic(rt);
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.localScale = Vector3.one;
        }

        var view = scoreGo.GetComponent<ScorePanelView>();
        if (view == null)
            view = scoreGo.AddComponent<ScorePanelView>();

        FishingDemoUISetup.WireGameplayHud(hud, view);
    }

    private static void StripNestedCanvasStack(GameObject go)
    {
        if (go.TryGetComponent(out Canvas canvas))
            Object.DestroyImmediate(canvas);
        if (go.TryGetComponent(out CanvasScaler scaler))
            Object.DestroyImmediate(scaler);
        if (go.TryGetComponent(out GraphicRaycaster raycaster))
            Object.DestroyImmediate(raycaster);
    }

    private static void SavePanel(Component panel, string fileName)
    {
        if (panel == null) return;

        string path = GetPanelPath(fileName);
        PrefabUtility.SaveAsPrefabAsset(panel.gameObject, path);
    }

    private static void EnsureFolder(string folderPath)
    {
        if (AssetDatabase.IsValidFolder(folderPath)) return;

        const string root = "Assets/Prefabs/UI";
        if (!AssetDatabase.IsValidFolder(root))
        {
            if (!AssetDatabase.IsValidFolder("Assets/Prefabs"))
                AssetDatabase.CreateFolder("Assets", "Prefabs");
            AssetDatabase.CreateFolder("Assets/Prefabs", "UI");
        }

        AssetDatabase.CreateFolder(root, "Panels");
    }
}
#endif
