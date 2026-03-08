using UnityEngine;

public class Bullet : MonoBehaviour, IPoolable
{
    public static event System.Action<Bullet> OnBulletSpawn;
    public static event System.Action<Bullet> OnBulletRecycle;

    private BulletConfig config;
    private Vector3 direction;
    private Vector3 startPos;

    public void Init(BulletConfig config, Vector3 dir)
    {
        this.config = config ?? throw new System.ArgumentNullException(nameof(config));
        this.direction = dir.normalized;

        // 设置初始位置
        transform.position = GetBottomCenterWorldPosition();
        this.startPos = transform.position;

        // 设置朝向
        transform.up = this.direction;

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

    //碰撞检测
    void CheckHit()
    {
        var activeFish = FishManager.Instance?.ActiveFish;
        if (activeFish == null) return;

        foreach (Fish fish in activeFish)
        {
            if (fish == null) continue;
            if (fish.IsDead) continue;

            if (Vector2.Distance(transform.position, fish.transform.position) < 0.5f)
            {
                fish.TakeDamage(config.damage);
                PoolManager.Instance.Release(this, config.prefab);
                return;
            }
        }
    }

    // 获取底部中心世界位置
    private Vector3 GetBottomCenterWorldPosition()
    {
        Camera cam = Camera.main;
        if (cam == null)
        {
            Debug.LogError("Main camera not found!");
            return Vector3.zero;
        }

        Vector3 screenPoint = new(Screen.width * 0.5f, 0, cam.nearClipPlane + 1f);
        return cam.ScreenToWorldPoint(screenPoint);
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