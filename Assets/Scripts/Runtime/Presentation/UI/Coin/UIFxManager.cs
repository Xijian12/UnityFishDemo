using UnityEngine;

/// <summary>
/// UI 特效管理器（金币 + 浮动加分文字）。
/// </summary>
public class UIFxManager : MonoBehaviour
{
    public static UIFxManager Instance;

    [Header("Canvas / 相机")]
    [SerializeField] private Canvas uiCanvas;
    [SerializeField] private Camera worldCamera;

    [Header("计分板锚点")]
    [SerializeField] private RectTransform scoreBoardTarget;
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
    private ScorePanelView _boundScorePanel;
    private bool _poolsCreated;

    private void Awake()
    {
        Instance = this;
        ResolveCamera();
        RefreshCanvasState();
        ValidateBindings();
    }

    private void Start()
    {
        EnsurePools();
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

    public void BindScorePanel(ScorePanelView view)
    {
        if (view == null) return;

        _boundScorePanel = view;
        uiCanvas = view.PanelCanvas;
        scoreBoardTarget = view.CoinFlyTarget;
        RefreshCanvasState();
        ValidateBindings();
        EnsurePools();
    }

    public void UnbindScorePanel(ScorePanelView view)
    {
        if (_boundScorePanel != view) return;
        _boundScorePanel = null;
    }

    private void OnFishKilled(FishKilledEvent fishKilledEvent)
    {
        if (!IsReadyForCoinFx()) return;

        if (!TryWorldToAnchoredPosition(fishKilledEvent.Position, out Vector2 startAnchoredPos))
            return;

        if (!TryRectTransformToAnchoredPosition(scoreBoardTarget, out Vector2 targetAnchoredPos))
            return;

        CoinUIFx coinFx = PoolManager.Instance.Get<CoinUIFx>(coinPrefab);
        if (coinFx == null) return;

        Transform coinParent = _canvasRect;
        coinFx.transform.SetParent(coinParent, false);
        coinFx.transform.SetAsLastSibling();

        coinFx.Initialize(
            poolPrefabKey: coinPrefab,
            startAnchoredPos: startAnchoredPos,
            targetAnchoredPos: targetAnchoredPos,
            score: fishKilledEvent.Score,
            moveDuration: coinMoveDuration);
        coinFx.Launch();
    }

    private void OnCoinArrived(CoinArrivedEvent coinArrivedEvent)
    {
        if (floatingTextPrefab == null || scoreBoardTarget == null || _canvasRect == null)
            return;
        if (PoolManager.Instance == null)
            return;

        if (!TryRectTransformToAnchoredPosition(scoreBoardTarget, out Vector2 scoreBoardAnchoredPos))
            return;

        FloatingScoreTextFx textFx = PoolManager.Instance.Get<FloatingScoreTextFx>(floatingTextPrefab);
        if (textFx == null) return;

        textFx.transform.SetParent(_canvasRect, false);
        textFx.transform.SetAsLastSibling();

        textFx.Initialize(
            poolPrefabKey: floatingTextPrefab,
            startAnchoredPos: scoreBoardAnchoredPos + floatingTextOffset,
            score: coinArrivedEvent.Score);
        textFx.Launch();
    }

    private bool IsReadyForCoinFx()
    {
        return uiCanvas != null
               && _canvasRect != null
               && scoreBoardTarget != null
               && coinPrefab != null
               && worldCamera != null
               && PoolManager.Instance != null;
    }

    private void EnsurePools()
    {
        if (_poolsCreated || PoolManager.Instance == null) return;
        if (uiCanvas == null || coinPrefab == null) return;

        Transform poolParent = _canvasRect != null ? _canvasRect : uiCanvas.transform;

        PoolManager.Instance.CreatePool<CoinUIFx>(
            coinPrefab,
            coinInitialPoolSize,
            coinMaxPoolSize,
            poolParent);

        if (floatingTextPrefab != null)
        {
            PoolManager.Instance.CreatePool<FloatingScoreTextFx>(
                floatingTextPrefab,
                textInitialPoolSize,
                textMaxPoolSize,
                poolParent);
        }

        _poolsCreated = true;
    }

    private void ResolveCamera()
    {
        if (worldCamera == null)
            worldCamera = Camera.main;
    }

    private void RefreshCanvasState()
    {
        if (uiCanvas == null)
            uiCanvas = GetComponentInParent<Canvas>();

        _canvasRect = uiCanvas != null ? uiCanvas.GetComponent<RectTransform>() : null;
        _uiEventCamera = uiCanvas != null && uiCanvas.renderMode != RenderMode.ScreenSpaceOverlay
            ? uiCanvas.worldCamera
            : null;
    }

    private void ValidateBindings()
    {
        if (uiCanvas == null)
            Debug.LogWarning("UIFxManager: uiCanvas not set. Assign ScorePanel or wait for ScorePanelView.BindScorePanel.");

        if (scoreBoardTarget == null)
            Debug.LogWarning("UIFxManager: scoreBoardTarget not set. Coin fly FX will be skipped.");

        if (coinPrefab != null && coinPrefab.GetComponent<RectTransform>() == null)
            Debug.LogError("UIFxManager: coinPrefab is not a UI prefab (missing RectTransform).");
    }

    private bool TryWorldToAnchoredPosition(Vector3 worldPosition, out Vector2 anchoredPosition)
    {
        anchoredPosition = default;
        if (_canvasRect == null || worldCamera == null) return false;

        Vector3 screenPos = worldCamera.WorldToScreenPoint(worldPosition);
        if (screenPos.z < 0f) return false;

        return RectTransformUtility.ScreenPointToLocalPointInRectangle(
            _canvasRect, screenPos, _uiEventCamera, out anchoredPosition);
    }

    private bool TryRectTransformToAnchoredPosition(RectTransform target, out Vector2 anchoredPosition)
    {
        anchoredPosition = default;
        if (target == null || _canvasRect == null) return false;

        Vector3 worldCenter = target.TransformPoint(target.rect.center);
        Vector2 screenPos = RectTransformUtility.WorldToScreenPoint(_uiEventCamera, worldCenter);
        return RectTransformUtility.ScreenPointToLocalPointInRectangle(
            _canvasRect, screenPos, _uiEventCamera, out anchoredPosition);
    }
}
