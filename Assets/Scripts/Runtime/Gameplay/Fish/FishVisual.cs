using UnityEngine;
using System.Collections;

public class FishVisual : MonoBehaviour
{
    private SpriteRenderer spriteRenderer;
    private Renderer _renderer;
    private Material _cachedMaterial;
    private Color _defaultColor;
    private bool _hasDefaultColor;
    private Transform _cachedTransform;

    private void Awake()
    {
        CacheDefaultColor();
        _cachedTransform = transform;
    }

    public void Init()
    {
        if (spriteRenderer == null)
            spriteRenderer = GetComponent<SpriteRenderer>();
        if (_renderer == null)
            _renderer = GetComponent<Renderer>();
    }

    public void Reset()
    {
        // 不要清空默认颜色缓存；对象池复用时需要始终回到 prefab 原始色。
        _cachedTransform.localScale = Vector3.one;
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
    public IEnumerator PlayDamage()
    {
        if (spriteRenderer != null)
        {
            Color originalColor = spriteRenderer.color;
            spriteRenderer.color = Color.white;

            yield return new WaitForSeconds(0.2f);

            if (this == null || !gameObject.activeInHierarchy || spriteRenderer == null)
            {
                yield break;
            }

            spriteRenderer.color = originalColor;
            yield break;
        }

        Material mat = GetSafeMaterial();
        if (mat != null)
        {
            Color originalColor = mat.color;
            mat.color = Color.white;

            yield return new WaitForSeconds(0.2f);

            if (this == null || !gameObject.activeInHierarchy)
            {
                yield break;
            }

            mat = GetSafeMaterial();
            if (mat != null)
            {
                mat.color = originalColor;
            }
        }

    }

    /// <summary>
    /// 播放死亡动画
    /// </summary>
    /// <returns></returns>
    public IEnumerator PlayDie()
    {
        float duration = 0.3f;
        float timer = 0f;
        Vector3 startScale = transform.localScale;
        Vector3 endScale = startScale * 1.2f;

        if (spriteRenderer != null)
        {
            Color startColor = spriteRenderer.color; // 记录初始颜色（包含Alpha）

            while (timer < duration) // 循环直到时间结束
            {
                // 安全检查,防止物体在动画过程中被销毁
                if (this == null || !gameObject.activeInHierarchy || spriteRenderer == null)
                {
                    yield break;
                }

                timer += Time.deltaTime; // 累加时间
                float t = timer / duration; // 计算归一化进度 (0.0 -> 1.0)

                // 1. 应用缩放动画
                transform.localScale = Vector3.Lerp(startScale, endScale, t);

                // 2. 应用透明度动画
                Color c = startColor;
                c.a = Mathf.Lerp(1f, 0f, t); // Alpha 从 1 变到 0
                spriteRenderer.color = c;

                yield return null; // 等待下一帧
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
                    // 安全检查
                    if (this == null || !gameObject.activeInHierarchy) { yield break; }

                    // 关键点,重新获取材质引用,防止材质被销毁
                    mat = GetSafeMaterial();
                    if (mat == null) { yield break; }

                    timer += Time.deltaTime;
                    float t = timer / duration;

                    transform.localScale = Vector3.Lerp(startScale, endScale, t);

                    Color c = startColor;
                    c.a = Mathf.Lerp(1f, 0f, t);
                    mat.color = c; // 修改材质颜色

                    yield return null;
                }
            }
            else
            {
                // 如果没有材质可修改颜色，只执行缩放动画
                while (timer < duration)
                {
                    if (this == null || !gameObject.activeInHierarchy) { yield break; }

                    timer += Time.deltaTime;
                    float t = timer / duration;

                    // 仅执行缩放
                    transform.localScale = Vector3.Lerp(startScale, endScale, t);

                    yield return null;
                }
            }
        }

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

    #endregion
}