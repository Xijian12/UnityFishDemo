using System.Collections;
using UnityEngine;

public class Fish : MonoBehaviour, IPoolable
{
    public static event System.Action<int> OnFishKilled;

    private FishConfig config;
    private Vector3 startPoint;
    private Vector3 endPoint;
    private Vector3 controlPoint;

    private Transform _cacheTransform;

    private float moveTime;
    private float duration;
    private float currentHp;

    private SpriteRenderer spriteRenderer;
    private bool isDying = false;
    private Coroutine damageRoutine;

    public bool IsDead { get; private set; } = false;

    #region 初始化

    public void Init(FishConfig config, Vector3 startPoint, Vector3 endPoint)
    {
        this.config = config;
        this.startPoint = startPoint;
        this.endPoint = endPoint;
        this.currentHp = config.hp;

        _cacheTransform = transform;
        _cacheTransform.position = startPoint;
        _cacheTransform.localScale = Vector3.one;

        float distance = Vector3.Distance(startPoint, endPoint);
        duration = distance / config.speed;
        moveTime = 0f;

        Vector3 mid = (startPoint + endPoint) / 2f;
        float curveOffset = Random.Range(-3f, 3f);
        controlPoint = mid + Vector3.up * curveOffset;
    }

    public void OnSpawn()
    {
        isDying = false;

        if (spriteRenderer == null)
            spriteRenderer = GetComponent<SpriteRenderer>();

        transform.localScale = Vector3.one;

        FishManager.Instance.AddFish(this);
    }

    public void OnRecycle()
    {
        FishManager.Instance.RemoveFish(this);
    }

    #endregion

    #region 移动

    public void ManualUpdate(float deltaTime)
    {
        if (config == null || isDying) return;

        moveTime += deltaTime;
        float t = Mathf.Clamp01(moveTime / duration);

        _cacheTransform.position = CalculateBezierPoint(t);

        Vector3 nextPos = CalculateBezierPoint(Mathf.Clamp01(t + 0.01f));
        Vector3 direction = (nextPos - _cacheTransform.position).normalized;

        if (direction != Vector3.zero)
            _cacheTransform.right = direction;

        if (t >= 1f)
        {
            SafeRelease();
        }
    }

    private Vector3 CalculateBezierPoint(float t)
    {
        float u = 1 - t;
        return u * u * startPoint
             + 2 * u * t * controlPoint
             + t * t * endPoint;
    }

    #endregion

    #region 受伤

    public bool TakeDamage(int damage)
    {
        if (IsDead) return false;
        if (isDying || config == null) return false;

        currentHp -= damage;

        if (damageRoutine != null)
            StopCoroutine(damageRoutine);

        damageRoutine = StartCoroutine(DamageEffect());

        if (currentHp <= 0)
        {
            isDying = true;
            StartCoroutine(DieRoutine());
            return true;
        }

        return false;
    }

    private IEnumerator DamageEffect()
    {
        if (spriteRenderer == null)
            yield break;

        Color originalColor = spriteRenderer.color;

        // 闪白
        spriteRenderer.color = Color.white;

        yield return new WaitForSeconds(0.2f);

        // 恢复原色
        spriteRenderer.color = originalColor;
    }

    #endregion

    #region 死亡

    private IEnumerator DieRoutine()
    {
        IsDead = true;
        int score = config != null ? config.score : 0;

        OnFishKilled?.Invoke(score);

        if (ScoreManager.Instance != null)
        {
            ScoreManager.Instance.SpawnCoinFromWorld(transform.position);
        }

        float duration = 0.3f;
        float timer = 0f;

        Vector3 startScale = transform.localScale;
        Vector3 endScale = startScale * 1.2f;

        Color startColor = spriteRenderer.color;

        while (timer < duration)
        {
            timer += Time.deltaTime;
            float t = timer / duration;

            transform.localScale = Vector3.Lerp(startScale, endScale, t);

            Color c = startColor;
            c.a = Mathf.Lerp(1f, 0f, t);
            spriteRenderer.color = c;

            yield return null;
        }

        SafeRelease();
    }

    private void SafeRelease()
    {
        if (config != null && config.prefab != null)
        {
            PoolManager.Instance.Release(this, config.prefab);
        }
    }

    #endregion
}