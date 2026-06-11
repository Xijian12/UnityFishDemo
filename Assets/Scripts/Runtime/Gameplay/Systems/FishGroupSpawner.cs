using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 仅负责「鱼群」实例的生成：路径采样、对象池准备、驱动 FishGroup.Spawn。
/// 不维护活跃鱼群列表、不参与关卡逻辑；单条鱼实例一律经 FishSpawner 创建。
/// </summary>
public class FishGroupSpawner : MonoBehaviour
{
    [Header("依赖")]
    [SerializeField] private FishSpawner fishSpawner;

    [Header("鱼群活动范围")]
    [SerializeField] private float xMin = -15f;
    [SerializeField] private float xMax = 15f;
    [SerializeField] private float zMin = 5f;
    [SerializeField] private float zMax = 30f;
    [SerializeField, Min(0f)] private float recyclePadding = 8f;

    [Tooltip("贝塞尔控制点横向偏移幅度")]
    [SerializeField, Min(0.1f)] private float curveStrength = 8f;

    private readonly HashSet<GameObject> _preparedPrefabs = new HashSet<GameObject>();

    public FishSpawner SingleFishSpawner => fishSpawner;

    /// <summary>
    /// 为指定鱼群配置创建对象池（同一 prefab 只建一次）。
    /// </summary>
    public void PreparePoolsForConfigs(IEnumerable<FishGroupConfig> configs)
    {
        if (configs == null || PoolManager.Instance == null) return;

        foreach (FishGroupConfig cfg in configs)
        {
            if (cfg == null || cfg.fishConfig == null || cfg.fishConfig.prefab == null) continue;
            EnsurePool(cfg);
        }
    }

    /// <summary>
    /// 生成一队鱼群；成功则 out FishGroup 供 FishGroupManager 纳入调度。
    /// </summary>
    public bool TrySpawnFishGroup(FishGroupConfig cfg, int groupId, out FishGroup group)
    {
        group = null;
        if (cfg == null || cfg.fishConfig == null || cfg.fishConfig.prefab == null || fishSpawner == null)
            return false;

        EnsurePool(cfg);
        BuildGroupCurve(cfg.groupDirection, out Vector3 p0, out Vector3 p1, out Vector3 p2, out Vector3 p3);

        var g = new FishGroup(cfg, groupId);
        if (!g.Spawn(p0, p1, p2, p3, xMin, xMax, zMin, zMax, recyclePadding, fishSpawner))
            return false;

        group = g;
        return true;
    }

    private void EnsurePool(FishGroupConfig cfg)
    {
        if (PoolManager.Instance == null) return;
        GameObject prefab = cfg.fishConfig.prefab;
        if (_preparedPrefabs.Contains(prefab)) return;

        PoolManager.Instance.CreatePool<Fish>(
            prefab,
            cfg.initialPoolSize,
            cfg.maxPoolSize,
            transform
        );
        _preparedPrefabs.Add(prefab);
    }

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
