using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 鱼群调度：维护活跃 FishGroup、每帧 Tick。
/// 具体「生成一队鱼」交给 FishGroupSpawner；循环刷怪由 LevelManager 按 LevelConfig 调用。
/// </summary>
public class FishGroupManager : MonoBehaviour
{
    [Header("依赖")]
    [SerializeField] private FishGroupSpawner fishGroupSpawner;

    private readonly List<FishGroup> activeGroups = new List<FishGroup>(8);

    private readonly List<float> cumulativeSpawnWeights = new();
    private float totalSpawnWeight;

    private readonly List<FishGroupConfig> spawnCandidates = new List<FishGroupConfig>(8);
    private readonly Dictionary<FishGroupConfig, int> remainWaves = new Dictionary<FishGroupConfig, int>(8);

    private bool loopSpawnWaves = true;
    private int nextGroupID;

    private void Update()
    {
        for (int i = activeGroups.Count - 1; i >= 0; i--)
        {
            FishGroup group = activeGroups[i];
            if (group == null)
            {
                activeGroups.RemoveAt(i);
                continue;
            }

            group.Tick(Time.deltaTime);
            if (group.IsFinished)
                activeGroups.RemoveAt(i);
        }
    }

    /// <summary>关卡预加载：由 LevelManager 传入关卡内全部鱼群配置。</summary>
    public void PreparePoolsForConfigs(IEnumerable<FishGroupConfig> configs)
    {
        fishGroupSpawner?.PreparePoolsForConfigs(configs);
    }

    /// <summary>
    /// 为 Continuous 刷怪准备候选鱼群池（通常传入关卡波次里出现的全部 FishGroupConfig）。
    /// </summary>
    public void ConfigureContinuousSpawn(IEnumerable<FishGroupConfig> configs, bool loopWaves)
    {
        loopSpawnWaves = loopWaves;
        BuildSpawnPlan(configs);
    }

    public void ClearContinuousSpawnPlan()
    {
        spawnCandidates.Clear();
        remainWaves.Clear();
        cumulativeSpawnWeights.Clear();
        totalSpawnWeight = 0f;
    }

    /// <summary>关卡波次：生成一队指定配置的鱼群。</summary>
    public bool SpawnFishGroup(FishGroupConfig cfg)
    {
        if (fishGroupSpawner == null || cfg == null) return false;

        if (fishGroupSpawner.TrySpawnFishGroup(cfg, nextGroupID++, out FishGroup group))
        {
            activeGroups.Add(group);
            return true;
        }

        return false;
    }

    /// <summary>关卡：一次触发多个鱼群配置。</summary>
    public void SpawnWaveFishGroups(IReadOnlyList<FishGroupConfig> groups)
    {
        if (groups == null) return;
        foreach (FishGroupConfig g in groups)
        {
            if (g != null)
                SpawnFishGroup(g);
        }
    }

    /// <summary>Continuous 模式：从当前候选池随机刷一队鱼群。</summary>
    public bool TrySpawnRandomGroup()
    {
        FishGroupConfig cfg = GetRandomGroupConfig();
        if (cfg == null) return false;

        if (!SpawnFishGroup(cfg))
            return false;

        if (loopSpawnWaves)
        {
            remainWaves[cfg] = cfg.spawnWaveCount;
        }
        else if (remainWaves.TryGetValue(cfg, out int remain))
        {
            remain--;
            remainWaves[cfg] = remain;
            if (remain <= 0)
                spawnCandidates.Remove(cfg);
        }

        return true;
    }

    private FishGroupConfig GetRandomGroupConfig()
    {
        if (spawnCandidates.Count == 0 || totalSpawnWeight <= 0f) return null;

        float random = Random.Range(0f, totalSpawnWeight);
        for (int i = 0; i < cumulativeSpawnWeights.Count; i++)
        {
            if (random <= cumulativeSpawnWeights[i])
                return spawnCandidates[i];
        }

        return null;
    }

    private void BuildSpawnPlan(IEnumerable<FishGroupConfig> configs)
    {
        spawnCandidates.Clear();
        remainWaves.Clear();
        cumulativeSpawnWeights.Clear();
        totalSpawnWeight = 0f;

        if (configs == null) return;

        foreach (FishGroupConfig cfg in configs)
        {
            if (cfg == null || cfg.spawnWaveCount <= 0) continue;
            if (remainWaves.ContainsKey(cfg)) continue;

            totalSpawnWeight += cfg.spawnWeight;
            cumulativeSpawnWeights.Add(totalSpawnWeight);
            remainWaves.Add(cfg, cfg.spawnWaveCount);
            spawnCandidates.Add(cfg);
        }
    }


    public void RestartLevel()
    {
        foreach (FishGroup group in activeGroups)
        {
            group.ReleaseAllMemberFish();
        }
        activeGroups.Clear();
    }
}
