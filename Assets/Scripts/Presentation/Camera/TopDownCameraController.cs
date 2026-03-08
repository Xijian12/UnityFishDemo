using UnityEngine;

/// <summary>
/// 顶视 3D 捕鱼相机：
/// - 跟随一个 Pivot（例如场景中心或炮台）
/// - 固定 45° 俯视角，可选小范围水平旋转与缩放
/// - 无 GC 分配，输入逻辑可关闭，方便以后接入自定义控制或网络
/// </summary>
public sealed class TopDownCameraController : MonoBehaviour
{
    [Header("跟随目标")]
    [SerializeField] private Transform followTarget;
    [SerializeField] private Vector3 lookAtOffset = Vector3.zero; // 例如 (0, -1.5f, 10f) 看向水体稍微偏前方

    [Header("输入开关")]
    [SerializeField] private bool enableInput = true;
    [SerializeField] private bool allowRotate = true;
    [SerializeField] private bool allowZoom = true;

    [Header("旋转设置")]
    [SerializeField] private float rotateSpeed = 60f;      // 水平旋转速度（度/秒）
    [SerializeField] private float maxYawOffset = 30f;     // 左右最大偏转角度

    [Header("缩放设置")]
    [SerializeField] private float zoomSpeed = 0.2f;       // 缩放速度（鼠标滚轮）
    [SerializeField] private float minZoom = 0.7f;         // 相机距离下限系数
    [SerializeField] private float maxZoom = 1.3f;         // 相机距离上限系数

    // 初始相机相对 followTarget 的球坐标参数
    private float _baseRadius;      // 水平距离
    private float _baseHeight;      // 垂直高度
    private float _baseYaw;         // 初始水平角度（绕 Y 轴）

    // 当前偏移
    private float _currentYawOffset;
    private float _currentZoom = 1f;

    private const float Deg2Rad = Mathf.PI / 180f;

    private void Awake()
    {
        if (followTarget == null)
        {
            return;
        }

        Vector3 offset = transform.position - followTarget.position;

        _baseHeight = offset.y;

        Vector3 horiz = offset;
        horiz.y = 0f;
        float horizMagnitude = horiz.magnitude;

        if (horizMagnitude < 0.01f)
        {
            // 如果初始位置与目标几乎重合，给一个默认的 45° 俯视位置
            _baseRadius = 20f;
            _baseHeight = 20f;
            _baseYaw = 0f;
        }
        else
        {
            _baseRadius = horizMagnitude;
            _baseYaw = Mathf.Atan2(horiz.x, horiz.z) * Mathf.Rad2Deg;
        }

        _currentYawOffset = 0f;
        _currentZoom = 1f;
    }

    private void LateUpdate()
    {
        if (followTarget == null)
        {
            return;
        }

        if (enableInput)
        {
            HandleInput();
        }

        UpdateCameraTransform();
    }

    private void HandleInput()
    {
        if (allowRotate && Input.GetMouseButton(1))
        {
            float mouseX = Input.GetAxis("Mouse X");
            if (mouseX != 0f)
            {
                _currentYawOffset += mouseX * rotateSpeed * Time.unscaledDeltaTime;
                _currentYawOffset = Mathf.Clamp(_currentYawOffset, -maxYawOffset, maxYawOffset);
            }
        }

        if (allowZoom)
        {
            float scroll = Input.mouseScrollDelta.y;
            if (scroll != 0f)
            {
                _currentZoom -= scroll * zoomSpeed;
                _currentZoom = Mathf.Clamp(_currentZoom, minZoom, maxZoom);
            }
        }
    }

    private void UpdateCameraTransform()
    {
        float yaw = _baseYaw + _currentYawOffset;
        float radius = _baseRadius * _currentZoom;
        float height = _baseHeight * _currentZoom;

        float yawRad = yaw * Deg2Rad;

        // 计算水平位置（绕 Y 轴旋转）
        float x = Mathf.Sin(yawRad) * radius;
        float z = Mathf.Cos(yawRad) * radius;

        Vector3 targetPos = followTarget.position;
        Vector3 camPos = new Vector3(targetPos.x + x, targetPos.y + height, targetPos.z + z);

        transform.position = camPos;

        // 看向目标点（可添加偏移，让视线略微落在水体前方）
        Vector3 lookTarget = targetPos + lookAtOffset;
        Vector3 forward = lookTarget - camPos;

        if (forward.sqrMagnitude > 0.0001f)
        {
            transform.rotation = Quaternion.LookRotation(forward.normalized, Vector3.up);
        }
    }

    // 供外部系统（例如网络或关卡脚本）直接控制相机状态的 API
    public void SetYawOffset(float yawOffset)
    {
        _currentYawOffset = Mathf.Clamp(yawOffset, -maxYawOffset, maxYawOffset);
    }

    public void SetZoomFactor(float zoomFactor)
    {
        _currentZoom = Mathf.Clamp(zoomFactor, minZoom, maxZoom);
    }

    public void SetFollowTarget(Transform target)
    {
        followTarget = target;
        Awake(); // 重新计算基准参数（本脚本无复杂状态，直接复用初始化逻辑）
    }
}

