using TMPro;
using UnityEngine;

/// <summary>
/// 计分板视图（内嵌于 GameplayHudPanel）：分数、Combo、金币飞行目标。
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
        ResolvePanelCanvas();
    }

    private void OnEnable()
    {
        ResolvePanelCanvas();
        ScoreManager.Instance?.BindPanel(this);
        UIFxManager.Instance?.BindScorePanel(this);
    }

    private void OnDisable()
    {
        ScoreManager.Instance?.UnbindPanel(this);
        UIFxManager.Instance?.UnbindScorePanel(this);
    }

    public void SetScoreDisplay(int current, int target)
    {
        if (curScoreNum == null) return;
        curScoreNum.SetText(target > 0 ? $"{current}/{target}" : current.ToString());
    }

    private void ResolvePanelCanvas()
    {
        Canvas canvas = GetComponent<Canvas>();
        if (canvas == null)
            canvas = GetComponentInParent<Canvas>();

        PanelCanvas = canvas != null && canvas.rootCanvas != null ? canvas.rootCanvas : canvas;
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
