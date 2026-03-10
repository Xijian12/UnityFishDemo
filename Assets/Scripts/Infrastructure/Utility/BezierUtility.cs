using UnityEngine;

/// <summary>
/// 三阶贝塞尔曲线工具类。
/// B(t) = (1-t)³·P0 + 3(1-t)²t·P1 + 3(1-t)t²·P2 + t³·P3，t ∈ [0,1]
/// </summary>
public static class BezierUtility
{
    /// <summary>
    /// 获取三阶贝塞尔曲线在参数 t 处的点。
    /// </summary>
    /// <param name="t">参数，范围 [0, 1]</param>
    /// <param name="p0">起点</param>
    /// <param name="p1">控制点 1</param>
    /// <param name="p2">控制点 2</param>
    /// <param name="p3">终点</param>
    public static Vector3 GetPoint(float t, Vector3 p0, Vector3 p1, Vector3 p2, Vector3 p3)
    {
        float u = 1f - t;
        float u2 = u * u;
        float u3 = u2 * u;
        float t2 = t * t;
        float t3 = t2 * t;

        return u3 * p0 + 3f * u2 * t * p1 + 3f * u * t2 * p2 + t3 * p3;
    }

    /// <summary>
    /// 获取三阶贝塞尔曲线在参数 t 处的一阶导数（切线方向）。
    /// B'(t) = 3(1-t)²(P1-P0) + 6(1-t)t(P2-P1) + 3t²(P3-P2)
    /// </summary>
    public static Vector3 GetTangent(float t, Vector3 p0, Vector3 p1, Vector3 p2, Vector3 p3)
    {
        float u = 1f - t;
        float u2 = u * u;
        float t2 = t * t;

        return 3f * u2 * (p1 - p0) + 6f * u * t * (p2 - p1) + 3f * t2 * (p3 - p2);
    }

    /// <summary>
    /// 计算三阶贝塞尔曲线近似弧长（通过采样累加弦长）。
    /// </summary>
    /// <param name="samples">采样数量，越大越精确</param>
    public static float GetApproximateLength(Vector3 p0, Vector3 p1, Vector3 p2, Vector3 p3, int samples = 32)
    {
        if (samples < 2) samples = 2;

        float length = 0f;
        Vector3 prev = p0;

        for (int i = 1; i <= samples; i++)
        {
            float t = (float)i / samples;
            Vector3 curr = GetPoint(t, p0, p1, p2, p3);
            length += Vector3.Distance(prev, curr);
            prev = curr;
        }

        return length;
    }
}
