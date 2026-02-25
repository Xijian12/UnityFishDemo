using UnityEngine;

public class Fish : MonoBehaviour, IPoolable
{
    // 静态事件,通知鱼以及被击杀了
    public static event System.Action<int> OnFishKilled;

    private FishConfig config;
    private Vector3 startPoint;
    private Vector3 endPoint;
    private Vector3 controlPoint;
    //缓存transform，以减少跨层访问
    private Transform _cacheTransform;

    private float moveTime;
    private float duration;
    private float currentHp;

    public void Init(FishConfig config, Vector3 startPoint, Vector3 endPoint)
    {
        this.config = config;
        this.startPoint = startPoint;
        this.endPoint = endPoint;
        this.currentHp = config.hp;

        _cacheTransform = transform;
        _cacheTransform.position = startPoint;

        float distance = Vector3.Distance(startPoint, endPoint);
        duration = distance / config.speed;
        moveTime = 0f;

        // 生成控制点（制造弧度）
        Vector3 mid = (startPoint + endPoint) / 2f;

        float curveOffset = Random.Range(-3f, 3f); // 弯曲强度
        controlPoint = mid + Vector3.up * curveOffset;
    }

    public void ManualUpdate(float deltaTime)
    {
        if (config == null) return;

        moveTime += deltaTime;
        float t = Mathf.Clamp01(moveTime / duration);
        _cacheTransform.position = CalculateBezierPoint(t);
        Vector3 direction = (CalculateBezierPoint(Mathf.Clamp01(t + 0.01f)) - _cacheTransform.position).normalized;
        if (direction != Vector3.zero)
        {
            _cacheTransform.right = direction;
        }

        if (t >= 1f)
        {
            PoolManager.Instance.Release(this, config.prefab);
        }
    }

    public void OnSpawn()
    {
        config = null;
        // 每生成一条鱼就加入到fishManager中的活跃数组中方便管理
        FishManager.Instance.AddFish(this);
    }

    public void OnRecycle()
    {
        // 每回收一条鱼就移除fishManager中的活跃数组中的鱼对象
        FishManager.Instance.RemoveFish(this);
    }

    public bool TakeDamage(int damage)
    {
        currentHp -= damage;
        if (currentHp <= 0)
        {
            int score = config?.score ?? 0;
            OnFishKilled?.Invoke(score); // 广播事件
            PoolManager.Instance.Release(this, config.prefab);
            return true;
        }
        return false;
    }

    private Vector3 CalculateBezierPoint(float t)
    {
        float u = 1 - t;

        return u * u * startPoint
             + 2 * u * t * controlPoint
             + t * t * endPoint;
    }
}
