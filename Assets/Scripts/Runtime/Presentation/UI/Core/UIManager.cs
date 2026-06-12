using System;
using UnityEngine;

/// <summary>
/// UI 总控：主菜单 → 关卡选择 → 战斗 HUD / 暂停 / 结算。
/// 实现 <see cref="ILevelUIService"/>，为关卡系统提供统一 UI 入口。
/// </summary>
[DefaultExecutionOrder(-50)]
public class UIManager : MonoBehaviour, ILevelUIService
{
    public static UIManager Instance { get; private set; }

    [Header("数据")]
    [SerializeField] private LevelCatalogConfig levelCatalog;

    [Header("玩法引用（Inspector 绑定）")]
    [SerializeField] private LevelManager levelManager;
    [SerializeField] private CannonController cannonController;

    [Header("面板")]
    [SerializeField] private MainMenuPanel mainMenuPanel;
    [SerializeField] private LevelSelectPanel levelSelectPanel;
    [SerializeField] private GameplayHudPanel gameplayHudPanel;
    [SerializeField] private PausePanel pausePanel;
    [SerializeField] private LevelResultPanel levelResultPanel;

    [Header("输入")]
    [SerializeField] private KeyCode pauseKey = KeyCode.Escape;

    private bool _inGameplay;

    /// <summary>主菜单 / 选关 / 暂停 / 结算显示时为 true，玩法输入应被拦截。</summary>
    public bool IsBlockingGameplayInput =>
        (mainMenuPanel != null && mainMenuPanel.IsVisible) ||
        (levelSelectPanel != null && levelSelectPanel.IsVisible) ||
        (pausePanel != null && pausePanel.IsVisible) ||
        (levelResultPanel != null && levelResultPanel.IsVisible);

    public event Action<LevelConfig> OnLevelPresentationStarted;
    public event Action<LevelState, LevelRuntime> OnLevelPresentationStateChanged;
    public event Action<LevelRuntime, float> OnHudRefresh;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        WirePanelEvents();
        BindLevelManager();
        SyncLevelCatalog();
        EnsureCanvasOnTop();
    }

    private void Start()
    {
        ShowMainMenu();
    }

    private void OnDestroy()
    {
        UnbindLevelManager();

        if (Instance == this)
            Instance = null;
    }

    private void Update()
    {
        if (!_inGameplay || levelManager == null) return;

        LevelState state = levelManager.Runtime.currentLevelState;
        if (state != LevelState.Running && state != LevelState.Paused) return;

        if (Input.GetKeyDown(pauseKey))
        {
            if (state == LevelState.Running)
                RequestPause();
            else if (state == LevelState.Paused)
                RequestResume();
        }
    }

    #region ILevelUIService

    public void ShowMainMenu()
    {
        _inGameplay = false;
        Time.timeScale = 1f;
        levelManager?.SuppressAutoSpawning();
        cannonController?.StopManualFiring();

        HideAllPanels();
        mainMenuPanel?.Show();
        BringGameUiToFront();
    }

    public void ShowLevelSelect()
    {
        HideAllPanels();
        cannonController?.StopManualFiring();
        levelSelectPanel?.BindCatalog(levelCatalog);
        levelSelectPanel?.Show();
        BringGameUiToFront();
    }

    public void RequestStartLevel(LevelConfig config)
    {
        if (config == null || levelManager == null) return;

        if (levelCatalog != null && !levelCatalog.Contains(config))
        {
            Debug.LogWarning($"UIManager: 关卡 '{config.name}' 未注册到 LevelCatalog。");
            return;
        }

        HideAllPanels();
        levelResultPanel?.Hide();
        gameplayHudPanel?.Show();
        gameplayHudPanel?.BindLevel(config);

        _inGameplay = true;
        if (!levelManager.RequestBeginLevel(config))
            _inGameplay = false;
    }

    public void RequestPause()
    {
        if (levelManager == null) return;
        if (levelManager.Runtime.currentLevelState != LevelState.Running) return;

        levelManager.PauseLevel();
        pausePanel?.Refresh(
            cannonController != null ? cannonController.CurrentCanonType : levelManager.Runtime.currentCanonType,
            levelManager.Runtime.currentBulletType);
        pausePanel?.Show();
    }

    public void RequestResume()
    {
        if (levelManager == null) return;
        if (levelManager.Runtime.currentLevelState != LevelState.Paused) return;

        levelManager.ResumeLevel();
        pausePanel?.Hide();
    }

    public void RequestRestart()
    {
        pausePanel?.Hide();
        levelResultPanel?.Hide();
        levelManager?.RestartLevel();
        _inGameplay = true;
        gameplayHudPanel?.Show();
    }

    public void ReturnToMainMenu()
    {
        pausePanel?.Hide();
        levelResultPanel?.Hide();
        levelManager?.AbortLevel();
        ShowMainMenu();
    }

    #endregion

    private void WirePanelEvents()
    {
        if (mainMenuPanel != null)
            mainMenuPanel.OnStartClicked += ShowLevelSelect;

        if (levelSelectPanel != null)
        {
            levelSelectPanel.OnBackClicked += ShowMainMenu;
            levelSelectPanel.OnLevelSelected += RequestStartLevel;
        }

        if (pausePanel != null)
        {
            pausePanel.OnResumeClicked += RequestResume;
            pausePanel.OnRestartClicked += RequestRestart;
            pausePanel.OnMainMenuClicked += ReturnToMainMenu;
        }

        if (levelResultPanel != null)
        {
            levelResultPanel.OnRetryClicked += RequestRestart;
            levelResultPanel.OnMainMenuClicked += ReturnToMainMenu;
        }
    }

    private void BindLevelManager()
    {
        if (levelManager == null) return;

        levelManager.OnLevelStateChanged += HandleLevelStateChanged;
        levelManager.OnHudUpdate += HandleHudUpdate;
    }

    private void SyncLevelCatalog()
    {
        if (levelManager == null) return;

        levelManager.SetLevelCatalog(levelCatalog);

        if (levelCatalog == null || levelCatalog.Count == 0)
            Debug.LogWarning("UIManager: LevelCatalog 为空，请在 Inspector 绑定 LevelCatalogConfig。");
    }

    private void UnbindLevelManager()
    {
        if (levelManager == null) return;

        levelManager.OnLevelStateChanged -= HandleLevelStateChanged;
        levelManager.OnHudUpdate -= HandleHudUpdate;
    }

    private void HandleLevelStateChanged(LevelState state)
    {
        if (levelManager == null) return;

        LevelRuntime runtime = levelManager.Runtime;
        LevelConfig config = levelManager.ActiveConfig;

        OnLevelPresentationStateChanged?.Invoke(state, runtime);

        switch (state)
        {
            case LevelState.Running:
                OnLevelPresentationStarted?.Invoke(config);
                pausePanel?.Hide();
                break;

            case LevelState.Succeeded:
            case LevelState.Failed:
                _inGameplay = false;
                pausePanel?.Hide();
                gameplayHudPanel?.Hide();
                levelResultPanel?.ShowResult(state, runtime, config);
                break;
        }
    }

    private void HandleHudUpdate(LevelRuntime runtime, float remainingTime)
    {
        if (levelManager == null || gameplayHudPanel == null) return;

        gameplayHudPanel.Refresh(runtime, remainingTime, levelManager.ActiveConfig);
        OnHudRefresh?.Invoke(runtime, remainingTime);
    }

    private void HideAllPanels()
    {
        mainMenuPanel?.Hide();
        levelSelectPanel?.Hide();
        gameplayHudPanel?.Hide();
        pausePanel?.Hide();
    }

    private void EnsureCanvasOnTop()
    {
        Canvas parentCanvas = GetComponentInParent<Canvas>();
        if (parentCanvas != null)
            parentCanvas.sortingOrder = 100;

        BringGameUiToFront();
    }

    private void BringGameUiToFront()
    {
        transform.SetAsLastSibling();
    }
}
