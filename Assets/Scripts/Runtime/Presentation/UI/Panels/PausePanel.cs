using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 暂停界面：展示炮台等级，提供继续/重开/返回主菜单。
/// </summary>
public class PausePanel : UIPanelBase
{
    [SerializeField] private TextMeshProUGUI canonLevelText;
    [SerializeField] private TextMeshProUGUI bulletTypeText;
    [SerializeField] private Button resumeButton;
    [SerializeField] private Button restartButton;
    [SerializeField] private Button mainMenuButton;

    public event Action OnResumeClicked;
    public event Action OnRestartClicked;
    public event Action OnMainMenuClicked;

    protected override void Awake()
    {
        base.Awake();
        if (resumeButton != null)
            resumeButton.onClick.AddListener(() => OnResumeClicked?.Invoke());
        if (restartButton != null)
            restartButton.onClick.AddListener(() => OnRestartClicked?.Invoke());
        if (mainMenuButton != null)
            mainMenuButton.onClick.AddListener(() => OnMainMenuClicked?.Invoke());
    }

    public void Refresh(CanonType canonType, BulletType bulletType)
    {
        if (canonLevelText != null)
            canonLevelText.SetText(CanonTypeDisplay.GetLevelText(canonType));

        if (bulletTypeText != null)
            bulletTypeText.SetText($"Bullet: {bulletType}");
    }
}
