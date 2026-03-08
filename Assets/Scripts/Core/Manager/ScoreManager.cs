using System.Collections;
using TMPro;
using UnityEngine;

public class ScoreManager : MonoBehaviour
{
    public static ScoreManager Instance;

    [SerializeField] private TextMeshProUGUI scoreText;
    [SerializeField] private TextMeshProUGUI comboText;
    [SerializeField] private TextMeshProUGUI niceText;
    [SerializeField] private Camera mainCamera;

    [SerializeField] private GameObject coinPrefab;
    [SerializeField] private Canvas canvas;

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

        if (mainCamera == null)
            mainCamera = Camera.main;

        ResetScore();
    }

    private void OnEnable()
    {
        Fish.OnFishKilled += AddScore;
    }

    private void OnDisable()
    {
        Fish.OnFishKilled -= AddScore;
    }

    public void AddScore(int points)
    {
        totalScore += points;
        UpdateScoreDisplay();

        float time = Time.time;

        if (time - lastKillTime <= 1f)
            comboCount++;
        else
            comboCount = 1;

        lastKillTime = time;

        UpdateCombo();
    }

    public void SpawnCoinFromWorld(Vector3 worldPos)
    {
        if (coinPrefab == null || scoreText == null || canvas == null)
        {
            Debug.LogError("ScoreManager引用未设置！");
            return;
        }

        Camera cam = canvas.renderMode == RenderMode.ScreenSpaceOverlay
            ? null
            : canvas.worldCamera;

        Vector3 screenPos = Camera.main.WorldToScreenPoint(worldPos);

        RectTransform canvasRect = canvas.GetComponent<RectTransform>();

        Vector2 uiStartPos;
        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            canvasRect,
            screenPos,
            cam,
            out uiStartPos
        );

        RectTransform scoreRect = scoreText.GetComponent<RectTransform>();

        Vector2 uiTargetPos;
        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            canvasRect,
            scoreRect.position,
            cam,
            out uiTargetPos
        );

        GameObject coin = Instantiate(coinPrefab, canvas.transform);

        CoinUIFx fx = coin.GetComponent<CoinUIFx>();

        if (fx == null)
        {
            Debug.LogError("CoinPrefab上没有CoinUIFx脚本！");
            return;
        }

        fx.Init(uiStartPos, uiTargetPos);
    }

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
            if (mainCamera != null)
                StartCoroutine(ScreenShake());

            if (niceText != null)
                StartCoroutine(ShowNice());
        }
    }

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

    private IEnumerator ScreenShake()
    {
        Vector3 originalPos = mainCamera.transform.position;

        float duration = 0.2f;
        float magnitude = 0.1f;
        float timer = 0f;

        while (timer < duration)
        {
            timer += Time.deltaTime;

            float offsetX = Random.Range(-1f, 1f) * magnitude;
            float offsetY = Random.Range(-1f, 1f) * magnitude;

            mainCamera.transform.position = new Vector3(
                originalPos.x + offsetX,
                originalPos.y + offsetY,
                originalPos.z
            );

            yield return null;
        }

        mainCamera.transform.position = originalPos;
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
        UpdateScoreDisplay();
    }

    private void UpdateScoreDisplay()
    {
        if (scoreText != null)
        {
            scoreText.SetText($"{totalScore}");
        }
    }
}