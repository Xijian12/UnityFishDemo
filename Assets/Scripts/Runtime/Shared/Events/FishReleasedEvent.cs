/// <summary>
/// 鱼回收到对象池前发布（OnRecycle 入口，供 FishManager 等订阅）。
/// </summary>
public class FishReleasedEvent
{
    public Fish Fish;
}
