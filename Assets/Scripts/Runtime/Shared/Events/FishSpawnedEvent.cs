/// <summary>
/// 鱼从对象池取出并完成 OnSpawn 后发布（供 FishManager 等订阅，Fish 不直接依赖管理器）。
/// </summary>
public class FishSpawnedEvent
{
    public Fish Fish;
}
