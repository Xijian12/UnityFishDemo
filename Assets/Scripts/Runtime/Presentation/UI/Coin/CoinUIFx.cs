using UnityEngine;

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
    private float _timer;                          // 当前动画进度计时器
    private int _score;                            // 分数值
    private bool _isPlaying;                       // 动画是否正在播放

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
        _timer = 0f;

        if (_rectTransform == null)
            _rectTransform = GetComponent<RectTransform>();
        _rectTransform.anchoredPosition = _startAnchoredPos;
    }

    /// <summary>
    /// 启动动画
    /// </summary>
    public void Launch()
    {
        _isPlaying = true;
    }

    private void Update()
    {
        if (!_isPlaying) return;  // 如果动画未启动，直接返回

        _timer += Time.deltaTime;  // 累积时间
        float t = Mathf.Clamp01(_timer / _moveDuration);  // 归一化进度 [0,1]

        // SmoothStep 缓动函数
        float smoothT = t * t * (3f - 2f * t);

        // 线性插值更新位置
        _rectTransform.anchoredPosition = Vector2.LerpUnclamped(_startAnchoredPos, _targetAnchoredPos, smoothT);

        if (t >= 1f)  // 动画完成
        {
            _isPlaying = false;  // 停止动画

            // 发布事件（通知计分系统加分）
            EventBusClass.Instance.Publish(new CoinArrivedEvent()
            {
                Score = _score,
                Position = _targetAnchoredPos
            });

            // 回收到对象池
            if (PoolManager.Instance != null && _poolPrefabKey != null)
            {
                PoolManager.Instance.Release(this, _poolPrefabKey);
            }
        }
    }

    public void OnSpawn()
    {
        _isPlaying = false;
        _timer = 0f;
        if (_rectTransform == null)
            _rectTransform = GetComponent<RectTransform>();
        if (_rectTransform != null)
            _rectTransform.localScale = Vector3.one;
    }

    public void OnRecycle()
    {
        _isPlaying = false;
        _timer = 0f;
        _score = 0;
        _poolPrefabKey = null;
    }
}