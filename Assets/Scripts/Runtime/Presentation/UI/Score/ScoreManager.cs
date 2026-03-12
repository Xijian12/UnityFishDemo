using System.Collections;
using TMPro;
using UnityEngine;

public class ScoreManager : MonoBehaviour
{
    public static ScoreManager Instance;

    [SerializeField] private TextMeshProUGUI scoreText;
    [SerializeField] private TextMeshProUGUI comboText;
    [SerializeField] private TextMeshProUGUI niceText;
    [SerializeField] private TopDownCameraController topDownCamera;

    private Coroutine comboRoutine;
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

            if (comboRoutine != null)
                StopCoroutine(comboRoutine);

            comboRoutine = StartCoroutine(FadeOutCombo());
        }

        if (comboCount >= 3)
        {
            // 震动相机
            if (topDownCamera != null)
                topDownCamera.PlayShake();

            if (niceText != null)
                StartCoroutine(ShowNice());
        }
    }

    /// <summary>
    /// 淡出combo
    /// </summary>
    /// <returns></returns>
    private IEnumerator FadeOutCombo()
    {
        float duration = 0.8f;
        float timer = 0f;

        Color startColor = comboText.color;
        startColor.a = 1f;
        comboText.color = startColor;

        while (timer < duration)
        {
            timer += Time.deltaTime;
            float t = timer / duration;

            Color c = comboText.color;
            c.a = Mathf.Lerp(1f, 0f, t);
            comboText.color = c;

            yield return null;
        }

        comboText.gameObject.SetActive(false);
    }

    private IEnumerator ShowNice()
    {
        if (niceText == null) yield break;

        niceText.gameObject.SetActive(true);

        float duration = 0.8f;
        float timer = 0f;

        Color startColor = niceText.color;
        startColor.a = 1f;
        niceText.color = startColor;

        while (timer < duration)
        {
            timer += Time.deltaTime;
            float t = timer / duration;

            Color c = niceText.color;
            c.a = Mathf.Lerp(1f, 0f, t);
            niceText.color = c;

            yield return null;
        }

        niceText.gameObject.SetActive(false);
    }

    public void ResetScore()
    {
        totalScore = 0;
        UpdateScoreDisplay(totalScore);
    }

    private void UpdateScoreDisplay(int score)
    {
        if (scoreText != null)
        {
            scoreText.SetText($"{score}");
        }
    }
}