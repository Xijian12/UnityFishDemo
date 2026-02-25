using TMPro;
using UnityEngine;

public class ScoreManager : MonoBehaviour
{
    public static ScoreManager Instance;

    // 拖入 Inspector
    [SerializeField] private TextMeshProUGUI scoreText;

    private int totalScore = 0;

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

        ResetScore();
    }

    // 订阅鱼死亡的事件
    private void OnEnable()
    {
        Fish.OnFishKilled += AddScore;
    }

    // 取消订阅鱼死亡的事件
    private void OnDisable()
    {
        Fish.OnFishKilled -= AddScore;
    }

    public void AddScore(int points)
    {
        totalScore += points;
        UpdateScoreDisplay();
    }

    public void ResetScore()
    {
        totalScore = 0;
        UpdateScoreDisplay();
    }

    // 只在OnFishKilled事件触发时才调用，对计分板UI进行修改
    private void UpdateScoreDisplay()
    {
        if (scoreText != null)
        {
            scoreText.SetText($"{totalScore}");
        }
    }
}