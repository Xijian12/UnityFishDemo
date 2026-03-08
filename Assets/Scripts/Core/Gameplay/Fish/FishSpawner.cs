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
                initialSize: 500,
                maxSize: 1000,
                parent: transform
            );
        }
    }

    // 定时生成一条鱼
    void Update()
    {
        if (!isInitialized) return;

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

    // 调用fish对象中的生成鱼函数
    void SpawnFish(FishConfig config)
    {
        // 直接从对象池中获取鱼并发射
        Fish fish = PoolManager.Instance.Get<Fish>(config.prefab);
        if (fish == null) return;

        Vector3 startPoint = new(-10f, Random.Range(-4f, 4f), 0f);
        Vector3 endPoint = new(10f, Random.Range(-4f, 4f), 0f);

        fish.Init(config, startPoint, endPoint);
    }

    // 销毁addressable资源
    private void OnDestroy()
    {
        fishDatabaseRef?.ReleaseAsset();
    }
}