using UnityEngine;

/// <summary>
/// 固定俯视摄像机。
/// - 位置固定 (0, 20, 0)，不每帧重算
/// - 俯角固定 60°，仅允许绕 Y 轴小范围旋转
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

    private float _currentYaw;
    private bool _positionApplied;

    private void Awake()
    {
        ApplyFixedTransform();
    }

    private void Start()
    {
        ApplyFixedTransform();
    }

    private void LateUpdate()
    {
        if (enableYawInput)
        {
            HandleYawInput();
        }

        ApplyRotationOnly();
    }

    /// <summary>
    /// 仅初始化时设置位置，运行中不再修改。
    /// </summary>
    private void ApplyFixedTransform()
    {
        if (_positionApplied) return;

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

    /// <summary>
    /// 仅更新旋转，不修改位置。
    /// </summary>
    private void ApplyRotationOnly()
    {
        transform.rotation = Quaternion.Euler(fixedPitch, _currentYaw, 0f);
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

    #endregion
}
