using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 关卡结算界面（胜利 / 失败）。
/// </summary>
public class LevelResultPanel : UIPanelBase
{
    [SerializeField] private TextMeshProUGUI titleText;
    [SerializeField] private TextMeshProUGUI scoreText;
    [SerializeField] private TextMeshProUGUI detailText;
    [SerializeField] private Button retryButton;
    [SerializeField] private Button mainMenuButton;

    public event Action OnRetryClicked;
    public event Action OnMainMenuClicked;

    protected override void Awake()
    {
        base.Awake();
        if (retryButton != null)
            retryButton.onClick.AddListener(() => OnRetryClicked?.Invoke());
        if (mainMenuButton != null)
            mainMenuButton.onClick.AddListener(() => OnMainMenuClicked?.Invoke());
    }

    public void ShowResult(LevelState state, LevelRuntime runtime, LevelConfig config)
    {
        bool success = state == LevelState.Succeeded;

        if (titleText != null)
            titleText.SetText(success ? "Victory!" : "Defeat");

        if (scoreText != null && runtime != null)
            scoreText.SetText($"Score: {runtime.currentScore}");

        if (detailText != null && config != null)
        {
            detailText.SetText(success
                ? $"{config.GetDisplayTitle()} ({config.levelType}) — Target {config.levelTargetScore} pts reached"
                : $"{config.GetDisplayTitle()} ({config.levelType}) — Target {config.levelTargetScore} pts, try again");
        }

        Show();
    }
}
