using System.Collections;
using UnityEngine;

public class Fish : MonoBehaviour, IPoolable
{
    private FishConfig config;
    private FishMovement _fishMovement;
    private FishCombat _fishCombat;
    private FishVisual _fishVisual;

    private Coroutine damageRoutine;
    private Coroutine dieRoutine;

    /// <summary>
    /// 是否正在死亡
    /// </summary>
    public bool IsDying => _fishCombat != null && _fishCombat.IsDying;
    /// <summary>
    /// 是否已死亡,Fish.IsDead 每次被访问时，都实时去读取 _fishCombat.IsDead 当前值，而不是在 Fish 里再存一份 bool
    /// </summary>
    public bool IsDead => _fishCombat != null && _fishCombat.IsDead;

    private void Awake()
    {
        CacheComponents();
    }

    #region 初始化

    public void Init(FishConfig config, Vector3 p0, Vector3 p1, Vector3 p2, Vector3 p3, float startDelay = 0f)
    {
        this.config = config;

        if (!EnsureDependencies())
        {
            return;
        }

        _fishMovement.SetPath(p0, p1, p2, p3, config.speed, startDelay);
        _fishCombat.Init(config);
        _fishVisual.Init();
        _fishVisual.Reset();
    }

    public void OnSpawn()
    {
        if (!EnsureDependencies())
        {
            return;
        }

        StopRunningCoroutines();
        // 防御式重置：即使上次回收中断，这里也确保状态干净
        _fishCombat?.Reset();
        _fishVisual?.Reset();

        FishManager.Instance?.AddFish(this);
    }

    public void OnRecycle()
    {
        StopRunningCoroutines();

        FishManager.Instance?.RemoveFish(this);
        _fishMovement?.Reset();
        _fishCombat?.Reset();
        _fishVisual?.Reset();
    }

    #endregion

    #region 移动

    public void ManualUpdate(float deltaTime)
    {
        if (config == null || IsDying) return;
        if (_fishMovement == null) return;

        if (!_fishMovement.Tick(deltaTime))
        {
            SafeRelease();
        }
    }

    #endregion

    #region 受伤

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

    #endregion

    #region 死亡

    private void SafeRelease()
    {
        if (!gameObject.activeInHierarchy) return;

        StopRunningCoroutines();

        if (config != null && config.prefab != null && PoolManager.Instance != null)
        {
            PoolManager.Instance.Release(this, config.prefab);
        }
    }

    #endregion

    #region 内部流程

    private void PlayDamageFx()
    {
        if (damageRoutine != null)
        {
            StopCoroutine(damageRoutine);
        }

        damageRoutine = StartCoroutine(DamageFxRoutine());
    }

    private IEnumerator DamageFxRoutine()
    {
        if (_fishVisual != null)
        {
            yield return _fishVisual.PlayDamage();
        }

        damageRoutine = null;
    }

    private void PlayDieSequence()
    {
        if (dieRoutine != null)
        {
            StopCoroutine(dieRoutine);
        }

        dieRoutine = StartCoroutine(DieAndRecycleRoutine());
    }

    private IEnumerator DieAndRecycleRoutine()
    {
        if (_fishVisual != null)
        {
            yield return _fishVisual.PlayDie();
        }

        dieRoutine = null;
        SafeRelease();
    }

    private void StopRunningCoroutines()
    {
        StopAllCoroutines();
        damageRoutine = null;
        dieRoutine = null;
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

    /// <summary>
    /// 缓存组件
    /// </summary>
    private void CacheComponents()
    {
        if (_fishMovement == null)
            _fishMovement = GetComponent<FishMovement>();
        if (_fishCombat == null)
            _fishCombat = GetComponent<FishCombat>();
        if (_fishVisual == null)
            _fishVisual = GetComponent<FishVisual>();
    }

    /// <summary>
    /// 发布鱼死亡事件
    /// </summary>
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