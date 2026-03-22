using UnityEngine;
using DG.Tweening;

/// <summary>
/// 金币飞行特效
/// </summary>
/// <remarks>
/// 强制要求此脚本所在的GameObject必须有RectTransform组件（UI元素必备）
/// </remarks>
[RequireComponent(typeof(RectTransform))]
public class CoinUIFx : MonoBehaviour, IPoolable
{
    private RectTransform _rectTransform;           // UI变换组件
    private GameObject _poolPrefabKey;              // 对象池标识
    private Vector2 _startAnchoredPos;              // 起始位置
    private Vector2 _targetAnchoredPos;             // 目标位置
    private float _moveDuration;                    // 移动总时长
    private int _score;                            // 分数值
    private Tween _moveTween;

    private void Awake()
    {
        _rectTransform = GetComponent<RectTransform>();
    }

    /// <summary>
    /// 初始化
    /// </summary>
    /// <param name="poolPrefabKey"></param>
    /// <param name="startAnchoredPos"></param>
    /// <param name="targetAnchoredPos"></param>
    /// <param name="score"></param>
    /// <param name="moveDuration"></param>
    public void Initialize(
        GameObject poolPrefabKey,
        Vector2 startAnchoredPos,
        Vector2 targetAnchoredPos,
        int score,
        float moveDuration)
    {
        _poolPrefabKey = poolPrefabKey;
        _startAnchoredPos = startAnchoredPos;
        _targetAnchoredPos = targetAnchoredPos;
        _score = score;
        _moveDuration = Mathf.Max(0.01f, moveDuration);

        if (_rectTransform == null)
            _rectTransform = GetComponent<RectTransform>();
        _rectTransform.anchoredPosition = _startAnchoredPos;
    }

    /// <summary>
    /// 启动动画
    /// </summary>
    public void Launch()
    {
        KillMoveTween();
        _moveTween = _rectTransform
            .DOAnchorPos(_targetAnchoredPos, _moveDuration)
            .SetEase(Ease.InOutSine)
            .OnComplete(() =>
        {
            EventBusClass.Instance.Publish(new CoinArrivedEvent()
            {
                Score = _score,
                Position = _targetAnchoredPos
            });

            if (PoolManager.Instance != null && _poolPrefabKey != null)
            {
                PoolManager.Instance.Release(this, _poolPrefabKey);
            }
        });
    }

    private void KillMoveTween()
    {
        if (_moveTween != null && _moveTween.IsActive())
        {
            _moveTween.Kill(false);
        }
        _moveTween = null;
    }

    public void OnSpawn()
    {
        KillMoveTween();
        if (_rectTransform == null)
            _rectTransform = GetComponent<RectTransform>();
        if (_rectTransform != null)
            _rectTransform.localScale = Vector3.one;
    }

    public void OnRecycle()
    {
        KillMoveTween();
        _score = 0;
        _poolPrefabKey = null;
    }
}