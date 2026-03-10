# 工程问题排查（UnityFishDemo）

本报告基于当前项目源码做静态审查，聚焦“稳定性、可维护性、性能和架构一致性”风险。

## 1. 单例初始化不一致，存在重复实例与场景切换行为不确定
- `FishManager`、`BulletManager`、`PoolManager` 仅在 `Awake` 中直接覆盖 `Instance`，没有重复实例保护；若场景中误放多个对象，后创建对象会静默覆盖全局引用。 
- `ScoreManager` 使用了 `DontDestroyOnLoad` + 判重，和其他 Manager 生命周期策略不一致，增加跨场景初始化顺序问题。

**建议**：统一单例模板（判重、日志、可选跨场景常驻策略），避免“部分常驻、部分非常驻”导致的隐性依赖。

## 2. Update 循环内修改集合，可能导致元素漏更新
- `BulletManager.Update()` 正向遍历 `ActiveBullets`，而 `Bullet.ManualUpdate()` 在命中/超距时会 `Release`，触发 `OnBulletRecycle` 并在同帧 `RemoveBullet`。这会在遍历过程中修改列表，可能导致后续元素跳过更新。
- `FishManager.Update()` 也在遍历 `ActiveFish` 时调用 `ManualUpdate`，鱼离场后会从列表移除，存在同类风险。

**建议**：改为倒序遍历，或先复制快照再遍历，或延迟移除队列（本帧结束统一处理）。

## 3. 空引用防护不足，存在运行时 NRE 风险
- `FishManager.Update()` 对 `ActiveFish[i]` 未做空判断就调用 `ManualUpdate`。
- `PoolManager.Get<T>()` 中，字典存在键但泛型类型不匹配时 `as ObjectPool<T>` 会得到 `null`，随后 `pool.Get()` 直接触发 NRE。

**建议**：关键热路径增加低成本空值保护和类型校验日志，防止生产环境硬崩。

## 4. Addressables 资源释放策略较粗，可能造成句柄管理问题
- `FishSpawner.LoadDatabase()` 中使用 `LoadAssetAsync` 获得句柄，仅在 `OnDestroy` 调用了 `fishDatabaseRef.ReleaseAsset()`，没有显式持有并释放 `AsyncOperationHandle`。

**建议**：保存并校验 handle 生命周期（是否有效、是否已释放），统一采用 handle 级释放策略，减少资源泄漏与重复释放风险。

## 5. Pool 生命周期边界不够健壮
- `ObjectPool.Release()` 未校验重复回收（同对象被多次 `Release` 会重复入队）。
- `Bullet.ManualUpdate()` 在 `config == null` 时直接 `SetActive(false)`，绕过 `PoolManager.Release`，可能造成管理器状态与池状态不一致。

**建议**：为池对象引入“inPool”状态位或 HashSet 防重；统一“失效对象必须走 Release”规则。

## 6. 硬编码参数较多，调优成本高
- 鱼活动边界、碰撞半径、特效时长、连击窗口等散落在多个脚本常量或 magic number 中。

**建议**：迁移到 ScriptableObject 或集中配置模块，按“玩法参数 / 表现参数”分层，便于策划与开发协作。

## 7. 职责边界有耦合：表现层与逻辑层互相调用
- `Fish.DieRoutine()` 直接触发 `ScoreManager.Instance.SpawnCoinFromWorld`，玩法对象直接依赖 UI/表现管理器。

**建议**：通过事件总线或应用服务层中转（如 `FishKilledEvent`），让 Fish 只负责发事件，UI 系统独立订阅处理。

## 8. 部分 API 使用不一致，增加维护噪音
- `ScoreManager` 已缓存 `mainCamera`，但 `SpawnCoinFromWorld` 仍使用 `Camera.main`。

**建议**：统一使用注入或缓存的相机引用，避免 tag 查找和多相机场景下目标不一致。

---

## 优先级建议（从高到低）
1. **高优先**：修复 Update 期间集合修改、空引用防护（直接影响稳定性）。
2. **中优先**：统一单例生命周期与 Pool 状态机（降低线上不可预期行为）。
3. **中优先**：Addressables handle 生命周期规范化（防资源泄漏）。
4. **低优先**：参数配置化与解耦优化（提升长期维护效率）。
