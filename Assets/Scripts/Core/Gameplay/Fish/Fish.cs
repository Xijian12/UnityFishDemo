using System.Collections;
using UnityEngine;

public class Fish : MonoBehaviour, IPoolable
{
    public static event System.Action<int> OnFishKilled;

    private FishConfig config;
    private FishMovement _fishMovement;
    private float currentHp;

    private SpriteRenderer spriteRenderer;
    private Renderer _renderer;
    private Material _cachedMaterial;
    private Color _defaultColor;
    private bool _hasDefaultColor;
    private bool isDying = false;
    private Coroutine damageRoutine;
    private Coroutine dieRoutine;

    public bool IsDead { get; private set; } = false;

    private void Awake()
    {
        CacheDefaultColor();
    }

    private void CacheDefaultColor()
    {
        if (spriteRenderer == null)
            spriteRenderer = GetComponent<SpriteRenderer>();
        if (_renderer == null)
            _renderer = GetComponent<Renderer>();

        if (spriteRenderer != null)
        {
            _defaultColor = spriteRenderer.color;
            _hasDefaultColor = true;
        }
        else if (_renderer != null && _renderer.sharedMaterial != null)
        {
            _defaultColor = _renderer.sharedMaterial.color;
            _hasDefaultColor = true;
        }
        else
        {
            _defaultColor = Color.white;
            _hasDefaultColor = true;
        }
    }

    #region 初始化

    public void Init(FishConfig config, Vector3 p0, Vector3 p1, Vector3 p2, Vector3 p3)
    {
        this.config = config;
        this.currentHp = config.hp;

        if (_fishMovement == null)
            _fishMovement = GetComponent<FishMovement>();
        if (_fishMovement == null)
        {
            Debug.LogError("Fish 需要 FishMovement 组件！");
            return;
        }

        transform.localScale = Vector3.one;
        _fishMovement.SetPath(p0, p1, p2, p3, config.speed);
    }

    public void OnSpawn()
    {
        isDying = false;
        IsDead = false;
        _cachedMaterial = null;

        if (spriteRenderer == null)
            spriteRenderer = GetComponent<SpriteRenderer>();
        if (_renderer == null)
            _renderer = GetComponent<Renderer>();
        if (_fishMovement == null)
            _fishMovement = GetComponent<FishMovement>();

        transform.localScale = Vector3.one;

        RestoreVisualState();

        FishManager.Instance?.AddFish(this);
    }

    public void OnRecycle()
    {
        StopAllCoroutines();
        damageRoutine = null;
        dieRoutine = null;

        FishManager.Instance?.RemoveFish(this);
        _fishMovement?.Reset();
        RestoreVisualState();
    }

    #endregion

    #region 移动

    public void ManualUpdate(float deltaTime)
    {
        if (config == null || isDying) return;
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
        if (IsDead) return false;
        if (isDying || config == null) return false;

        currentHp -= damage;

        if (damageRoutine != null)
        {
            StopCoroutine(damageRoutine);
            damageRoutine = null;
        }

        damageRoutine = StartCoroutine(DamageEffect());

        if (currentHp <= 0)
        {
            isDying = true;

            if (dieRoutine != null)
            {
                StopCoroutine(dieRoutine);
                dieRoutine = null;
            }

            dieRoutine = StartCoroutine(DieRoutine());
            return true;
        }

        return false;
    }

    private IEnumerator DamageEffect()
    {
        if (spriteRenderer != null)
        {
            Color originalColor = spriteRenderer.color;
            spriteRenderer.color = Color.white;

            yield return new WaitForSeconds(0.2f);

            if (this == null || !gameObject.activeInHierarchy || spriteRenderer == null || IsDead)
            {
                damageRoutine = null;
                yield break;
            }

            spriteRenderer.color = originalColor;
            damageRoutine = null;
            yield break;
        }

        Material mat = GetSafeMaterial();
        if (mat != null)
        {
            Color originalColor = mat.color;
            mat.color = Color.white;

            yield return new WaitForSeconds(0.2f);

            if (this == null || !gameObject.activeInHierarchy || IsDead)
            {
                damageRoutine = null;
                yield break;
            }

            mat = GetSafeMaterial();
            if (mat != null)
            {
                mat.color = originalColor;
            }
        }

        damageRoutine = null;
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

        if (spriteRenderer != null)
        {
            Color startColor = spriteRenderer.color;

            while (timer < duration)
            {
                if (this == null || !gameObject.activeInHierarchy || spriteRenderer == null)
                {
                    dieRoutine = null;
                    yield break;
                }

                timer += Time.deltaTime;
                float t = timer / duration;

                transform.localScale = Vector3.Lerp(startScale, endScale, t);

                Color c = startColor;
                c.a = Mathf.Lerp(1f, 0f, t);
                spriteRenderer.color = c;

                yield return null;
            }
        }
        else
        {
            Material mat = GetSafeMaterial();

            if (mat != null)
            {
                Color startColor = mat.color;

                while (timer < duration)
                {
                    if (this == null || !gameObject.activeInHierarchy)
                    {
                        dieRoutine = null;
                        yield break;
                    }

                    mat = GetSafeMaterial();
                    if (mat == null)
                    {
                        dieRoutine = null;
                        yield break;
                    }

                    timer += Time.deltaTime;
                    float t = timer / duration;

                    transform.localScale = Vector3.Lerp(startScale, endScale, t);

                    Color c = startColor;
                    c.a = Mathf.Lerp(1f, 0f, t);
                    mat.color = c;

                    yield return null;
                }
            }
            else
            {
                while (timer < duration)
                {
                    if (this == null || !gameObject.activeInHierarchy)
                    {
                        dieRoutine = null;
                        yield break;
                    }

                    timer += Time.deltaTime;
                    float t = timer / duration;
                    transform.localScale = Vector3.Lerp(startScale, endScale, t);

                    yield return null;
                }
            }
        }

        dieRoutine = null;
        SafeRelease();
    }

    private void SafeRelease()
    {
        if (!gameObject.activeInHierarchy) return;

        StopAllCoroutines();
        damageRoutine = null;
        dieRoutine = null;

        if (config != null && config.prefab != null && PoolManager.Instance != null)
        {
            PoolManager.Instance.Release(this, config.prefab);
        }
    }

    #endregion

    #region 工具方法

    private Material GetSafeMaterial()
    {
        if (_renderer == null)
            _renderer = GetComponent<Renderer>();

        if (_renderer == null)
            return null;

        if (_cachedMaterial == null)
        {
            _cachedMaterial = _renderer.material;
        }

        return _cachedMaterial;
    }

    private void RestoreVisualState()
    {
        if (!_hasDefaultColor)
            CacheDefaultColor();

        Color c = _defaultColor;
        c.a = 1f;

        if (spriteRenderer != null)
        {
            spriteRenderer.color = c;
        }
        else
        {
            Material mat = GetSafeMaterial();
            if (mat != null)
                mat.color = c;
        }
    }

    #endregion
}