using TMPro;
using UnityEngine;

/// <summary>
/// ScorePanel 预制体视图：绑定分数、Combo、金币飞行目标，并向 ScoreManager / UIFxManager 注册。
/// </summary>
[DisallowMultipleComponent]
public class ScorePanelView : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI curScoreNum;
    [SerializeField] private TextMeshProUGUI comboText;
    [SerializeField] private TextMeshProUGUI niceText;
    [SerializeField] private RectTransform coinFlyTarget;

    public Canvas PanelCanvas { get; private set; }
    public TextMeshProUGUI CurScoreNum => curScoreNum;
    public TextMeshProUGUI ComboText => comboText;
    public TextMeshProUGUI NiceText => niceText;
    public RectTransform CoinFlyTarget => coinFlyTarget != null ? coinFlyTarget : curScoreNum != null ? curScoreNum.rectTransform : null;

    private void Awake()
    {
        AutoBindReferences();
        EnsureScoreBoardLayout();
        PanelCanvas = GetComponentInParent<Canvas>();
    }

    private void Start()
    {
        ScoreManager.Instance?.BindPanel(this);
        UIFxManager.Instance?.BindScorePanel(this);
    }

    private void OnDestroy()
    {
        ScoreManager.Instance?.UnbindPanel(this);
        UIFxManager.Instance?.UnbindScorePanel(this);
    }

    public void SetScoreDisplay(int current, int target)
    {
        if (curScoreNum == null) return;
        curScoreNum.SetText(target > 0 ? $"{current}/{target}" : current.ToString());
    }

    private void AutoBindReferences()
    {
        if (curScoreNum == null)
            curScoreNum = transform.Find("CurScoreNum")?.GetComponent<TextMeshProUGUI>();
        if (comboText == null)
            comboText = transform.Find("ComboText")?.GetComponent<TextMeshProUGUI>();
        if (niceText == null)
            niceText = transform.Find("NiceText")?.GetComponent<TextMeshProUGUI>();
        if (coinFlyTarget == null && curScoreNum != null)
            coinFlyTarget = curScoreNum.rectTransform;
    }

    /// <summary>
    /// 作为 GameplayHudPanel 子节点时铺满父级，避免零尺寸导致锚点坍缩。
    /// </summary>
    private void EnsureScoreBoardLayout()
    {
        if (transform.localScale.sqrMagnitude < 0.0001f)
            transform.localScale = Vector3.one;

        if (GetComponent<Canvas>() != null) return;

        var rt = transform as RectTransform;
        if (rt == null) return;

        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = rt.offsetMax = Vector2.zero;
        rt.pivot = new Vector2(0.5f, 0.5f);
    }
}
