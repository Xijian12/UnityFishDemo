using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 鱼群调度：维护活跃 FishGroup、每帧 Tick、沙盒随机刷群。
/// 具体「生成一队鱼」交给 FishGroupSpawner；不直接 new/Instantiate 鱼，不实现单鱼逻辑。
/// </summary>
public class FishGroupManager : MonoBehaviour
{
    [Header("基础配置")]
    [SerializeField] private bool isLoop = true;

    [Header("依赖")]
    [SerializeField] private FishGroupSpawner fishGroupSpawner;

    [Header("鱼群配置（沙盒随机刷用）")]
    [SerializeField] private List<FishGroupConfig> groupConfigs = new List<FishGroupConfig>();
    [SerializeField, Min(0.1f)] private float spawnInterval = 5f;

    private readonly List<FishGroup> activeGroups = new List<FishGroup>(8);

    private readonly List<float> cumulativeSpawnWeights = new();
    private float totalSpawnWeight;

    private readonly List<FishGroupConfig> spawnCandidates = new List<FishGroupConfig>(8);
    private readonly Dictionary<FishGroupConfig, int> remainWaves = new Dictionary<FishGroupConfig, int>(8);

    private float spawnTimer;
    private int nextGroupID;

    [SerializeField, Tooltip("关闭后不在此组件上自动按间隔刷鱼群")]
    private bool autoSpawnEnabled = true;

    public void SetAutoSpawnEnabled(bool enabled) => autoSpawnEnabled = enabled;

    private void Start()
    {
        BuildSpawnPlan();
        PreparePoolsFromInspectorList();
    }

    private void Update()
    {
        if (autoSpawnEnabled && groupConfigs != null && groupConfigs.Count > 0)
        {
            spawnTimer += Time.deltaTime;
            if (spawnTimer >= spawnInterval)
            {
                spawnTimer = 0f;
                SpawnRandomGroup();
            }
        }

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

    private void PreparePoolsFromInspectorList()
    {
        fishGroupSpawner?.PreparePoolsForConfigs(groupConfigs);
    }

    /// <summary>关卡预加载：由 LevelManager 传入关卡内全部鱼群配置。</summary>
    public void PreparePoolsForConfigs(IEnumerable<FishGroupConfig> configs)
    {
        fishGroupSpawner?.PreparePoolsForConfigs(configs);
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

    private void SpawnRandomGroup()
    {
        FishGroupConfig cfg = GetRandomGroupConfig();
        if (cfg == null) return;

        if (SpawnFishGroup(cfg))
        {
            if (isLoop)
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
        }
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

    private void BuildSpawnPlan()
    {
        spawnCandidates.Clear();
        remainWaves.Clear();
        cumulativeSpawnWeights.Clear();
        totalSpawnWeight = 0f;

        if (groupConfigs == null) return;

        foreach (FishGroupConfig cfg in groupConfigs)
        {
            if (cfg == null || cfg.spawnWaveCount <= 0) continue;
            if (remainWaves.ContainsKey(cfg)) continue;

            totalSpawnWeight += cfg.spawnWeight;
            cumulativeSpawnWeights.Add(totalSpawnWeight);
            remainWaves.Add(cfg, cfg.spawnWaveCount);
            spawnCandidates.Add(cfg);
        }
    }
}
