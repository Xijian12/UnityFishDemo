using UnityEngine;

/// <summary>
/// 固定俯视摄像机。
/// - 位置固定 (0, 20, 0)，不每帧重算
/// - 俯角固定 60°，仅允许绕 Y 轴小范围旋转
/// - 抖动由相机内部管理，外部不直接改 Transform
/// - 支持慢镜头模式（使用 unscaledTime 不受 Time.timeScale 影响）
/// - 与游戏逻辑解耦，不依赖、不影响其他系统
/// </summary>
public sealed class TopDownCameraController : MonoBehaviour
{
    [Header("固定位置")]
    [Tooltip("摄像机世界坐标，仅在初始化时应用，运行中不每帧重算")]
    [SerializeField] private Vector3 fixedPosition = new Vector3(0f, 20f, 0f);

    [Header("固定俯角")]
    [Tooltip("俯视角度（Pitch），单位：度。60 表示向下看 60°")]
    [SerializeField] private float fixedPitch = 60f;

    [Header("Y 轴旋转（微调）")]
    [SerializeField] private bool enableYawInput = true;
    [Tooltip("绕 Y 轴最大偏转角度（度），左右各 ±maxYaw")]
    [SerializeField] private float maxYaw = 30f;
    [Tooltip("Y 轴旋转速度（度/秒）")]
    [SerializeField] private float yawSpeed = 60f;

    [Header("慢镜头")]
    [Tooltip("启用时，摄像机更新使用 unscaledDeltaTime，不受 Time.timeScale 影响")]
    [SerializeField] private bool useUnscaledTime = false;

    [Header("相机抖动")]
    [SerializeField] private float defaultShakeDuration = 0.2f;
    [SerializeField] private float defaultShakeMagnitude = 0.1f;

    private float _currentYaw;
    private bool _positionApplied;
    private float _shakeTimer;
    private float _shakeDuration;
    private float _shakeMagnitude;
    private Vector3 _shakeOffset;

    private void Awake()
    {
        ApplyFixedTransform();
    }

    private void Start()
    {
        ApplyFixedTransform();
    }

    /// <summary>
    /// 每帧更新
    /// </summary>
    private void LateUpdate()
    {
        if (enableYawInput)
        {
            HandleYawInput();
        }

        // 震动相机
        UpdateShake();

        // 应用位置和旋转
        ApplyPositionOnly();
        ApplyRotationOnly();
    }

    /// <summary>
    /// 仅初始化时设置位置，运行中不再修改。
    /// </summary>
    private void ApplyFixedTransform()
    {
        if (_positionApplied) return;

        _shakeOffset = Vector3.zero;
        transform.position = fixedPosition;
        _currentYaw = 0f;
        ApplyRotationOnly();
        _positionApplied = true;
    }

    private void HandleYawInput()
    {
        if (!Input.GetMouseButton(1)) return;

        float dt = useUnscaledTime ? Time.unscaledDeltaTime : Time.deltaTime;
        float mouseX = Input.GetAxis("Mouse X");
        if (Mathf.Abs(mouseX) < 0.001f) return;

        _currentYaw += mouseX * yawSpeed * dt;
        _currentYaw = Mathf.Clamp(_currentYaw, -maxYaw, maxYaw);
    }

    private void ApplyPositionOnly()
    {
        transform.position = fixedPosition + _shakeOffset;
    }

    /// <summary>
    /// 仅更新旋转，不修改位置。
    /// </summary>
    private void ApplyRotationOnly()
    {
        transform.rotation = Quaternion.Euler(fixedPitch, _currentYaw, 0f);
    }

    private void UpdateShake()
    {
        if (_shakeTimer <= 0f)
        {
            _shakeOffset = Vector3.zero;
            return;
        }

        float dt = useUnscaledTime ? Time.unscaledDeltaTime : Time.deltaTime;
        _shakeTimer -= dt;

        float attenuate = _shakeDuration > 0.0001f ? Mathf.Clamp01(_shakeTimer / _shakeDuration) : 0f;
        float currentMagnitude = _shakeMagnitude * attenuate;

        _shakeOffset = new Vector3(
            Random.Range(-1f, 1f) * currentMagnitude,
            Random.Range(-1f, 1f) * currentMagnitude,
            0f
        );
    }

    #region 公共 API

    /// <summary>
    /// 设置 Y 轴偏转（供外部或慢镜头系统调用）。
    /// </summary>
    public void SetYaw(float yaw)
    {
        _currentYaw = Mathf.Clamp(yaw, -maxYaw, maxYaw);
    }

    /// <summary>
    /// 是否使用 unscaled 时间（慢镜头模式下不受 Time.timeScale 影响）。
    /// </summary>
    public void SetUseUnscaledTime(bool use)
    {
        useUnscaledTime = use;
    }

    /// <summary>
    /// 重置到初始俯视角度（Y 轴归零）。
    /// </summary>
    public void ResetYaw()
    {
        _currentYaw = 0f;
    }

    /// <summary>
    /// 触发一次相机抖动。
    /// </summary>
    public void PlayShake(float duration = -1f, float magnitude = -1f)
    {
        _shakeDuration = duration > 0f ? duration : defaultShakeDuration;
        _shakeMagnitude = magnitude > 0f ? magnitude : defaultShakeMagnitude;
        _shakeTimer = _shakeDuration;
    }

    #endregion
}
