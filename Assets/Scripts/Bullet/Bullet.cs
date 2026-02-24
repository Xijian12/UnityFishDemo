using UnityEngine;

public class Bullet : MonoBehaviour, IPoolable
{
    private BulletConfig config;
    private Vector3 direction;
    private Vector3 startPos;

    public void Init(BulletConfig config, Vector3 dir)
    {
        this.config = config ?? throw new System.ArgumentNullException(nameof(config));
        this.direction = dir.normalized;

        // 设置初始位置：屏幕顶部中央
        transform.position = GetBottomCenterWorldPosition();
        this.startPos = transform.position;

        // 设置朝向,让子弹指向移动方向（假设子弹默认朝上）
        transform.up = this.direction;

        gameObject.SetActive(true);
    }

    void Update()
    {
        if (config == null)
        {
            gameObject.SetActive(false);
            return;
        }

        // 移动
        transform.position += config.speed * Time.deltaTime * direction;

        // 超出一定距离后（即飞出屏幕）后，也需要回收子弹
        if (Vector3.Distance(startPos, transform.position) > config.maxDistance)
        {
            PoolManager.Instance.Release(this, config.prefab);
            return;
        }

        // 碰撞检测
        CheckHit();
    }

    void CheckHit()
    {
        var activeFish = FishManager.Instance?.ActiveFish;
        if (activeFish == null) return;

        foreach (Fish fish in activeFish)
        {
            if (fish == null) continue;

            if (Vector2.Distance(transform.position, fish.transform.position) < 0.5f)
            {
                fish.TakeDamage(config.damage);
                PoolManager.Instance.Release(this, config.prefab);
                return;
            }
        }
    }

    // 获取屏幕顶部底部的世界坐标
    private Vector3 GetBottomCenterWorldPosition()
    {
        Camera cam = Camera.main;
        if (cam == null)
        {
            Debug.LogError("Main camera not found!");
            return Vector3.zero;
        }

        // 屏幕底层中央（Y = 0）
        Vector3 screenPoint = new(Screen.width * 0.5f, 0, cam.nearClipPlane + 1f);
        return cam.ScreenToWorldPoint(screenPoint);
    }

    public void OnRecycle()
    {
        config = null;
        direction = Vector3.zero;
        startPos = Vector3.zero;
        gameObject.SetActive(false);
    }

    public void OnSpawn()
    {
        // 留空
    }
}