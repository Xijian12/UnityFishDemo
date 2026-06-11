using UnityEngine;

/// <summary>
/// 统一控制刷怪模式：仅用 autoSpawn 开关区分单鱼/鱼群定时刷怪。
/// 禁止禁用 FishSpawner / FishGroupManager 组件本身——否则 Start 不执行，
/// FishSpawner 无法加载 FishDatabase（IsReady 永远 false），关卡会一直卡在等待。
/// </summary>
public class FishSpawnModeController : MonoBehaviour
{
    [Header("模式")]
    [SerializeField] private FishSpawnMode mode = FishSpawnMode.SingleFish;

    [Header("刷怪组件引用")]
    [SerializeField] private FishSpawner fishSpawner;
    [SerializeField] private FishGroupManager fishGroupManager;

    private void Awake()
    {
        AutoBindIfNeeded();
        ApplyMode();
    }

    /// <summary>
    /// 验证时自动绑定组件
    /// </summary>
    private void OnValidate()
    {
        AutoBindIfNeeded();
        ApplyMode();
    }

    /// <summary>
    /// 运行时切换模式（可给按钮/调试面板调用）。
    /// </summary>
    public void SetMode(FishSpawnMode newMode)
    {
        mode = newMode;
        ApplyMode();
    }

    /// <summary>
    /// 非关卡场景：当前模式一侧允许自动刷怪，另一侧关闭。
    /// 关卡场景：LevelManager 应在 SetMode 之后再次关闭两侧自动刷怪。
    /// </summary>
    private void ApplyMode()
    {
        if (fishSpawner != null)
            fishSpawner.SetAutoSpawnEnabled(mode == FishSpawnMode.SingleFish);

        if (fishGroupManager != null)
            fishGroupManager.SetAutoSpawnEnabled(mode == FishSpawnMode.FishGroup);
    }

    private void AutoBindIfNeeded()
    {
        // 不在运行时用 Find：请在 Inspector 绑定 FishSpawner / FishGroupManager。
    }
}
