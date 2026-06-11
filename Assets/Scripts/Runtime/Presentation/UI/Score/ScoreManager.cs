using System.Collections;
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

    private Tween comboTween;
    private Tween niceTween;
    private int totalScore = 0;
    private int comboCount = 0;
    private float lastKillTime = 0f;

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

    public void AddScore(FishKilledEvent fishKilledEvent)
    {
        totalScore += fishKilledEvent.Score;
        UpdateScoreDisplay(totalScore);

        float time = Time.time;

        if (time - lastKillTime <= 1f)
            comboCount++;
        else
            comboCount = 1;

        lastKillTime = time;

        UpdateCombo();
    }

    /// <summary>
    /// 更新combo
    /// </summary>
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
            // 震动相机
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
        UpdateScoreDisplay(totalScore);
    }

    /// <summary>当前总分（关卡胜负与 HUD 同步用）</summary>
    public int GetTotalScore() => totalScore;

    private void UpdateScoreDisplay(int score)
    {
        if (scoreText != null)
        {
            scoreText.SetText($"{score}");
        }
    }

    private void OnDestroy()
    {
        if (comboTween != null && comboTween.IsActive()) comboTween.Kill(false);
        if (niceTween != null && niceTween.IsActive()) niceTween.Kill(false);
    }
}