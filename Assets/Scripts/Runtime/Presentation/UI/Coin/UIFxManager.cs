using UnityEngine;

/// <summary>
/// UI 特效管理器（金币 + 浮动加分文字）。
/// 所有坐标统一换算到 Root Canvas 的局部空间。
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
    private Transform _poolParent;

    private void Awake()
    {
        Instance = this;
        ResolveCamera();
        RefreshCanvasState();
    }

    private void Start()
    {
        EnsurePools();
        ValidateBindings();
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
        EnsurePools();
        ValidateBindings();
    }

    public void UnbindScorePanel(ScorePanelView view)
    {
        if (_boundScorePanel != view) return;
        _boundScorePanel = null;
    }

    private void OnFishKilled(FishKilledEvent fishKilledEvent)
    {
        if (!IsReadyForCoinFx()) return;

        if (!TryWorldToCanvasAnchoredPosition(fishKilledEvent.Position, out Vector2 startAnchoredPos))
            return;

        if (!TryRectToCanvasAnchoredPosition(scoreBoardTarget, out Vector2 targetAnchoredPos))
            return;

        CoinUIFx coinFx = PoolManager.Instance.Get<CoinUIFx>(coinPrefab);
        if (coinFx == null) return;

        RectTransform coinRect = coinFx.GetComponent<RectTransform>();
        coinRect.SetParent(_canvasRect, false);
        coinRect.SetAsLastSibling();

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

        if (!TryRectToCanvasAnchoredPosition(scoreBoardTarget, out Vector2 scoreBoardAnchoredPos))
            return;

        FloatingScoreTextFx textFx = PoolManager.Instance.Get<FloatingScoreTextFx>(floatingTextPrefab);
        if (textFx == null) return;

        RectTransform textRect = textFx.GetComponent<RectTransform>();
        textRect.SetParent(_canvasRect, false);
        textRect.SetAsLastSibling();

        textFx.Initialize(
            poolPrefabKey: floatingTextPrefab,
            startAnchoredPos: scoreBoardAnchoredPos + floatingTextOffset,
            score: coinArrivedEvent.Score);
        textFx.Launch();
    }

    private bool IsReadyForCoinFx()
    {
        return _canvasRect != null
               && scoreBoardTarget != null
               && scoreBoardTarget.gameObject.activeInHierarchy
               && coinPrefab != null
               && worldCamera != null
               && PoolManager.Instance != null;
    }

    private void EnsurePools()
    {
        if (PoolManager.Instance == null || _canvasRect == null || coinPrefab == null)
            return;

        Transform poolParent = _canvasRect;
        if (_poolsCreated && _poolParent == poolParent)
            return;

        _poolParent = poolParent;

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
        Canvas root = ResolveRootCanvas(uiCanvas);
        if (root == null && _boundScorePanel != null)
            root = ResolveRootCanvas(_boundScorePanel.PanelCanvas);

        if (root != null)
            uiCanvas = root;

        _canvasRect = root != null ? root.GetComponent<RectTransform>() : null;
        _uiEventCamera = root != null && root.renderMode != RenderMode.ScreenSpaceOverlay
            ? root.worldCamera
            : null;

        EnsureCanvasTransformValid(_canvasRect);
    }

    private static Canvas ResolveRootCanvas(Canvas canvas)
    {
        if (canvas == null) return null;
        return canvas.rootCanvas != null ? canvas.rootCanvas : canvas;
    }

    private static void EnsureCanvasTransformValid(RectTransform canvasRect)
    {
        if (canvasRect == null) return;

        if (canvasRect.localScale.sqrMagnitude < 0.0001f)
            canvasRect.localScale = Vector3.one;
    }

    private void ValidateBindings()
    {
        if (_canvasRect == null)
            Debug.LogWarning("UIFxManager: Root Canvas not resolved. Wait for GameplayHudPanel / ScorePanelView bind.");

        if (scoreBoardTarget == null)
            Debug.LogWarning("UIFxManager: scoreBoardTarget not set. Coin fly FX will be skipped.");

        if (coinPrefab != null && coinPrefab.GetComponent<RectTransform>() == null)
            Debug.LogError("UIFxManager: coinPrefab is not a UI prefab (missing RectTransform).");
    }

    private bool TryWorldToCanvasAnchoredPosition(Vector3 worldPosition, out Vector2 anchoredPosition)
    {
        anchoredPosition = default;
        if (_canvasRect == null || worldCamera == null) return false;

        Vector3 screenPos = worldCamera.WorldToScreenPoint(worldPosition);
        if (screenPos.z < 0f) return false;

        return RectTransformUtility.ScreenPointToLocalPointInRectangle(
            _canvasRect, screenPos, _uiEventCamera, out anchoredPosition);
    }

    private bool TryRectToCanvasAnchoredPosition(RectTransform target, out Vector2 anchoredPosition)
    {
        anchoredPosition = default;
        if (target == null || _canvasRect == null) return false;

        Vector2 screenPos = GetRectCenterScreenPoint(target, _uiEventCamera);
        return RectTransformUtility.ScreenPointToLocalPointInRectangle(
            _canvasRect, screenPos, _uiEventCamera, out anchoredPosition);
    }

    private static Vector2 GetRectCenterScreenPoint(RectTransform rect, Camera eventCamera)
    {
        rect.GetWorldCorners(ScratchCorners);
        Vector3 worldCenter = (ScratchCorners[0] + ScratchCorners[2]) * 0.5f;
        return RectTransformUtility.WorldToScreenPoint(eventCamera, worldCenter);
    }

    private static readonly Vector3[] ScratchCorners = new Vector3[4];
}
