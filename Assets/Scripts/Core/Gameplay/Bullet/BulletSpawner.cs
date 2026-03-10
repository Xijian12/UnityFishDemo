using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// 子弹生成器：仅负责从对象池取出子弹并初始化参数。
/// 不处理输入、不计算方向、不控制发射时机。
/// 发射由 CannonController 调用。
/// </summary>
public class BulletSpawner : MonoBehaviour
{
    [Header("子弹配置")]
    [SerializeField] private BulletConfig smallBulletConfig;
    [SerializeField] private BulletConfig mediumBulletConfig;
    [SerializeField] private BulletConfig bigBulletConfig;

    private readonly Dictionary<BulletType, BulletConfig> _configMap = new();

    private void Start()
    {
        BuildConfigMap();
        CreatePools();
    }

    /// <summary>
    /// 发射子弹。由 CannonController 调用，传入出生点、方向、子弹类型。
    /// </summary>
    /// <param name="spawnPos">出生点（通常为炮口位置）</param>
    /// <param name="direction">发射方向（XZ 平面单位向量）</param>
    /// <param name="bulletType">子弹类型</param>
    /// <returns>是否成功发射</returns>
    public bool Fire(Vector3 spawnPos, Vector3 direction, BulletType bulletType)
    {
        if (!_configMap.TryGetValue(bulletType, out BulletConfig config) || config == null)
            return false;

        Bullet bullet = PoolManager.Instance.Get<Bullet>(config.prefab);
        if (bullet == null)
        {
            Debug.LogWarning("BulletSpawner: Failed to get bullet from pool.");
            return false;
        }

        bullet.Init(config, spawnPos, direction);
        return true;
    }

    private void BuildConfigMap()
    {
        _configMap.Clear();
        TryAddConfig(BulletType.SmallBullet, smallBulletConfig);
        TryAddConfig(BulletType.MediumBullet, mediumBulletConfig);
        TryAddConfig(BulletType.BigBullet, bigBulletConfig);
    }

    private void TryAddConfig(BulletType type, BulletConfig config)
    {
        if (config != null)
            _configMap[type] = config;
    }

    private void CreatePools()
    {
        foreach (var kvp in _configMap)
        {
            PoolManager.Instance.CreatePool<Bullet>(
                kvp.Value.prefab,
                initialSize: 10,
                maxSize: 20,
                parent: transform
            );
        }
    }
}
