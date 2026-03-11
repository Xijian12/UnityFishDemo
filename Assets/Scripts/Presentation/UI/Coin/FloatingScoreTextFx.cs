using TMPro;
using UnityEngine;

/// <summary>
/// 计分板旁的浮动加分文字特效（对象池友好）。
/// 仅负责：显示文本 + 上浮 + 淡出 + 回收。
/// </summary>
public class FloatingScoreTextFx : MonoBehaviour, IPoolable
{
    [SerializeField] private float moveDistance = 60f;
    [SerializeField] private float duration = 0.6f;

    private RectTransform _rectTransform;
    private TextMeshProUGUI _text;

    private GameObject _poolPrefabKey;
    private Vector2 _startAnchoredPos;
    private Vector2 _endAnchoredPos;
    private float _timer;
    private bool _isPlaying;
    private Color _initialColor;

    private void Awake()
    {
        _rectTransform = GetComponent<RectTransform>();
        _text = GetComponent<TextMeshProUGUI>();
        if (_text != null)
            _initialColor = _text.color;
    }

    /// <summary>
    /// 仅初始化参数，不触发播放。
    /// </summary>
    public void Initialize(GameObject poolPrefabKey, Vector2 startAnchoredPos, int score)
    {
        _poolPrefabKey = poolPrefabKey;
        _startAnchoredPos = startAnchoredPos;
        _endAnchoredPos = _startAnchoredPos + new Vector2(0f, moveDistance);
        _timer = 0f;

        if (_rectTransform == null)
            _rectTransform = GetComponent<RectTransform>();
        _rectTransform.anchoredPosition = _startAnchoredPos;

        if (_text == null)
            _text = GetComponent<TextMeshProUGUI>();
        if (_text != null)
        {
            _text.text = "+" + score;
            Color c = _initialColor;
            c.a = 1f;
            _text.color = c;
        }
    }

    public void Launch()
    {
        _isPlaying = true;
    }

    private void Update()
    {
        if (!_isPlaying) return;

        _timer += Time.deltaTime;
        float t = Mathf.Clamp01(_timer / Mathf.Max(0.01f, duration));

        // 上浮与透明度变化都采用无分配计算。
        _rectTransform.anchoredPosition = Vector2.LerpUnclamped(_startAnchoredPos, _endAnchoredPos, t);

        if (_text != null)
        {
            Color c = _text.color;
            c.a = 1f - t;
            _text.color = c;
        }

        if (t >= 1f)
        {
            _isPlaying = false;
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
    }

    public void OnRecycle()
    {
        _isPlaying = false;
        _timer = 0f;
        _poolPrefabKey = null;
    }
}
