using TMPro;
using UnityEngine;
using DG.Tweening;

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
    private Color _initialColor;
    private Sequence _sequence;

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
        KillSequence();
        float safeDuration = Mathf.Max(0.01f, duration);
        _sequence = DOTween.Sequence();
        _sequence.Join(_rectTransform.DOAnchorPos(_endAnchoredPos, safeDuration).SetEase(Ease.OutQuad));
        if (_text != null)
        {
            _sequence.Join(_text.DOFade(0f, safeDuration).SetEase(Ease.OutQuad));
        }

        _sequence.OnComplete(() =>
        {
            if (PoolManager.Instance != null && _poolPrefabKey != null)
            {
                PoolManager.Instance.Release(this, _poolPrefabKey);
            }
        });
    }

    private void KillSequence()
    {
        if (_sequence != null && _sequence.IsActive())
        {
            _sequence.Kill(false);
        }
        _sequence = null;
    }

    public void OnSpawn()
    {
        KillSequence();
    }

    public void OnRecycle()
    {
        KillSequence();
        _poolPrefabKey = null;
    }
}
