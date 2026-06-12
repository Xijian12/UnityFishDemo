using UnityEngine;
using DG.Tweening;

/// <summary>
/// 单鱼实体：移动、战斗、表现。仅通过对象池与事件总线对外通信，不依赖 FishManager。
/// </summary>
public class Fish : MonoBehaviour, IPoolable
{
    private FishConfig config;
    private FishMovement _fishMovement;
    private FishCombat _fishCombat;
    private FishVisual _fishVisual;
    private bool _externalMovementControl;      // 是否外部控制移动

    private Tween damageTween;
    private Tween dieTween;

    public bool IsDying => _fishCombat != null && _fishCombat.IsDying;
    public bool IsDead => _fishCombat != null && _fishCombat.IsDead;
    public FishMovement Movement => _fishMovement;
    public Transform CachedTransform => transform;
    public FishConfig Config => config;

    #region 初始化

    public void Init(FishConfig config, Vector3 p0, Vector3 p1, Vector3 p2, Vector3 p3, float speedOverride = -1f)
    {
        CacheComponents();
        this.config = config;

        if (!EnsureDependencies())
            return;

        float moveSpeed = speedOverride > 0f ? speedOverride : config.speed;
        _fishMovement.SetPath(p0, p1, p2, p3, moveSpeed);
        _fishCombat.Init(config);
        _fishVisual.Init();
        _fishVisual.Reset(config.spawnScale);
    }

    public void OnSpawn()
    {
        if (!EnsureDependencies())
            return;

        StopRunningTweens();
        _fishCombat?.Reset();
        _fishVisual?.Reset(config != null ? config.spawnScale : (Vector3?)null);

        EventBusClass.Instance.Publish(new FishSpawnedEvent { Fish = this });
    }

    public void OnRecycle()
    {
        EventBusClass.Instance.Publish(new FishReleasedEvent { Fish = this });

        StopRunningTweens();
        _externalMovementControl = false;
        _fishMovement?.Reset();
        _fishCombat?.Reset();
        _fishVisual?.Reset(config != null ? config.spawnScale : (Vector3?)null);
    }

    #endregion

    #region 移动

    public void ManualUpdate(float deltaTime)
    {
        if (config == null || IsDying) return;
        if (_fishMovement == null) return;
        if (_externalMovementControl) return;

        if (!_fishMovement.Tick(deltaTime))
            ReleaseToPool();
    }

    public void SetExternalMovementControl(bool external)
    {
        _externalMovementControl = external;
    }

    #endregion

    #region 受伤与死亡

    public bool TakeDamage(int damage)
    {
        if (_fishCombat == null || _fishVisual == null) return false;

        bool accepted = _fishCombat.TakeDamage(damage);
        if (!accepted) return false;

        PlayDamageFx();

        if (_fishCombat.TryEnterDeath())
        {
            PublishFishKilledEvent();
            PlayDieSequence();
            return true;
        }

        return false;
    }

    /// <summary>
    /// 主动回收到对象池（路径结束、死亡动画完成、关卡清理等）。
    /// </summary>
    public void ReleaseToPool()
    {
        if (!gameObject.activeInHierarchy) return;

        StopRunningTweens();

        if (config != null && config.prefab != null && PoolManager.Instance != null)
            PoolManager.Instance.Release(this, config.prefab);
    }

    #endregion

    #region 内部流程

    private void PlayDamageFx()
    {
        if (_fishVisual == null) return;

        if (damageTween != null && damageTween.IsActive())
            damageTween.Kill(false);

        damageTween = _fishVisual.PlayDamage();
    }

    private void PlayDieSequence()
    {
        if (_fishVisual == null)
        {
            ReleaseToPool();
            return;
        }

        if (dieTween != null && dieTween.IsActive())
            dieTween.Kill(false);

        dieTween = _fishVisual.PlayDie();
        if (dieTween == null)
        {
            ReleaseToPool();
            return;
        }

        dieTween.OnComplete(ReleaseToPool);
    }

    private void StopRunningTweens()
    {
        if (damageTween != null && damageTween.IsActive()) damageTween.Kill(false);
        if (dieTween != null && dieTween.IsActive()) dieTween.Kill(false);
        damageTween = null;
        dieTween = null;
    }

    private bool EnsureDependencies()
    {
        CacheComponents();
        if (_fishMovement == null || _fishCombat == null || _fishVisual == null)
        {
            Debug.LogError("Fish 缺少依赖组件：FishMovement / FishCombat / FishVisual");
            return false;
        }

        return true;
    }

    private void CacheComponents()
    {
        if (_fishMovement == null)
            _fishMovement = GetComponent<FishMovement>();
        if (_fishCombat == null)
            _fishCombat = GetComponent<FishCombat>();
        if (_fishVisual == null)
            _fishVisual = GetComponent<FishVisual>();
    }

    private void PublishFishKilledEvent()
    {
        if (_fishCombat == null) return;

        EventBusClass.Instance.Publish(new FishKilledEvent
        {
            FishType = _fishCombat.GetFishType(),
            Score = _fishCombat.GetScore(),
            Position = transform.position
        });
    }

    #endregion
}
