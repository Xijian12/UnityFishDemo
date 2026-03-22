using UnityEngine;

/// <summary>
/// 鱼的三阶贝塞尔曲线移动组件。
/// - 运动仅发生在 XZ 平面，Y 始终进行微小波动
/// - 与帧率无关（deltaTime）
/// - 自动朝向移动方向
/// - 兼容对象池，移动结束后可回收
/// </summary>
[RequireComponent(typeof(Transform))]
public class FishMovement : MonoBehaviour
{
    public static readonly Vector3 XZPlaneNormal = Vector3.up;

    private Vector3 _p0;
    private Vector3 _p1;
    private Vector3 _p2;
    private Vector3 _p3;

    private float _baseY;
    private float _floatAmplitude;
    private float _floatFrequency;
    private float _floatPhase;

    private Vector3 _startPosition;
    private Vector3 _endPosition;
    private Vector3 _controlPoint1;
    private Vector3 _controlPoint2;

    private float _duration;

    /// <summary>
    /// 移动时间
    /// </summary>
    private float _elapsedTime;
    /// <summary>
    /// 移动速度
    /// </summary>
    private float _speed;
    private bool _isActive;

    private Transform _cachedTransform;

    /// <summary>
    /// 移动是否已完成，可用于触发回收。
    /// </summary>
    public bool IsComplete => _isActive && _elapsedTime >= _duration;

    /// <summary>
    /// 当前是否正在移动。
    /// </summary>
    public bool IsActive => _isActive;

    private void Awake()
    {
        _baseY = 0f;
        _floatAmplitude = 0.1f;   // 上下动作幅度
        _floatFrequency = 1.5f;    // 上下动作速度
        _floatPhase = Random.Range(0f, Mathf.PI * 2f); // 随机相位，避免所有鱼同步上下动作
        _cachedTransform = transform;
    }

    /// <summary>
    /// 设置三阶贝塞尔路径并启动移动。
    /// 所有点的 Y 分量会被强制为 0，保证运动在 XZ 平面。
    /// </summary>
    /// <param name="p0">起点</param>
    /// <param name="p1">控制点 1</param>
    /// <param name="p2">控制点 2</param>
    /// <param name="p3">终点</param>
    /// <param name="speed">沿曲线移动的速度（单位/秒）</param>
    public void SetPath(Vector3 p0, Vector3 p1, Vector3 p2, Vector3 p3, float speed)
    {
        _p0 = ForceXZ(p0);
        _p1 = ForceXZ(p1);
        _p2 = ForceXZ(p2);
        _p3 = ForceXZ(p3);
        _speed = Mathf.Max(0.001f, speed);
        _elapsedTime = 0f;
        _isActive = true;

        float length = BezierUtility.GetApproximateLength(_p0, _p1, _p2, _p3);
        _duration = length / _speed;

        _cachedTransform.position = _p0;
        ApplyFacing(0f);
    }

    /// <summary>
    /// 每帧调用，推动移动。与帧率无关。
    /// </summary>
    /// <param name="deltaTime">本帧增量时间</param>
    /// <returns>true 表示仍在移动，false 表示已结束</returns>
    public bool Tick(float deltaTime)
    {
        if (!_isActive) return false;
        if (deltaTime <= 0f) return true;

        _elapsedTime += deltaTime;
        float t = Mathf.Clamp01(_elapsedTime / _duration);

        Vector3 pos = BezierUtility.GetPoint(t, _p0, _p1, _p2, _p3);
        // 主路径仍然在 XZ 上，Y 用泳层 + 浮动控制
        pos.y = _baseY + Mathf.Sin(_elapsedTime * _floatFrequency + _floatPhase) * _floatAmplitude;
    
        _cachedTransform.position = pos;

        ApplyFacing(t);

        if (t >= 1f)
        {
            _isActive = false;
            return false;
        }

        return true;
    }

    /// <summary>
    /// 重置状态，供对象池复用时调用。
    /// </summary>
    public void Reset()
    {
        _isActive = false;
        _elapsedTime = 0f;
    }

    private static Vector3 ForceXZ(Vector3 v)
    {
        v.y = 0f;
        return v;
    }

    private void ApplyFacing(float t)
    {
        Vector3 tangent = BezierUtility.GetTangent(t, _p0, _p1, _p2, _p3);
        tangent.y = 0f;

        if (tangent.sqrMagnitude > 0.0001f)
        {
            _cachedTransform.forward = tangent.normalized;
        }
    }
}
