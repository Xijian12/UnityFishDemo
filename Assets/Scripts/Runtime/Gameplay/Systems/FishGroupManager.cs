using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 鱼群系统入口：
/// - 负责按配置生成鱼群
/// - 负责 Tick 鱼群生命周期（非鱼个体移动，个体移动仍在 FishManager）
/// </summary>
public class FishGroupManager : MonoBehaviour
{

    [Header("基础配置")]
    [SerializeField] private bool isLoop = true;

    [Header("鱼群配置")]
    [SerializeField] private List<FishGroupConfig> groupConfigs = new List<FishGroupConfig>();
    [SerializeField, Min(0.1f)] private float spawnInterval = 5f;
    [SerializeField, Min(0.1f)] private float curveStrength = 8f;

    [Header("鱼群活动范围")]
    [SerializeField] private float xMin = -15f;
    [SerializeField] private float xMax = 15f;
    [SerializeField] private float zMin = 5f;
    [SerializeField] private float zMax = 30f;
    [SerializeField, Min(0f)] private float recyclePadding = 8f;

    private readonly List<FishGroup> activeGroups = new List<FishGroup>(8);
    private readonly HashSet<GameObject> preparedPools = new HashSet<GameObject>();

    private readonly List<float> cumulativeSpawnWeights = new();        // 累加权重
    private float totalSpawnWeight = 0f;                        // 总权重

    /// <summary>
    /// 生成候选鱼群配置
    /// </summary>
    private readonly List<FishGroupConfig> spawnCandidates = new List<FishGroupConfig>(8);
    /// <summary>
    /// 剩余波次
    /// </summary>
    private readonly Dictionary<FishGroupConfig, int> remainWaves = new Dictionary<FishGroupConfig, int>(8);
    /// <summary>
    /// 生成计时器
    /// </summary>
    private float spawnTimer;
    private int nextGroupID;

    private void Start()
    {
        BuildSpawnPlan();
        PreparePools();
    }

    private void Update()
    {
        if (groupConfigs == null || groupConfigs.Count == 0) return;

        spawnTimer += Time.deltaTime;
        if (spawnTimer >= spawnInterval)
        {
            spawnTimer = 0f;
            SpawnRandomGroup();
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
            {
                activeGroups.RemoveAt(i);
            }
        }
    }

    /// <summary>
    /// 创建对象池
    /// </summary>
    private void PreparePools()
    {
        if (PoolManager.Instance == null) return;

        for (int i = 0; i < groupConfigs.Count; i++)
        {
            FishGroupConfig cfg = groupConfigs[i];
            if (cfg == null || cfg.fishConfig == null || cfg.fishConfig.prefab == null) continue;
            if (preparedPools.Contains(cfg.fishConfig.prefab)) continue;

            PoolManager.Instance.CreatePool<Fish>(
                cfg.fishConfig.prefab,
                cfg.initialPoolSize,
                cfg.maxPoolSize,
                transform
            );
            preparedPools.Add(cfg.fishConfig.prefab);
        }
    }

    /// <summary>
    /// 随机生成一个鱼群
    /// </summary>
    private void SpawnRandomGroup()
    {
        FishGroupConfig cfg = GetRandomGroupConfig();
        if (cfg == null || cfg.fishConfig == null || cfg.fishConfig.prefab == null) return;

        EnsurePool(cfg);

        BuildGroupCurve(cfg.groupDirection, out Vector3 p0, out Vector3 p1, out Vector3 p2, out Vector3 p3);
        FishGroup group = new FishGroup(cfg, nextGroupID++);
        if (group.Spawn(p0, p1, p2, p3, xMin, xMax, zMin, zMax, recyclePadding))
        {
            activeGroups.Add(group);
            if (isLoop)
            {
                remainWaves[cfg] = cfg.spawnWaveCount;
            }
            else
            {
                if (remainWaves.TryGetValue(cfg, out int remain))
                {
                    remain--;
                    remainWaves[cfg] = remain;
                    if (remain <= 0)
                    {
                        spawnCandidates.Remove(cfg);
                    }
                }
            }
        }
    }

    /// <summary>
    /// 随机生成一个鱼群配置
    /// </summary>
    /// <returns></returns>
    private FishGroupConfig GetRandomGroupConfig()
    {
        if (spawnCandidates.Count == 0) return null;
        if (totalSpawnWeight <= 0f) return null;
        float random = Random.Range(0f, totalSpawnWeight);
        for (int i = 0; i < cumulativeSpawnWeights.Count; i++)
        {
            if (random <= cumulativeSpawnWeights[i])
            {
                return spawnCandidates[i];
            }
        }
        return null;
    }

    /// <summary>
    /// 构建生成计划
    /// </summary>
    private void BuildSpawnPlan()
    {
        spawnCandidates.Clear();
        remainWaves.Clear();

        if (groupConfigs == null) return;

        for (int i = 0; i < groupConfigs.Count; i++)
        {
            FishGroupConfig cfg = groupConfigs[i];
            if (cfg == null) continue;
            if (cfg.spawnWaveCount <= 0) continue;

            // 去重，避免同一个配置重复统计
            if (remainWaves.ContainsKey(cfg)) continue;

            totalSpawnWeight += cfg.spawnWeight;
            cumulativeSpawnWeights.Add(totalSpawnWeight);

            remainWaves.Add(cfg, cfg.spawnWaveCount);
            spawnCandidates.Add(cfg);
        }
    }

    /// <summary>
    /// 确保对象池存在
    /// </summary>
    /// <param name="cfg"></param>
    private void EnsurePool(FishGroupConfig cfg)
    {
        if (PoolManager.Instance == null) return;
        if (preparedPools.Contains(cfg.fishConfig.prefab)) return;

        PoolManager.Instance.CreatePool<Fish>(
            cfg.fishConfig.prefab,
            cfg.initialPoolSize,
            cfg.maxPoolSize,
            transform
        );
        preparedPools.Add(cfg.fishConfig.prefab);
    }

    /// <summary>
    /// 构建鱼群曲线
    /// </summary>
    /// <param name="direction"></param>
    /// <param name="p0"></param>
    /// <param name="p1"></param>
    /// <param name="p2"></param>
    /// <param name="p3"></param>
    private void BuildGroupCurve(FishGroupDirection direction, out Vector3 p0, out Vector3 p1, out Vector3 p2, out Vector3 p3)
    {
        float startZ = Random.Range(zMin, zMax);
        float endZ = Mathf.Clamp(startZ + Random.Range(-4f, 4f), zMin, zMax);
        Vector3 dir = DirectionToVector(direction);

        if (Mathf.Abs(dir.x) >= Mathf.Abs(dir.z))
        {
            bool leftToRight = dir.x >= 0f;
            p0 = leftToRight ? new Vector3(xMin, 0f, startZ) : new Vector3(xMax, 0f, startZ);
            p3 = leftToRight ? new Vector3(xMax, 0f, endZ) : new Vector3(xMin, 0f, endZ);
        }
        else
        {
            bool downToUp = dir.z >= 0f;
            float startX = Random.Range(xMin, xMax);
            float endX = Mathf.Clamp(startX + Random.Range(-4f, 4f), xMin, xMax);
            p0 = downToUp ? new Vector3(startX, 0f, zMin) : new Vector3(startX, 0f, zMax);
            p3 = downToUp ? new Vector3(endX, 0f, zMax) : new Vector3(endX, 0f, zMin);
        }

        Vector3 mid = (p0 + p3) * 0.5f;
        Vector3 segDir = (p3 - p0).normalized;
        Vector3 perp = Vector3.Cross(segDir, Vector3.up);
        float offset = Random.Range(-curveStrength, curveStrength);

        p1 = mid + perp * offset;
        p2 = mid - perp * offset * 0.7f;
        p1.y = 0f;
        p2.y = 0f;
    }

    /// <summary>
    /// 方向转换为向量
    /// </summary>
    /// <param name="direction"></param>
    /// <returns></returns>
    private static Vector3 DirectionToVector(FishGroupDirection direction)
    {
        switch (direction)
        {
            case FishGroupDirection.LeftToRight: return Vector3.right;
            case FishGroupDirection.RightToLeft: return Vector3.left;
            case FishGroupDirection.UpToDown: return Vector3.back;
            case FishGroupDirection.DownToUp: return Vector3.forward;
            case FishGroupDirection.LeftUpToRightDown: return new Vector3(1f, 0f, -1f).normalized;
            case FishGroupDirection.RightUpToLeftDown: return new Vector3(-1f, 0f, -1f).normalized;
            case FishGroupDirection.LeftDownToRightUp: return new Vector3(1f, 0f, 1f).normalized;
            case FishGroupDirection.RightDownToLeftUp: return new Vector3(-1f, 0f, 1f).normalized;
            default: return Vector3.right;
        }
    }
}
