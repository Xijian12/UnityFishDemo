/// <summary>
/// 关卡刷怪驱动方式（由 LevelConfig + LevelManager 统一调度）。
/// </summary>
public enum LevelSpawnDriveMode
{
    /// <summary>仅按 WaveConfig 时间点刷怪。</summary>
    Wave = 0,

    /// <summary>关卡进行中按间隔循环刷怪（单鱼/鱼群由 levelSpawnMode 决定）。</summary>
    Continuous = 1
}
