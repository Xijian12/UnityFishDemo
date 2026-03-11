using UnityEngine;

public class Bullet : MonoBehaviour, IPoolable
{
    public static event System.Action<Bullet> OnBulletSpawn;
    public static event System.Action<Bullet> OnBulletRecycle;

    private BulletConfig config;
    private Vector3 direction;
    private Vector3 startPos;

    /// <summary>
    /// 从炮口位置沿给定方向发射。
    /// 方向通常在 XZ 平面（Y=0），由 CannonController 计算。
    /// </summary>
    public void Init(BulletConfig config, Vector3 spawnPos, Vector3 dir)
    {
        this.config = config ?? throw new System.ArgumentNullException(nameof(config));
        this.direction = dir.sqrMagnitude > 0.0001f ? dir.normalized : Vector3.forward;
        this.startPos = spawnPos;

        transform.position = spawnPos;
        transform.forward = this.direction;

        gameObject.SetActive(true);
    }

    public void ManualUpdate(float deltaTime)
    {
        if (config == null)
        {
            gameObject.SetActive(false);
            return;
        }

        // 移动
        transform.position += config.speed * deltaTime * direction;

        // 超出最大距离
        if (Vector3.Distance(startPos, transform.position) > config.maxDistance)
        {
            PoolManager.Instance.Release(this, config.prefab);
            return;
        }

        //碰撞检测
        CheckHit();
    }

    private const float HitRadius = 0.8f;
    private const float HitRadiusSq = HitRadius * HitRadius;

    void CheckHit()
    {
        var activeFish = FishManager.Instance?.ActiveFish;
        if (activeFish == null) return;

        Vector3 bulletPos = transform.position;

        for (int i = 0; i < activeFish.Count; i++)
        {
            Fish fish = activeFish[i];
            if (fish == null || fish.IsDead) continue;

            if (!fish.gameObject.activeInHierarchy || !fish.isActiveAndEnabled) continue;
            float sqrDist = (fish.transform.position - bulletPos).sqrMagnitude;
            if (sqrDist < HitRadiusSq)
            {
                fish.TakeDamage(config.damage);
                PoolManager.Instance.Release(this, config.prefab);
                return;
            }
        }
    }

    public void OnRecycle()
    {
        OnBulletRecycle?.Invoke(this);

        config = null;
        direction = Vector3.zero;
        startPos = Vector3.zero;
        gameObject.SetActive(false);
    }

    public void OnSpawn()
    {
        OnBulletSpawn?.Invoke(this);
    }
}