using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 全局单鱼调度：活跃列表、最近鱼查询、统一 ManualUpdate。
/// 「生成单条鱼」的请求入口转发给 FishSpawner（不在此写路径/池细节）。
/// </summary>
public class FishManager : MonoBehaviour
{
    public static FishManager Instance;

    [Header("依赖")]
    [SerializeField] private FishSpawner fishSpawner;

    public List<Fish> ActiveFish = new();

    /// <summary>供关卡等待 Addressable 数据库就绪。</summary>
    public bool IsFishDatabaseReady => fishSpawner != null && fishSpawner.IsReady;

    /// <summary>由 LevelManager / 模式控制器统一开关定时单鱼刷怪（不走 Find）。</summary>
    public void SetSingleFishAutoSpawnEnabled(bool enabled) =>
        fishSpawner?.SetAutoSpawnEnabled(enabled);

    private void Awake()
    {
        Instance = this;
    }

    public void AddFish(Fish fish)
    {
        ActiveFish.Add(fish);
    }

    public void RemoveFish(Fish fish)
    {
        ActiveFish.Remove(fish);
    }

    /// <summary>
    /// 关卡/外部请求生成一条随机路径的单鱼（具体算法在 FishSpawner）。
    /// </summary>
    public void RequestSpawnSingleFish(FishConfig config)
    {
        fishSpawner?.SpawnFish(config);
    }

    /// <summary>
    /// 关卡波次：按条目批量请求单鱼生成。
    /// </summary>
    public void RequestSpawnSingleFishWave(IReadOnlyList<SingleFishSpawnEntry> entries)
    {
        if (entries == null || fishSpawner == null) return;

        foreach (SingleFishSpawnEntry e in entries)
        {
            if (e?.fishConfig == null || e.count <= 0) continue;

            for (int k = 0; k < e.count; k++)
                fishSpawner.SpawnFish(e.fishConfig);
        }
    }

    public Fish GetNearestFish(Vector3 fromPosition, float maxDistance = float.MaxValue)
    {
        float maxSq = maxDistance * maxDistance;
        Fish nearest = null;
        float nearestSq = maxSq;

        for (int i = 0; i < ActiveFish.Count; i++)
        {
            Fish fish = ActiveFish[i];
            if (fish == null || fish.IsDead) continue;

            float sq = (fish.transform.position - fromPosition).sqrMagnitude;
            if (sq < nearestSq)
            {
                nearestSq = sq;
                nearest = fish;
            }
        }

        return nearest;
    }

    private void Update()
    {
        for (int i = ActiveFish.Count - 1; i >= 0; i--)
        {
            Fish fish = ActiveFish[i];
            if (fish != null)
                fish.ManualUpdate(Time.deltaTime);
        }
    }
}
