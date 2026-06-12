using TMPro;
using UnityEngine;
using DG.Tweening;

public class ScoreManager : MonoBehaviour
{
    public static ScoreManager Instance;

    [SerializeField] private TextMeshProUGUI scoreText;
    [SerializeField] private TextMeshProUGUI comboText;
    [SerializeField] private TextMeshProUGUI niceText;
    [SerializeField] private TopDownCameraController topDownCamera;

    private ScorePanelView _panelView;
    private Tween comboTween;
    private Tween niceTween;
    private int totalScore;
    private int targetScore;
    private int comboCount;
    private float lastKillTime;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
            return;
        }

        if (topDownCamera == null && Camera.main != null)
            topDownCamera = Camera.main.GetComponent<TopDownCameraController>();

        ResetScore();
    }

    private void OnEnable()
    {
        EventBusClass.Instance.Subscribe<FishKilledEvent>(AddScore);
    }

    private void OnDisable()
    {
        EventBusClass.Instance.Unsubscribe<FishKilledEvent>(AddScore);
    }

    public void BindPanel(ScorePanelView view)
    {
        if (view == null) return;
        _panelView = view;
        ApplyPanelReferences(view);
        UpdateScoreDisplay(totalScore);
    }

    public void UnbindPanel(ScorePanelView view)
    {
        if (_panelView != view) return;
        _panelView = null;
    }

    public void SetTargetScore(int target)
    {
        targetScore = Mathf.Max(0, target);
        UpdateScoreDisplay(totalScore);
    }

    public void AddScore(FishKilledEvent fishKilledEvent)
    {
        totalScore += fishKilledEvent.Score;
        UpdateScoreDisplay(totalScore);

        float time = Time.time;
        comboCount = time - lastKillTime <= 1f ? comboCount + 1 : 1;
        lastKillTime = time;
        UpdateCombo();
    }

    private void ApplyPanelReferences(ScorePanelView view)
    {
        scoreText = view.CurScoreNum ?? scoreText;
        comboText = view.ComboText ?? comboText;
        niceText = view.NiceText ?? niceText;
    }

    private void UpdateCombo()
    {
        if (comboText == null) return;

        if (comboCount > 1)
        {
            comboText.gameObject.SetActive(true);
            comboText.text = comboCount + " COMBO!";
            if (comboTween != null && comboTween.IsActive()) comboTween.Kill(false);
            comboText.alpha = 1f;
            comboTween = comboText
                .DOFade(0f, 0.8f)
                .SetEase(Ease.OutQuad)
                .OnComplete(() => comboText.gameObject.SetActive(false));
        }

        if (comboCount >= 3)
        {
            if (topDownCamera != null)
                topDownCamera.PlayShake();

            if (niceText != null)
            {
                if (niceTween != null && niceTween.IsActive()) niceTween.Kill(false);
                niceText.gameObject.SetActive(true);
                niceText.alpha = 1f;
                niceTween = niceText
                    .DOFade(0f, 0.8f)
                    .SetEase(Ease.OutQuad)
                    .OnComplete(() => niceText.gameObject.SetActive(false));
            }
        }
    }

    public void ResetScore()
    {
        totalScore = 0;
        comboCount = 0;
        UpdateScoreDisplay(totalScore);
    }

    public int GetTotalScore() => totalScore;

    private void UpdateScoreDisplay(int score)
    {
        if (_panelView != null)
        {
            _panelView.SetScoreDisplay(score, targetScore);
            return;
        }

        if (scoreText != null)
            scoreText.SetText(targetScore > 0 ? $"{score}/{targetScore}" : score.ToString());
    }

    private void OnDestroy()
    {
        if (comboTween != null && comboTween.IsActive()) comboTween.Kill(false);
        if (niceTween != null && niceTween.IsActive()) niceTween.Kill(false);
    }
}
