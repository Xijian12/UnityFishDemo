using UnityEngine;
using DG.Tweening;

public class FishVisual : MonoBehaviour
{
    private SpriteRenderer spriteRenderer;
    private Renderer _renderer;
    private Material _cachedMaterial;
    private Color _defaultColor;
    private bool _hasDefaultColor;
    private Transform _cachedTransform;
    private Vector3 _prefabLocalScale = Vector3.one;
    private Tween _damageTween;
    private Tween _dieTween;

    private void Awake()
    {
        CacheDefaultColor();
        _cachedTransform = transform;
        _prefabLocalScale = _cachedTransform.localScale;
    }

    public void Init()
    {
        if (spriteRenderer == null)
            spriteRenderer = GetComponent<SpriteRenderer>();
        if (_renderer == null)
            _renderer = GetComponent<Renderer>();
    }

    /// <param name="spawnScale">来自 FishConfig；为 null 时使用 Prefab 初始 localScale</param>
    public void Reset(Vector3? spawnScale = null)
    {
        KillTweens();
        Vector3 s = spawnScale ?? _prefabLocalScale;
        if (s.sqrMagnitude < 1e-8f) s = Vector3.one;
        _cachedTransform.localScale = s;
        RestoreVisualState();
    }

    /// <summary>
    /// 缓存默认颜色
    /// </summary>
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

    /// <summary>
    /// 播放受伤动画
    /// </summary>
    /// <returns></returns>
    public Tween PlayDamage()
    {
        KillTween(ref _damageTween);

        if (spriteRenderer != null)
        {
            Color originalColor = spriteRenderer.color;
            spriteRenderer.color = Color.white;
            _damageTween = spriteRenderer.DOColor(originalColor, 0.2f).SetEase(Ease.OutQuad);
            return _damageTween;
        }

        Material mat = GetSafeMaterial();
        if (mat != null)
        {
            Color originalColor = mat.color;
            mat.color = Color.white;
            _damageTween = mat.DOColor(originalColor, 0.2f).SetEase(Ease.OutQuad);
            return _damageTween;
        }

        return null;
    }

    /// <summary>
    /// 播放死亡动画
    /// </summary>
    /// <returns></returns>
    public Tween PlayDie()
    {
        KillTween(ref _dieTween);

        float duration = 0.3f;
        Vector3 startScale = _cachedTransform.localScale;
        Vector3 endScale = startScale * 1.2f;
        Sequence seq = DOTween.Sequence();
        seq.Join(_cachedTransform.DOScale(endScale, duration).SetEase(Ease.OutQuad));

        if (spriteRenderer != null)
        {
            seq.Join(spriteRenderer.DOColor(Color.white, duration).SetEase(Ease.OutQuad));
            seq.Join(spriteRenderer.DOFade(0f, duration).SetEase(Ease.OutQuad));
        }
        else
        {
            Material mat = GetSafeMaterial();
            if (mat != null)
            {
                seq.Join(mat.DOColor(Color.white, duration).SetEase(Ease.OutQuad));
                seq.Join(mat.DOFade(0f, duration).SetEase(Ease.OutQuad));
            }
        }

        _dieTween = seq;
        return _dieTween;
    }

    #region 工具方法

    /// <summary>
    /// 获取安全材质
    /// </summary>
    /// <returns></returns>
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

    /// <summary>
    /// 恢复视觉状态
    /// </summary>
    private void RestoreVisualState()
    {
        if (!_hasDefaultColor)
        {
            CacheDefaultColor();
        }

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

    private void KillTweens()
    {
        KillTween(ref _damageTween);
        KillTween(ref _dieTween);
    }

    private static void KillTween(ref Tween tween)
    {
        if (tween != null && tween.IsActive())
        {
            tween.Kill(false);
        }
        tween = null;
    }

    #endregion
}