using System.Collections;
using System.Collections.Generic;
using System.Text;
using TMPro;
using UnityEngine;

/// <summary>
/// 仅负责关卡流程：计时、分数同步、胜负、波次时刻。
/// 不直接调用 FishSpawner / FishGroupSpawner；刷怪请求经 FishManager、FishGroupManager。
/// </summary>
public class LevelManager : MonoBehaviour
{
    [Header("关卡")]
    [SerializeField] private LevelConfig levelConfig;
    [Tooltip("进入场景后自动开始关卡")]
    [SerializeField] private bool runLevelOnStart = true;

    [Header("引用（须在 Inspector 绑定，禁止使用 Find）")]
    [SerializeField] private FishManager fishManager;
    [SerializeField] private FishGroupManager fishGroupManager;
    [SerializeField] private FishSpawnModeController spawnModeController;
    [SerializeField] private CannonController cannonController;

    [Header("可选 UI")]
    [SerializeField] private TextMeshProUGUI timerText;
    [SerializeField] private TextMeshProUGUI targetScoreText;

    private readonly LevelRuntime _runtime = new LevelRuntime();
    private readonly HashSet<int> _wavesTriggered = new HashSet<int>();
    private readonly StringBuilder _uiStringBuilder = new StringBuilder();

    public LevelRuntime Runtime => _runtime;
    public LevelConfig ActiveConfig => levelConfig;

    public event System.Action<LevelState> OnLevelStateChanged;

    private void Awake()
    {
        if (runLevelOnStart && levelConfig != null)
            DisableAutoSpawners();
    }

    private void Start()
    {
        if (runLevelOnStart && levelConfig != null)
            StartCoroutine(StartLevelWhenReady());
    }

    private IEnumerator StartLevelWhenReady()
    {
        bool needFishDb = levelConfig != null &&
                          levelConfig.levelSpawnMode == FishSpawnMode.SingleFish;

        if (needFishDb && fishManager != null)
        {
            float wait = 0f;
            const float timeout = 60f;
            while (!fishManager.IsFishDatabaseReady && wait < timeout)
            {
                wait += Time.unscaledDeltaTime;
                yield return null;
            }

            if (!fishManager.IsFishDatabaseReady)
                Debug.LogError("LevelManager: FishSpawner 数据库未就绪（检查 FishManager→FishSpawner、Addressables）。");
        }

        BeginLevel(levelConfig);
    }

    public void BeginLevel(LevelConfig config)
    {
        if (config == null)
        {
            Debug.LogWarning("LevelManager.BeginLevel: config is null.");
            return;
        }

        levelConfig = config;
        _wavesTriggered.Clear();

        ScoreManager.Instance?.ResetScore();

        _runtime.Init(config);
        _runtime.currentLevelState = LevelState.Running;

        spawnModeController?.SetMode(config.levelSpawnMode);
        DisableAutoSpawners();

        if (fishGroupManager != null)
            fishGroupManager.PreparePoolsForConfigs(CollectAllFishGroupConfigs(config));

        cannonController?.SetBulletType(config.initialBulletType);

        UpdateOptionalHud();
        OnLevelStateChanged?.Invoke(LevelState.Running);
    }

    public void RestartLevel()
    {
        if (levelConfig != null)
            BeginLevel(levelConfig);
    }

    private void Update()
    {
        if (_runtime.currentLevelState != LevelState.Running || levelConfig == null)
            return;

        _runtime.currentTime += Time.deltaTime;

        if (ScoreManager.Instance != null)
            _runtime.currentScore = ScoreManager.Instance.GetTotalScore();

        TryTriggerWaves();
        CheckWinLose();
        UpdateOptionalHud();
    }

    private void DisableAutoSpawners()
    {
        fishManager?.SetSingleFishAutoSpawnEnabled(false);
        fishGroupManager?.SetAutoSpawnEnabled(false);
    }

    private void TryTriggerWaves()
    {
        if (levelConfig.waveConfigs == null || levelConfig.waveConfigs.Count == 0)
            return;

        for (int i = 0; i < levelConfig.waveConfigs.Count; i++)
        {
            if (_wavesTriggered.Contains(i))
                continue;

            WaveConfig w = levelConfig.waveConfigs[i];
            if (w == null)
                continue;

            if (_runtime.currentTime < w.startTime)
                continue;

            _wavesTriggered.Add(i);
            DispatchWave(w);
            _runtime.currentWaveIndex = i;
        }
    }

    /// <summary>仅分发请求，不包含路径/池实现。</summary>
    private void DispatchWave(WaveConfig wave)
    {
        if (levelConfig == null) return;

        if (levelConfig.levelSpawnMode == FishSpawnMode.FishGroup)
            fishGroupManager?.SpawnWaveFishGroups(wave.fishGroups);
        else
            fishManager?.RequestSpawnSingleFishWave(wave.singleFishEntries);
    }

    private void CheckWinLose()
    {
        if (levelConfig.levelTargetScore > 0 && _runtime.currentScore >= levelConfig.levelTargetScore)
        {
            EndLevel(LevelState.Succeeded);
            return;
        }

        if (_runtime.currentTime >= levelConfig.levelTime)
        {
            bool met = levelConfig.levelTargetScore <= 0 || _runtime.currentScore >= levelConfig.levelTargetScore;
            EndLevel(met ? LevelState.Succeeded : LevelState.Failed);
        }
    }

    private void EndLevel(LevelState endState)
    {
        if (_runtime.currentLevelState == endState) return;

        _runtime.currentLevelState = endState;
        DisableAutoSpawners();
        OnLevelStateChanged?.Invoke(endState);
    }

    public float GetRemainingTime()
    {
        if (levelConfig == null || _runtime.currentLevelState != LevelState.Running)
            return 0f;
        return Mathf.Max(0f, levelConfig.levelTime - _runtime.currentTime);
    }

    private void UpdateOptionalHud()
    {
        if (timerText != null && levelConfig != null && _runtime.currentLevelState == LevelState.Running)
        {
            _uiStringBuilder.Clear();
            _uiStringBuilder.AppendFormat("{0:F1}s / {1:F0}s", GetRemainingTime(), levelConfig.levelTime);
            timerText.SetText(_uiStringBuilder.ToString());
        }

        if (targetScoreText != null && levelConfig != null)
        {
            _uiStringBuilder.Clear();
            _uiStringBuilder.AppendFormat("{0} / {1}", _runtime.currentScore, levelConfig.levelTargetScore);
            targetScoreText.SetText(_uiStringBuilder.ToString());
        }
    }

    private static List<FishGroupConfig> CollectAllFishGroupConfigs(LevelConfig cfg)
    {
        var list = new List<FishGroupConfig>();
        if (cfg.waveConfigs == null) return list;

        foreach (WaveConfig w in cfg.waveConfigs)
        {
            if (w?.fishGroups == null) continue;

            foreach (FishGroupConfig g in w.fishGroups)
            {
                if (g != null && !list.Contains(g))
                    list.Add(g);
            }
        }

        return list;
    }
}
