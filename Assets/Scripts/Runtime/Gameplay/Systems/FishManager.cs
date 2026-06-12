using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 单鱼调度层：维护活跃列表、驱动 ManualUpdate、转发刷怪请求。
/// 通过事件订阅 Fish 生命周期，Fish 不反向引用本类。
/// </summary>
public class FishManager : MonoBehaviour
{
    public static FishManager Instance { get; private set; }

    [Header("依赖")]
    [SerializeField] private FishSpawner fishSpawner;

    private readonly List<Fish> _activeFish = new();

    /// <summary>只读活跃鱼列表（碰撞检测等只读遍历）。</summary>
    public IReadOnlyList<Fish> ActiveFish => _activeFish;

    public bool IsFishDatabaseReady => fishSpawner != null && fishSpawner.IsReady;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
    }

    private void OnEnable()
    {
        EventBusClass.Instance.Subscribe<FishSpawnedEvent>(OnFishSpawned);
        EventBusClass.Instance.Subscribe<FishReleasedEvent>(OnFishReleased);
    }

    private void OnDisable()
    {
        EventBusClass.Instance.Unsubscribe<FishSpawnedEvent>(OnFishSpawned);
        EventBusClass.Instance.Unsubscribe<FishReleasedEvent>(OnFishReleased);
    }

    private void OnDestroy()
    {
        if (Instance == this)
            Instance = null;
    }

    private void Update()
    {
        for (int i = _activeFish.Count - 1; i >= 0; i--)
        {
            Fish fish = _activeFish[i];
            if (fish == null)
            {
                _activeFish.RemoveAt(i);
                continue;
            }

            fish.ManualUpdate(Time.deltaTime);
        }
    }

    #region 刷怪请求（转发 FishSpawner）

    public void RequestSpawnRandomSingleFish()
    {
        fishSpawner?.SpawnRandomFish();
    }

    public void RequestSpawnSingleFish(FishConfig config)
    {
        fishSpawner?.SpawnFish(config);
    }

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

    #endregion

    #region 查询

    public Fish GetNearestFish(Vector3 fromPosition, float maxDistance = float.MaxValue)
    {
        float maxSq = maxDistance * maxDistance;
        Fish nearest = null;
        float nearestSq = maxSq;

        for (int i = 0; i < _activeFish.Count; i++)
        {
            Fish fish = _activeFish[i];
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

    #endregion

    #region 关卡清理

    /// <summary>回收所有活跃单鱼（关卡切换 / 重开）。</summary>
    public void ClearAllFish()
    {
        if (_activeFish.Count == 0) return;

        var snapshot = new List<Fish>(_activeFish);
        for (int i = 0; i < snapshot.Count; i++)
        {
            Fish fish = snapshot[i];
            if (fish != null && fish.gameObject.activeInHierarchy)
                fish.ReleaseToPool();
        }

        _activeFish.Clear();
    }

    /// <summary>兼容旧命名。</summary>
    public void RestartLevel() => ClearAllFish();

    #endregion

    #region 事件

    private void OnFishSpawned(FishSpawnedEvent e)
    {
        if (e?.Fish == null || _activeFish.Contains(e.Fish)) return;
        _activeFish.Add(e.Fish);
    }

    private void OnFishReleased(FishReleasedEvent e)
    {
        if (e?.Fish == null) return;
        _activeFish.Remove(e.Fish);
    }

    #endregion
}
