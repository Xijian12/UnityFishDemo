public interface IPoolable
{
    void OnSpawn();     // 从池中取出时调用
    void OnRecycle();   // 回收到池中时调用
}
