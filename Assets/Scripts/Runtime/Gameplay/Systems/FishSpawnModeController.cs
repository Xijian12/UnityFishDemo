using UnityEngine;

/// <summary>
/// 统一控制刷怪模式，避免 FishSpawner 与 FishGroupManager 同时生效。
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
    /// 自动绑定组件
    /// </summary>
    private void AutoBindIfNeeded()
    {
        if (fishSpawner == null)
        {
            fishSpawner = FindObjectOfType<FishSpawner>();
        }

        if (fishGroupManager == null)
        {
            fishGroupManager = FindObjectOfType<FishGroupManager>();
        }
    }

    /// <summary>
    /// 应用模式
    /// </summary>
    /// <param name="mode"></param>
    private void ApplyMode()
    {
        if (fishSpawner != null)
        {
            fishSpawner.enabled = (mode == FishSpawnMode.SingleFish);
        }

        if (fishGroupManager != null)
        {
            fishGroupManager.enabled = (mode == FishSpawnMode.FishGroup);
        }
    }
}
