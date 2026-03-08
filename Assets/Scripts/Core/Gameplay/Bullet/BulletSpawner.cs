using UnityEngine;
using System.Collections.Generic;

public class BulletSpawner : MonoBehaviour
{
    private float lastShotTime;

    [SerializeField] private BulletType currentType;

    // 在 Inspector 中按 BulletType 显示配置
    [SerializeField] private BulletConfig smallBulletConfig;
    [SerializeField] private BulletConfig mediumBulletConfig;
    [SerializeField] private BulletConfig bigBulletConfig;

    // 限制子弹生成的频率
    [SerializeField] private float spawnInterval = 1f;

    // 存放所有不同种类子弹的配置信息
    private readonly Dictionary<BulletType, BulletConfig> configMap = new();
    private readonly List<BulletType> spawnableTypes = new();

    private bool isSpawn = false;

    void Start()
    {
        currentType = BulletType.SmallBullet;
        BuildConfigMap();
        CreatePools();
        BuildSpawnLists();
    }

    void BuildConfigMap()
    {
        configMap.Clear();

        TryAddConfig(BulletType.SmallBullet, smallBulletConfig);
        TryAddConfig(BulletType.MediumBullet, mediumBulletConfig);
        TryAddConfig(BulletType.BigBullet, bigBulletConfig);
    }

    void TryAddConfig(BulletType type, BulletConfig config)
    {
        if (config != null)
        {
            // 确保 BulletConfig 的 bulletType 与字段匹配
            if (config.bulletType != type)
            {
                Debug.LogWarning($"BulletConfig for {type} has mismatched bulletType: {config.bulletType}");
            }
            configMap[type] = config;
        }
    }

    void CreatePools()
    {
        foreach (var kvp in configMap)
        {
            PoolManager.Instance.CreatePool<Bullet>(
                kvp.Value.prefab,
                initialSize: 10,
                maxSize: 20,
                parent: transform
            );
        }
    }

    // 添加所有类型的子弹要数组中
    void BuildSpawnLists()
    {
        spawnableTypes.Clear();

        // 按枚举顺序添加（保证确定性）
        AddToSpawnList(BulletType.SmallBullet, smallBulletConfig);
        AddToSpawnList(BulletType.MediumBullet, mediumBulletConfig);
        AddToSpawnList(BulletType.BigBullet, bigBulletConfig);
    }

    void AddToSpawnList(BulletType type, BulletConfig config)
    {
        if (configMap.ContainsKey(type) && config != null)
        {
            spawnableTypes.Add(type);
        }
    }

    void FixedUpdate()
    {
        if (Input.GetMouseButtonDown(0))
        {
            isSpawn = !isSpawn;
        }

        if (isSpawn && Time.time >= lastShotTime + spawnInterval)
        {
            SpawnTargetBullet();
            lastShotTime = Time.time;
        }
    }

    void SpawnTargetBullet()
    {
        if (spawnableTypes.Count == 0) return;

        for (int i = 0; i < spawnableTypes.Count; i++)
        {
            // 从对象池中选择当前的子弹类型的子弹进行发射
            if (currentType == spawnableTypes[i])
            {
                BulletType type = spawnableTypes[i];
                SpawnBullet(configMap[type]);
                return;
            }
        }
    }

    void SpawnBullet(BulletConfig config)
    {
        // 直接从对象池中获取子弹并发射
        Bullet bullet = PoolManager.Instance.Get<Bullet>(config.prefab);
        if (bullet == null)
        {
            Debug.LogWarning("Failed to get bullet from pool!");
            return;
        }

        Camera cam = Camera.main;
        if (cam == null) return;

        // 鼠标世界位置
        Vector3 mouseWorld = cam.ScreenToWorldPoint(Input.mousePosition);
        mouseWorld.z = 0f;

        // 发射点：屏幕底部中央
        Vector3 spawnPos = GetBottomCenterWorldPosition();

        // 方向：从发射点指向鼠标
        Vector3 dir = mouseWorld - spawnPos;

        // 初始化子弹（内部会设置位置、朝向、激活）
        bullet.Init(config, dir);
    }
    private Vector3 GetBottomCenterWorldPosition()
    {
        Camera cam = Camera.main;
        if (cam == null) return Vector3.zero;

        Vector3 screenPoint = new(Screen.width * 0.5f, 0, 0);
        Vector3 worldPos = cam.ScreenToWorldPoint(screenPoint);
        // 确保在 2D 平面
        worldPos.z = 0;
        return worldPos;
    }
}