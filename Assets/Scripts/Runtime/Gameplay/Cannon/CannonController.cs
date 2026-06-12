using UnityEngine;

/// <summary>
/// 炮台控制器：决定是否发射、发射方向/目标，并调用 BulletSpawner 执行发射。
/// 不直接操作对象池，不负责子弹初始化。
/// </summary>
public class CannonController : MonoBehaviour
{
    public enum FireMode
    {
        Manual,   // 鼠标手动瞄准，点击切换持续开火
        Auto      // 自动朝最近鱼发射
    }

    [Header("炮台")]
    [SerializeField] private Transform muzzlePoint;

    [Header("发射")]
    [SerializeField] private BulletSpawner bulletSpawner;
    [SerializeField] private FireMode fireMode = FireMode.Manual;
    [SerializeField] private float fireInterval = 0.3f;
    [SerializeField] private BulletType currentBulletType = BulletType.SmallBullet;
    [SerializeField] private CanonType currentCanonType = CanonType.Single;

    [Header("自动追踪的最大距离")]
    [SerializeField] private float maxTrackDistance = 50f;

    private float _lastFireTime;
    private bool _isManualFiring;

    private void Update()
    {
        if (Time.timeScale <= 0f) return;

        if (UIInputGuard.ShouldBlockGameplayInput())
        {
            _isManualFiring = false;
            return;
        }

        UpdateAimVisual();
        HandleManualInput();

        if (!ShouldFire()) return;
        if (Time.time < _lastFireTime + fireInterval) return;

        Vector3 spawnPos = GetMuzzlePosition();
        Vector3 dir = GetFireDirection(spawnPos);

        if (dir.sqrMagnitude < 0.0001f)
        {
            if (fireMode == FireMode.Auto) return;
            dir = Vector3.forward;
        }

        if (bulletSpawner != null && bulletSpawner.Fire(spawnPos, dir, currentBulletType))
        {
            _lastFireTime = Time.time;
        }
    }

    /// <summary>
    /// 处理手动模式输入：
    /// 左键点击一次开始持续开火，再点一次停止。
    /// </summary>
    private void HandleManualInput()
    {
        if (fireMode != FireMode.Manual) return;
        if (UIInputGuard.IsPointerOverUI()) return;

        if (Input.GetMouseButtonDown(0))
            _isManualFiring = !_isManualFiring;
    }

    private bool ShouldFire()
    {
        return fireMode == FireMode.Manual
            ? _isManualFiring
            : true;
    }

    /// <summary>
    /// 获取炮口位置
    /// </summary>
    /// <returns>炮口位置</returns>
    private Vector3 GetMuzzlePosition()
    {
        if (muzzlePoint != null)
            return muzzlePoint.position;

        return transform.position;
    }

    private Vector3 GetFireDirection(Vector3 muzzlePos)
    {
        return fireMode == FireMode.Manual
            ? GetDirectionManual(muzzlePos)
            : GetDirectionAuto(muzzlePos);
    }

    /// <summary>
    /// 手动模式下，根据鼠标位置计算发射方向
    /// </summary>
    /// <param name="muzzlePos">炮口位置</param>
    /// <returns>发射方向</returns>
    private Vector3 GetDirectionManual(Vector3 muzzlePos)
    {
        Camera cam = Camera.main;
        if (cam == null) return Vector3.forward;

        Plane xzPlane = new Plane(Vector3.up, Vector3.zero);
        Ray ray = cam.ScreenPointToRay(Input.mousePosition);

        if (!xzPlane.Raycast(ray, out float enter))
            return Vector3.forward;

        Vector3 mouseWorld = ray.GetPoint(enter);
        mouseWorld.y = muzzlePos.y;

        Vector3 dir = mouseWorld - muzzlePos;
        dir.y = 0f;

        if (dir.sqrMagnitude < 0.0001f)
            return Vector3.forward;

        return dir.normalized;
    }

    /// <summary>
    /// 自动模式下，根据最近鱼的位置计算发射方向
    /// </summary>
    /// <param name="muzzlePos">炮口位置</param>
    /// <returns>发射方向</returns>
    private Vector3 GetDirectionAuto(Vector3 muzzlePos)
    {
        Fish nearest = FishManager.Instance?.GetNearestFish(muzzlePos, maxTrackDistance);
        if (nearest == null) return Vector3.forward;

        Vector3 targetPos = nearest.transform.position;
        targetPos.y = muzzlePos.y;

        Vector3 dir = targetPos - muzzlePos;
        dir.y = 0f;

        if (dir.sqrMagnitude < 0.0001f)
            return Vector3.forward;

        return dir.normalized;
    }

    /// <summary>
    /// 设置发射模式
    /// </summary>
    /// <param name="mode">发射模式</param>
    public void SetFireMode(FireMode mode)
    {
        fireMode = mode;

        if (fireMode != FireMode.Manual)
        {
            _isManualFiring = false;
        }
    }

    /// <summary>
    /// 更新瞄准视觉
    /// </summary>
    private void UpdateAimVisual()
    {
        if (fireMode != FireMode.Manual) return;

        Vector3 muzzlePos = GetMuzzlePosition();
        Vector3 dir = GetDirectionManual(muzzlePos);

        if (dir.sqrMagnitude < 0.0001f) return;

        transform.forward = dir;
    }

    /// <summary>
    /// 设置子弹类型
    /// </summary>
    /// <param name="type">子弹类型</param>
    public void SetBulletType(BulletType type) => currentBulletType = type;

    public CanonType CurrentCanonType => currentCanonType;

    public void SetCanonType(CanonType type) => currentCanonType = type;

    public void StopManualFiring() => _isManualFiring = false;

    /// <summary>
    /// 是否正在手动发射
    /// </summary>
    /// <returns>是否正在手动发射</returns>
    public bool IsManualFiring => _isManualFiring;
}