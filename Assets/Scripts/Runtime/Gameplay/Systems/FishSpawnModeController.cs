using UnityEngine;

/// <summary>
/// 保留刷怪组件引用，供 Inspector 绑定与调试查看当前模式。
/// 刷怪驱动（波次 / 循环）由 LevelConfig + LevelManager 负责，不在此开关 autoSpawn。
/// </summary>
public class FishSpawnModeController : MonoBehaviour
{
    [Header("当前模式（运行时由 LevelManager 写入）")]
    [SerializeField] private FishSpawnMode mode = FishSpawnMode.SingleFish;

    [Header("刷怪组件引用")]
    [SerializeField] private FishSpawner fishSpawner;
    [SerializeField] private FishGroupManager fishGroupManager;

    public FishSpawnMode Mode => mode;
    public FishSpawner FishSpawner => fishSpawner;
    public FishGroupManager FishGroupManager => fishGroupManager;

    public void SetMode(FishSpawnMode newMode) => mode = newMode;
}
