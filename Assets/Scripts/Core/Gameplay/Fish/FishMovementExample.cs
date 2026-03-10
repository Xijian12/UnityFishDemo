using UnityEngine;

/// <summary>
/// 三阶贝塞尔控制点设置示例。
/// 展示如何为 FishMovement 设置 P0、P1、P2、P3 四个点。
/// 运动仅发生在 XZ 平面，Y 始终为 0。
/// </summary>
public static class FishMovementExample
{
    /// <summary>
    /// 示例 1：左→右的 S 形曲线。
    /// P0 在左侧，P3 在右侧；P1、P2 在连线两侧，形成 S 形。
    /// </summary>
    public static void Example_SCurve()
    {
        Vector3 p0 = new Vector3(-15f, 0f, 15f);  // 起点：左侧
        Vector3 p3 = new Vector3(15f, 0f, 25f);   // 终点：右侧
        Vector3 mid = (p0 + p3) * 0.5f;
        Vector3 dir = (p3 - p0).normalized;
        Vector3 perp = Vector3.Cross(dir, Vector3.up);

        Vector3 p1 = mid + perp * 5f;   // 控制点 1：连线一侧
        p1.y = 0f;
        Vector3 p2 = mid - perp * 4f;   // 控制点 2：连线另一侧
        p2.y = 0f;

        // fish.Init(config, p0, p1, p2, p3);
    }

    /// <summary>
    /// 示例 2：弧形路径。
    /// P1 靠近 P0 并向外偏移，P2 靠近 P3 并向内收。
    /// </summary>
    public static void Example_ArcCurve()
    {
        Vector3 p0 = new Vector3(-12f, 0f, 10f);
        Vector3 p3 = new Vector3(12f, 0f, 30f);
        Vector3 dir = (p3 - p0).normalized;
        Vector3 perp = Vector3.Cross(dir, Vector3.up);
        float dist = Vector3.Distance(p0, p3);

        Vector3 p1 = p0 + dir * (dist * 0.33f) + perp * 6f;
        p1.y = 0f;
        Vector3 p2 = p3 - dir * (dist * 0.33f) + perp * 4f;
        p2.y = 0f;

        // fish.Init(config, p0, p1, p2, p3);
    }

    /// <summary>
    /// 示例 3：简单弧线（单侧弯曲）。
    /// P1、P2 都在同一侧，形成单向弧。
    /// </summary>
    public static void Example_SimpleArc()
    {
        Vector3 p0 = new Vector3(-10f, 0f, 20f);
        Vector3 p3 = new Vector3(10f, 0f, 20f);
        Vector3 mid = (p0 + p3) * 0.5f;
        Vector3 perp = Vector3.Cross((p3 - p0).normalized, Vector3.up);

        Vector3 p1 = mid + perp * 8f;
        p1.y = 0f;
        Vector3 p2 = mid + perp * 6f;
        p2.y = 0f;

        // fish.Init(config, p0, p1, p2, p3);
    }

    /// <summary>
    /// 示例 4：随机控制点（用于 FishSpawner）。
    /// </summary>
    public static void GetRandomControlPoints(Vector3 p0, Vector3 p3, float curveStrength, out Vector3 p1, out Vector3 p2)
    {
        Vector3 mid = (p0 + p3) * 0.5f;
        Vector3 perp = Vector3.Cross((p3 - p0).normalized, Vector3.up);
        float offset = Random.Range(-curveStrength, curveStrength);

        p1 = mid + perp * offset;
        p1.y = 0f;
        p2 = mid - perp * offset * 0.7f;
        p2.y = 0f;
    }
}
