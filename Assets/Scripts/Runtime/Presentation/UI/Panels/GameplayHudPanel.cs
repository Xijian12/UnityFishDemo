using System.Text;
using TMPro;
using UnityEngine;

/// <summary>
/// 战斗 HUD：关卡标题、倒计时。分数由 ScorePanel 预制体负责。
/// </summary>
public class GameplayHudPanel : UIPanelBase
{
    protected override bool ShouldCreateRaycastBlocker => false;

    [SerializeField] private TextMeshProUGUI timerText;
    [SerializeField] private TextMeshProUGUI levelTitleText;
    [SerializeField] private ScorePanelView scorePanel;

    private readonly StringBuilder _sb = new StringBuilder(32);

    public ScorePanelView ScorePanel => scorePanel;

    protected override void Awake()
    {
        base.Awake();
        if (scorePanel == null)
            scorePanel = GetComponentInChildren<ScorePanelView>(true);
    }

    public void BindLevel(LevelConfig config)
    {
        if (levelTitleText == null || config == null) return;
        levelTitleText.SetText($"{config.GetDisplayTitle()}  {config.GetHudSubtitle()}");
    }

    public void Refresh(LevelRuntime runtime, float remainingTime, LevelConfig config)
    {
        if (runtime == null || config == null) return;

        if (timerText != null)
        {
            _sb.Clear();
            _sb.AppendFormat("{0:F0}", Mathf.CeilToInt(remainingTime));
            timerText.SetText(_sb.ToString());
        }
    }
}
