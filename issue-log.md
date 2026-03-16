# FishingDemo Issue Log

## 1. 项目定位与玩法认知

### 问题
- 早期对玩法理解存在偏差：是否为传统抛竿钓鱼流程。

### 根因
- 代码实现实际上是“子弹命中鱼 -> 扣血 -> 死亡得分”的捕鱼玩法。

### 解决
- 全局扫描脚本后明确主循环：
  - `BulletSpawner/CannonController` 负责发射
  - `Bullet` 负责命中判定
  - `Fish` 负责受伤死亡
  - `ScoreManager/UIFxManager` 负责得分与反馈

### 状态
- 已确认，后续按捕鱼方向继续迭代。

---

## 2. Bullet 更新管理分散

### 问题
- `Bullet` 自己 `Update`，与 `FishManager` 风格不一致，不利于统一性能控制。

### 根因
- 缺少类似鱼系统的集中调度。

### 解决
- 引入 `BulletManager` 统一管理活跃子弹。
- `Bullet` 改为 `ManualUpdate(deltaTime)`。
- 通过对象池生命周期（`OnSpawn/OnRecycle`）维护活跃列表。

### 状态
- 已完成并通过 lints。

---

## 3. 2D 到 3D 迁移方向不清晰

### 问题
- 不确定是否要“改项目模板”才能做 3D。

### 根因
- 对 Unity 模板与场景能力边界认知不清。

### 解决
- 结论：无需换模板，直接在当前项目创建 3D 场景即可。
- 确定相机方案从 FPS 调整为“水面俯视 45 度”。

### 状态
- 路线确定，按 3D 捕鱼推进。

---

## 4. 相机每帧重算导致画面不稳定

### 问题
- 主摄像机位置和朝向每帧被动态计算，画面抖动/漂移。

### 根因
- 相机脚本把“固定相机”写成了“持续跟随求解”。

### 解决
- 重写 `TopDownCameraController`：
  - 固定位置 `(0,20,0)`
  - 固定俯角（默认 45）
  - 仅允许小范围 Y 轴微调
  - 支持慢镜头时序（`unscaledDeltaTime`）

### 状态
- 已完成。

---

## 5. 鱼移动不是三阶贝塞尔

### 问题
- 鱼在 XZ 平面接近直线，不符合预期曲线。

### 根因
- 原逻辑为简化路径或二次曲线，控制点策略不满足需求。

### 解决
- 新增 `BezierUtility`（三阶曲线点、切线、长度近似）。
- 新增 `FishMovement`（XZ 平面移动、Y 固定 0、自动朝向、deltaTime 无关）。
- `Fish` 与 `FishSpawner` 改为四点初始化（`p0/p1/p2/p3`）。

### 状态
- 已完成，鱼路径系统模块化。

---

## 6. 子弹发射架构冲突（BulletSpawner vs CannonController）

### 问题
- 两个脚本同时参与“是否发射/方向计算/发射执行”，逻辑冲突。

### 根因
- 职责边界不清，入口不唯一。

### 解决
- 职责收敛：
  - `CannonController`：输入、发射模式、方向计算、发射时机
  - `BulletSpawner`：从池取子弹并初始化参数（唯一发射执行器）
- 明确唯一调用链：`CannonController -> BulletSpawner.Fire() -> Bullet.Init()`

### 状态
- 已完成。

---

## 7. 对象池复用后鱼变白

### 问题
- 鱼受击闪白后回收，再次取出仍保持白色；运行越久白鱼越多。

### 根因
- 回收/取出时未完整重置视觉状态。
- 受击协程在回收时未彻底管理。
- 颜色恢复逻辑依赖“当前色”而非“默认色”。

### 解决
- 在鱼生命周期中加入显示状态重置：
  - `OnSpawn` 恢复默认颜色和 alpha
  - `OnRecycle` 停止协程并清理视觉状态
- 缓存默认颜色，避免残留态累积。

### 状态
- 已修复并通过 lints。

---

## 8. 报错：inactive object 启动协程失败

### 现象
- `Coroutine couldn't be started because the game object is inactive`
- 调用链涉及 `Fish.TakeDamage -> StartCoroutine(...)`。

### 根因
- 子弹命中时命中的鱼对象可能已进入回收流程（inactive），但仍触发了受击协程启动。

### 处理方向
- 在 `TakeDamage` 前置校验：
  - `gameObject.activeInHierarchy`
  - `IsDead/isDying`
- 回收时先解除活跃列表，再停止协程，避免被命中遍历命中到无效对象。

### 状态
- 已完成问题定位并纳入后续防御式校验策略。

---

## 9. 金币 UI 可激活但不可见

### 问题
- Hierarchy 中金币对象激活，但场景/游戏中看不到。

### 根因
- 出现过“3D 金币模型”和“2D UI 坐标系动画”混用。
- 以及 UI 绑定、父节点、坐标系不一致导致的可见性问题。

### 解决
- 统一决策：当前阶段全部使用 **2D UI 坐标系金币**。
- `UIFxManager` 增加 UI 绑定校验与防呆：
  - 强制 root canvas
  - 取出对象后强制挂到目标 canvas
- `CoinUIFx` 强制 `RectTransform`（UI 专用）。

### 状态
- 已确定方案并已实施代码收敛。

---

## 10. 金币到达事件改造（静态事件 -> 事件总线）

### 问题
- 原先 `CoinUIFx.OnArrived` 静态事件与全局总线并存，事件链不统一。

### 根因
- 事件入口风格不一致，增加维护复杂度。

### 解决
- 统一改为事件总线链路：
  - `CoinUIFx` 到达后触发“到达通知”
  - `UIFxManager` 统一发布 `CoinArrivedEvent`
  - `UIFxManager` 订阅 `CoinArrivedEvent` 生成 `+score` 浮字

### 状态
- 已完成重构。

---

## 当前架构决策（阶段性）

- 普通金币：**2D UI 坐标系**（稳定、易适配、低维护成本）
- 特殊金币（后续）：预留 **3D 世界特效分支**（如慢镜头、稀有奖励）
- 事件流：
  - `FishKilledEvent` -> 金币飞向计分板
  - `CoinArrivedEvent` -> 计分板右侧显示浮动 `+score`

---

## 后续建议（下一步）

1. 为金币/浮字增加调试开关（显示起点、终点、当前坐标）。
2. 给 UI Fx 增加统一配置资产（时长、偏移、池容量）。
3. 增加“普通金币 / 特殊金币”分发字段，避免后续改动牵动主链路。
4. 补一份玩法事件流文档（事件名、触发点、消费者）。

---

## 11. 编译报错：`EventBusClass` 命名空间不可见

### 问题
- `CoinUIFx.cs` 出现编译错误：`The type or namespace name 'EventBusClass' could not be found...`。

### 根因
- UI 特效脚本直接依赖事件总线实现细节，导致命名空间/程序集引用脆弱。
- 视觉层与事件发布层耦合过深。

### 解决
- 取消 `CoinUIFx` 对 `EventBusClass` 的直接依赖。
- 改为 `CoinUIFx -> UIFxManager.PublishCoinArrived(...) -> EventBusClass.Publish(...)`。
- 由 `UIFxManager` 统一承接事件发布。

### 状态
- 已修复并通过编译。

---

## 12. 鱼对象生命周期职责混乱（`Fish` vs `FishCombat` vs `FishVisual`）

### 问题
- 鱼对象拆分后，生命状态、事件发布、视觉协程管理边界不清，出现逻辑分散与重复状态风险。

### 根因
- 聚合对象与子模块职责没有严格分层。

### 解决
- `FishCombat`：仅维护 HP 与死亡状态（`IsDead/IsDying`、`TryEnterDeath`）。
- `FishVisual`：仅负责受击/死亡视觉表现与复用重置。
- `Fish`：作为编排层，统一受伤流程、协程生命周期、事件发布和安全回收。
- `Fish.IsDead` 改为转发 `_fishCombat.IsDead`，避免双份状态。

### 状态
- 已完成重构，职责边界清晰。

---

## 13. `ScoreManager` 遗留抖动逻辑与相机耦合

### 问题
- `ScoreManager` 中残留了相机抖动和相机引用，UI 管理脚本跨层做了相机控制。

### 根因
- 表现层职责混杂，缺少相机能力归口。

### 解决
- 将抖动能力迁移到 `TopDownCameraController.PlayShake()`。
- `ScoreManager` 仅触发效果，不再持有旧的抖动协程实现。

### 状态
- 已完成清理。

---

## 14. 目录分层与命名不统一

### 问题
- `Core/Gameplay/Presentation/Infrastructure` 混杂，路径层级和命名风格不一致，后续维护成本高。

### 根因
- 迭代中逐步扩展，缺少统一分层约束。

### 解决
- 统一迁移到 `Assets/Scripts/Runtime` 分层：
  - `Runtime/Gameplay`
  - `Runtime/Presentation`
  - `Runtime/Infrastructure`
  - `Runtime/Shared`
- 迁移时同步移动 `.meta`，保证 GUID 稳定，避免场景/Prefab 引用断裂。

### 状态
- 已完成脚本与 `.meta` 迁移。

---

## 15. 切换鱼群模式后无鱼生成

### 问题
- 切到鱼群模式后没有任何鱼群出现。

### 根因
- 场景中 `FishGroupManager.groupConfigs` 为空（`groupConfigs: []`）。
- 项目中未创建/未绑定 `FishGroupConfig` 资源。
- `FishSpawnModeController` 未挂载到场景时，模式切换不生效。

### 解决
- 新增 `FishSpawnModeController` 与 `FishSpawnMode`，统一控制单鱼/鱼群二选一启停。
- 在 `FishGroupConfig` 增加鱼群配置字段和 `spawnWaveCount`。
- 在 `FishGroupManager` 增加固定波次生成计划（`remainWaves`）。

### 状态
- 代码链路已完成，需在场景中正确挂载并绑定配置资产。

---

## 16. 鱼群阵型方向错误（直线/V 字反向）

### 问题
- 直线阵列与 V 字阵列朝向与移动方向不一致，出现“阵型反了”。

### 根因
- 阵型局部坐标定义与“前进轴”约定不统一。
- V 字形展开轴与头部朝向关系错误。

### 解决
- 统一约定：`local +Z` 为前进方向。
- 线阵改为按前进轴定义，再通过组旋转映射到世界方向。
- V 字改为“鱼头在原点、两翼向 `local -Z`（后方）展开”。

### 状态
- 已修复，阵型朝向与移动方向一致。

---

## 17. 鱼群移动出现卡顿/闪烁

### 问题
- 鱼群轨迹视觉不平滑，出现抖动和闪烁感。

### 根因
- `FishManager` 采用隔帧更新（每帧只更新一半鱼，`deltaTime * 2` 补偿），造成可见抖动。

### 解决
- 改为每帧更新所有活鱼：`fish.ManualUpdate(Time.deltaTime)`。

### 状态
- 已修复，轨迹平滑度提升。

---

## 18. 鱼群“整群同时消失”观感异常

### 问题
- 看起来像鱼头出屏后整群同时消失，缺少前后排错峰离场。

### 根因
- 虽然是逐条回收，但所有鱼共享同节奏路径（同帧起跑、近似同路径长度、同速），导致回收时刻几乎一致。

### 解决
- 为 `FishMovement.SetPath` 增加 `startDelay`。
- `FishGroup` 根据槽位后向深度分配延迟（后排延迟更大）。
- `FishGroupConfig` 增加：
  - `baseStartDelay`
  - `startDelayPerMeter`
  - `maxStartDelay`

### 状态
- 已完成，鱼群具备错峰起跑与错峰离场效果。

---

## 当前架构补充（鱼群系统）

- 刷怪模式统一入口：
  - `SingleFish`：`FishSpawner`
  - `FishGroup`：`FishGroupManager`
- 鱼群数量由配置驱动：
  - `groupCount`：单群鱼数
  - `spawnWaveCount`：群波次数
- 鱼群内部时序：
  - 槽位偏移决定空间阵型
  - 起跑延迟决定时间阵型

