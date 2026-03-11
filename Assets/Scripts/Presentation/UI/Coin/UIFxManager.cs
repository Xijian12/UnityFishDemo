using UnityEngine;

/// <summary>
/// UI 特效管理器（金币 + 浮动加分文字）。
///
/// 设计目标：
/// 1) 订阅鱼死亡事件，作为唯一 UI 表现入口。
/// 2) 负责坐标转换（世界坐标 -> UI anchored 坐标）。
/// 3) 负责从对象池取出/回收金币与浮字对象。
/// 4) 不处理游戏分数累加逻辑（由 ScoreManager 单独负责）。
/// </summary>
public class UIFxManager : MonoBehaviour
{
    public static UIFxManager Instance;

    [Header("Canvas / 相机")]
    [SerializeField] private Canvas uiCanvas;
    [SerializeField] private Camera worldCamera;

    [Header("计分板锚点")]
    [Tooltip("计分板 RectTransform，金币飞行目标点。")]
    [SerializeField] private RectTransform scoreBoardTarget;
    [Tooltip("浮动文字相对计分板目标点的偏移（右侧显示）。")]
    [SerializeField] private Vector2 floatingTextOffset = new Vector2(100f, -25f);

    [Header("预制体（需实现 IPoolable）")]
    [SerializeField] private GameObject coinPrefab;
    [SerializeField] private GameObject floatingTextPrefab;

    [Header("对象池配置")]
    [SerializeField] private int coinInitialPoolSize = 30;
    [SerializeField] private int coinMaxPoolSize = 120;
    [SerializeField] private int textInitialPoolSize = 20;
    [SerializeField] private int textMaxPoolSize = 80;

    [Header("动画参数")]
    [SerializeField] private float coinMoveDuration = 0.45f;

    private RectTransform _canvasRect;
    private Camera _uiEventCamera;

    private void Awake()
    {
        Instance = this;

        if (uiCanvas == null)
            uiCanvas = GetComponentInParent<Canvas>();
        if (uiCanvas != null && uiCanvas.rootCanvas != null)
            uiCanvas = uiCanvas.rootCanvas;
        if (worldCamera == null)
            worldCamera = Camera.main;

        _canvasRect = uiCanvas != null ? uiCanvas.GetComponent<RectTransform>() : null;
        _uiEventCamera = (uiCanvas != null && uiCanvas.renderMode == RenderMode.ScreenSpaceOverlay)
            ? null
            : uiCanvas != null ? uiCanvas.worldCamera : null;

        ValidateBindings();
    }

    private void Start()
    {
        CreatePools();
    }

    private void OnEnable()
    {
        EventBusClass.Instance.Subscribe<FishKilledEvent>(OnFishKilled);
        EventBusClass.Instance.Subscribe<CoinArrivedEvent>(OnCoinArrived);
    }

    private void OnDisable()
    {
        EventBusClass.Instance.Unsubscribe<FishKilledEvent>(OnFishKilled);
        EventBusClass.Instance.Unsubscribe<CoinArrivedEvent>(OnCoinArrived);
    }

    /// <summary>
    /// 鱼死亡 UI 入口：
    /// 1) 世界坐标转 UI 坐标
    /// 2) 从池中取金币并发射
    /// </summary>
    private void OnFishKilled(FishKilledEvent fishKilledEvent)
    {
        if (uiCanvas == null || _canvasRect == null || scoreBoardTarget == null || coinPrefab == null)
            return;
        if (worldCamera == null)
            return;
        if (PoolManager.Instance == null)
            return;

        if (!TryWorldToAnchoredPosition(fishKilledEvent.Position, out Vector2 startAnchoredPos))
            return;

        if (!TryScreenToAnchoredPosition(scoreBoardTarget.position, out Vector2 targetAnchoredPos))
            return;

        CoinUIFx coinFx = PoolManager.Instance.Get<CoinUIFx>(coinPrefab);
        if (coinFx == null) return;

        // 确保对象池取出的金币始终挂在目标 Canvas 下，避免层级/坐标系污染。
        coinFx.transform.SetParent(uiCanvas.transform, false);
        coinFx.transform.SetAsLastSibling();

        coinFx.Initialize(
            poolPrefabKey: coinPrefab,
            startAnchoredPos: startAnchoredPos,
            targetAnchoredPos: targetAnchoredPos,
            score: fishKilledEvent.Score,
            moveDuration: coinMoveDuration);
        coinFx.Launch();
    }

    /// <summary>
    /// 由事件总线在“金币到达计分板”时触发。
    /// 在计分板右侧生成 +score 浮动文字。
    /// </summary>
    private void OnCoinArrived(CoinArrivedEvent coinArrivedEvent)
    {
        if (floatingTextPrefab == null || scoreBoardTarget == null || _canvasRect == null)
            return;
        if (PoolManager.Instance == null)
            return;

        if (!TryScreenToAnchoredPosition(scoreBoardTarget.position, out Vector2 scoreBoardAnchoredPos))
            return;

        FloatingScoreTextFx textFx = PoolManager.Instance.Get<FloatingScoreTextFx>(floatingTextPrefab);
        if (textFx == null) return;

        textFx.transform.SetParent(uiCanvas.transform, false);
        textFx.transform.SetAsLastSibling();

        textFx.Initialize(
            poolPrefabKey: floatingTextPrefab,
            startAnchoredPos: scoreBoardAnchoredPos + floatingTextOffset,
            score: coinArrivedEvent.Score);
        textFx.Launch();
    }

    /// <summary>
    /// 创建对象池
    /// </summary>
    private void CreatePools()
    {
        if (PoolManager.Instance == null) return;

        if (coinPrefab != null)
        {
            PoolManager.Instance.CreatePool<CoinUIFx>(
                coinPrefab,
                coinInitialPoolSize,
                coinMaxPoolSize,
                uiCanvas != null ? uiCanvas.transform : transform);
        }

        if (floatingTextPrefab != null)
        {
            PoolManager.Instance.CreatePool<FloatingScoreTextFx>(
                floatingTextPrefab,
                textInitialPoolSize,
                textMaxPoolSize,
                uiCanvas != null ? uiCanvas.transform : transform);
        }
    }

    /// <summary>
    /// 运行前检查关键绑定，防止“对象激活但不可见”。
    /// </summary>
    private void ValidateBindings()
    {
        if (uiCanvas == null)
        {
            Debug.LogError("UIFxManager: uiCanvas 未设置，金币 UI 无法显示。");
            return;
        }

        if (_canvasRect == null)
        {
            Debug.LogError("UIFxManager: uiCanvas 缺少 RectTransform。");
            return;
        }

        if (_canvasRect.lossyScale.sqrMagnitude < 0.0001f)
        {
            Debug.LogWarning("UIFxManager: 当前 Canvas 缩放接近 0，UI 可能不可见，请检查 Canvas 层级缩放。");
        }

        if (coinPrefab != null && coinPrefab.GetComponent<RectTransform>() == null)
        {
            Debug.LogError("UIFxManager: coinPrefab 不是 UI 预制体（缺少 RectTransform）。当前系统为 2D UI 坐标系金币方案。");
        }
    }

    /// <summary>
    /// 世界坐标 -> 当前 Canvas 的 anchored 坐标。
    /// </summary>
    private bool TryWorldToAnchoredPosition(Vector3 worldPosition, out Vector2 anchoredPosition)
    {
        anchoredPosition = default;
        Vector3 screenPos = worldCamera.WorldToScreenPoint(worldPosition);
        return RectTransformUtility.ScreenPointToLocalPointInRectangle(
            _canvasRect, screenPos, _uiEventCamera, out anchoredPosition);
    }

    /// <summary>
    /// 屏幕坐标 -> Canvas anchored 坐标。
    /// </summary>
    private bool TryScreenToAnchoredPosition(Vector3 screenPosition, out Vector2 anchoredPosition)
    {
        anchoredPosition = default;
        return RectTransformUtility.ScreenPointToLocalPointInRectangle(
            _canvasRect, screenPosition, _uiEventCamera, out anchoredPosition);
    }
}