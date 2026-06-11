using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;

public class FishSpawner : MonoBehaviour
{
    public AssetReferenceT<FishDatabase> fishDatabaseRef;

    [SerializeField] private float spawnInterval = 1f;

    private readonly List<FishConfig> loadedConfigs = new();
    private readonly List<float> cumulativeWeights = new();

    private float timer;
    private bool isInitialized = false;
    [SerializeField, Tooltip("关闭后仅响应关卡/脚本显式调用生成，不自动定时刷鱼")]
    private bool autoSpawnEnabled = true;

    /// <summary>数据库与对象池是否已就绪（关卡系统可等待此条件再刷怪）</summary>
    public bool IsReady => isInitialized;

    public void SetAutoSpawnEnabled(bool enabled) => autoSpawnEnabled = enabled;

    void Start()
    {
        // 启动协程
        StartCoroutine(LoadDatabase());
    }

    // 加载鱼对象的数据库
    IEnumerator LoadDatabase()
    {
        // 1. 发起异步加载
        var handle = fishDatabaseRef.LoadAssetAsync<FishDatabase>();
        yield return handle; // 暂停，直到加载完成

        // 2. 检查加载状态
        if (handle.Status != AsyncOperationStatus.Succeeded)
        {
            Debug.LogError("Failed to load FishDatabase.");
            yield break; // 停止协程
        }

        FishDatabase database = handle.Result; // 获取加载成功的对象

        // 3. 数据校验
        if (database.allFishConfigEntries == null || database.allFishConfigEntries.Count == 0)
        {
            Debug.LogError("FishDatabase is empty!");
            yield break;
        }

        // 4. 构建权重数组
        float totalWeight = 0f;
        foreach (var configEntry in database.allFishConfigEntries)
        {
            if (configEntry == null) continue;
            loadedConfigs.Add(configEntry.config);
            totalWeight += configEntry.spawnWeight;
            cumulativeWeights.Add(totalWeight); // 添加当前权重
        }

        // 5. 创建对象池
        CreatePools();

        // 6. 标记初始化完成
        isInitialized = true;
    }

    // 创建对象池
    void CreatePools()
    {
        foreach (var config in loadedConfigs)
        {
            PoolManager.Instance.CreatePool<Fish>(
                config.prefab,
                initialSize: 50,
                maxSize: 100,
                parent: transform
            );
        }
    }

    // 定时生成一条鱼
    void Update()
    {
        if (!isInitialized || !autoSpawnEnabled) return;

        timer += Time.deltaTime;

        if (timer >= spawnInterval)
        {
            timer = 0f;
            SpawnRandomFish();
        }
    }

    // 根据权重随机选择鱼类型
    void SpawnRandomFish()
    {
        if (loadedConfigs.Count == 0) return;

        float maxWeight = cumulativeWeights[^1];
        float random = Random.Range(0f, maxWeight);

        for (int i = 0; i < cumulativeWeights.Count; i++)
        {
            if (random <= cumulativeWeights[i])
            {
                SpawnFish(loadedConfigs[i]);
                return;
            }
        }
    }

    // 活动范围：X∈[-15,15], Z∈[5,35]，Y 固定为 0（XZ 平面）
    private const float FishXMin = -15f;
    private const float FishXMax = 15f;
    private const float FishZMin = 5f;
    private const float FishZMax = 35f;

    [Tooltip("贝塞尔控制点偏离幅度，越大曲线越弯")]
    [SerializeField] private float curveStrength = 20f;

    /// <summary>
    /// 关卡或外部调用：生成一条指定配置的鱼（须已在 FishDatabase 中并建池）。
    /// </summary>
    public void SpawnFish(FishConfig config)
    {
        Fish fish = PoolManager.Instance.Get<Fish>(config.prefab);
        if (fish == null) return;

        bool leftToRight = Random.value > 0.5f;
        float z = Random.Range(FishZMin, FishZMax);

        Vector3 p0 = leftToRight
            ? new Vector3(FishXMin, 0f, z)
            : new Vector3(FishXMax, 0f, z);
        float endZ = Mathf.Clamp(z + Random.Range(-3f, 3f), FishZMin, FishZMax);
        Vector3 p3 = leftToRight
            ? new Vector3(FishXMax, 0f, endZ)
            : new Vector3(FishXMin, 0f, endZ);

        GetCubicBezierControlPoints(p0, p3, leftToRight, out Vector3 p1, out Vector3 p2);
        fish.Init(config, p0, p1, p2, p3);
    }

    /// <summary>
    /// 鱼群专用：按给定路径生成单条鱼（池化 + Init），不经随机路径。返回实例供鱼群登记。
    /// </summary>
    public Fish SpawnFishOnPath(FishConfig config, Vector3 p0, Vector3 p1, Vector3 p2, Vector3 p3, float speedOverride)
    {
        if (config == null || config.prefab == null || PoolManager.Instance == null) return null;

        Fish fish = PoolManager.Instance.Get<Fish>(config.prefab);
        if (fish == null) return null;

        fish.SetExternalMovementControl(true);
        fish.Init(config, p0, p1, p2, p3, speedOverride);
        return fish;
    }

    /// <summary>
    /// 根据起点、终点和方向生成三阶贝塞尔控制点。
    /// 控制点在 XZ 平面垂直于连线的两侧，形成 S 形或弧线。
    /// </summary>
    private void GetCubicBezierControlPoints(Vector3 p0, Vector3 p3, bool leftToRight, out Vector3 p1, out Vector3 p2)
    {
        Vector3 mid = (p0 + p3) * 0.5f;
        Vector3 dir = (p3 - p0).normalized;
        Vector3 perp = Vector3.Cross(dir, Vector3.up);
        float offset = Random.Range(-curveStrength, curveStrength);

        p1 = mid + perp * offset;
        p1.y = 0f;
        p2 = mid - perp * offset * 0.7f;
        p2.y = 0f;
    }

    // 销毁addressable资源
    private void OnDestroy()
    {
        fishDatabaseRef?.ReleaseAsset();
    }
}