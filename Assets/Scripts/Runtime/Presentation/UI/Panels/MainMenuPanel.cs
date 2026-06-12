using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 开始游戏主界面。
/// </summary>
public class MainMenuPanel : UIPanelBase
{
    [SerializeField] private Button startButton;
    [SerializeField] private TextMeshProUGUI titleText;

    public event Action OnStartClicked;

    protected override void Awake()
    {
        base.Awake();
        if (startButton != null)
            startButton.onClick.AddListener(HandleStartClicked);
    }

    private void OnDestroy()
    {
        if (startButton != null)
            startButton.onClick.RemoveListener(HandleStartClicked);
    }

    protected override void OnShow()
    {
        if (titleText != null)
            titleText.SetText("Fish Battle");
    }

    private void HandleStartClicked() => OnStartClicked?.Invoke();
}
