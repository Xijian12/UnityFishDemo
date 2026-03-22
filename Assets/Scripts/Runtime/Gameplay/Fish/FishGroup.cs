using UnityEngine;
using System.Collections.Generic;

public class FishGroup{
    private readonly FishGroupConfig _config;
    private readonly int _groupID;
    private readonly List<FishGroupMember> _members = new List<FishGroupMember>(16);
    private readonly List<Vector3> _slotOffsets = new List<Vector3>(16);

    private Vector3 _groupP0;
    private Vector3 _groupP1;
    private Vector3 _groupP2;
    private Vector3 _groupP3;
    private Vector3 _exitDirection;

    private float _recycleMinX;
    private float _recycleMaxX;
    private float _recycleMinZ;
    private float _recycleMaxZ;

    public FishGroup(FishGroupConfig config, int groupID)
    {
        _config = config;
        _groupID = groupID;
        if (_config != null)
        {
            FormationCalculator.CalculateFormationPosition(_config.formationType, _config, _slotOffsets);
        }
    }

    public int GroupID => _groupID;
    public bool IsFinished => _members.Count == 0;

    /// <summary>
    /// 使用“组路径 + 槽位偏移”生成一组鱼。
    /// 每条鱼得到独立贝塞尔路径：groupPath + slotOffset。
    /// </summary>
    public bool Spawn(
        Vector3 groupP0,
        Vector3 groupP1,
        Vector3 groupP2,
        Vector3 groupP3,
        float battleMinX,
        float battleMaxX,
        float battleMinZ,
        float battleMaxZ,
        float recyclePadding)
    {
        if (_config == null || _config.fishConfig == null || _config.fishConfig.prefab == null)
        {
            Debug.LogWarning("FishGroup Spawn failed: config/fishConfig/prefab is null.");
            return false;
        }

        if (_slotOffsets.Count == 0)
        {
            FormationCalculator.CalculateFormationPosition(_config.formationType, _config, _slotOffsets);
        }
        if (_slotOffsets.Count == 0) return false;

        _members.Clear();
        CacheGroupPath(groupP0, groupP1, groupP2, groupP3);
        CacheRecycleBounds(battleMinX, battleMaxX, battleMinZ, battleMaxZ, recyclePadding);

        // 4. 计算鱼群整体朝向 (forward vector)
        Vector3 forward = groupP3 - groupP0; // 从起点指向终点
        if (forward.sqrMagnitude < 0.0001f) // 如果 P0 和 P3 太近，方向不确定
        {
            forward = DirectionToVector(_config.groupDirection); // 使用配置的方向
        }
        forward.y = 0f; // 强制方向在 XZ 平面（水平面）
        if (forward.sqrMagnitude < 0.0001f) forward = Vector3.right; // 如果水平方向也无效，使用默认方向

        Vector3 endTangent = BezierUtility.GetTangent(1f, _groupP0, _groupP1, _groupP2, _groupP3);
        endTangent.y = 0f;
        _exitDirection = endTangent.sqrMagnitude > 0.0001f ? endTangent.normalized : forward.normalized;

        Quaternion rot = Quaternion.LookRotation(forward.normalized, Vector3.up);
        // 这个旋转用于将原始的队形偏移量转换到与鱼群路径方向一致的空间中。

        // 6. 循环生成每条鱼
        for (int i = 0; i < _slotOffsets.Count; i++)
        {
            // a. 从对象池获取鱼
            Fish fish = PoolManager.Instance.Get<Fish>(_config.fishConfig.prefab);
            if (fish == null) continue; // 如果对象池耗尽或出错，跳过这条鱼

            Vector3 localOffset = _slotOffsets[i];
            Vector3 offset = rot * localOffset;
            Vector3 p0 = ForceXZ(_groupP0 + offset);
            Vector3 p1 = ForceXZ(_groupP1 + offset);
            Vector3 p2 = ForceXZ(_groupP2 + offset);
            Vector3 p3 = ForceXZ(_groupP3 + offset);

            fish.SetExternalMovementControl(true);
            fish.Init(_config.fishConfig, p0, p1, p2, p3, _config.groupSpeed);

            _members.Add(new FishGroupMember
            {
                FishInstance = fish,
                SlotIndex = i,
                LocalOffset = localOffset,
                IsAlive = true,
                IsDetached = false,
                Movement = fish.Movement,
                CachedTransform = fish.CachedTransform
            });
        }

        return _members.Count > 0;
    }

    /// <summary>
    /// 更新鱼群
    /// </summary>
    /// <param name="deltaTime"></param>
    public void Tick(float deltaTime)
    {
        for (int i = _members.Count - 1; i >= 0; i--)
        {
            FishGroupMember member = _members[i];
            Fish fish = member.FishInstance;
            if (fish == null || member.CachedTransform == null || !fish.gameObject.activeInHierarchy)
            {
                _members.RemoveAt(i);
                continue;
            }

            if (fish.IsDead || fish.IsDying)
            {
                member.IsAlive = false;
                continue;
            }

            if (!member.IsDetached)
            {
                if (member.Movement == null || !member.Movement.Tick(deltaTime))
                {
                    member.IsDetached = true;
                }
            }
            else
            {
                // 移动鱼
                member.CachedTransform.position += _exitDirection * (_config.groupSpeed * deltaTime);
                if (_exitDirection.sqrMagnitude > 0.0001f)
                {
                    member.CachedTransform.forward = _exitDirection;
                }
            }

            if (IsOutOfRecycleBounds(member.CachedTransform.position))
            {
                ReleaseMemberFish(member);
                _members.RemoveAt(i);
            }
        }
    }

    /// <summary>
    /// 缓存组路径
    /// </summary>
    /// <param name="p0"></param>
    /// <param name="p1"></param>
    /// <param name="p2"></param>
    /// <param name="p3"></param>
    private void CacheGroupPath(Vector3 p0, Vector3 p1, Vector3 p2, Vector3 p3)
    {
        _groupP0 = ForceXZ(p0);
        _groupP1 = ForceXZ(p1);
        _groupP2 = ForceXZ(p2);
        _groupP3 = ForceXZ(p3);
    }

    /// <summary>
    /// 缓存回收边界
    /// </summary>
    /// <param name="minX"></param>
    /// <param name="maxX"></param>
    /// <param name="minZ"></param>
    /// <param name="maxZ"></param>
    /// <param name="padding"></param>
    private void CacheRecycleBounds(float minX, float maxX, float minZ, float maxZ, float padding)
    {
        float pad = Mathf.Max(0f, padding);
        _recycleMinX = minX - pad;
        _recycleMaxX = maxX + pad;
        _recycleMinZ = minZ - pad;
        _recycleMaxZ = maxZ + pad;
    }

    /// <summary>
    /// 是否超出回收边界
    /// </summary>
    /// <param name="position"></param>
    /// <returns></returns>
    private bool IsOutOfRecycleBounds(Vector3 position)
    {
        return position.x < _recycleMinX || position.x > _recycleMaxX || position.z < _recycleMinZ || position.z > _recycleMaxZ;
    }

    /// <summary>
    /// 释放成员鱼
    /// </summary>
    /// <param name="member"></param>
    private void ReleaseMemberFish(FishGroupMember member)
    {
        Fish fish = member.FishInstance;
        if (fish == null || !fish.gameObject.activeInHierarchy) return;

        fish.SetExternalMovementControl(false);
        if (_config != null && _config.fishConfig != null && _config.fishConfig.prefab != null && PoolManager.Instance != null)
        {
            PoolManager.Instance.Release(fish, _config.fishConfig.prefab);
        }
    }

    private static Vector3 ForceXZ(Vector3 value)
    {
        value.y = 0f;
        return value;
    }

    /// <summary>
    /// 方向转换为向量
    /// </summary>
    /// <param name="direction"></param>
    /// <returns></returns>
    private static Vector3 DirectionToVector(FishGroupDirection direction)
    {
        switch (direction)
        {
            case FishGroupDirection.LeftToRight: return Vector3.right;
            case FishGroupDirection.RightToLeft: return Vector3.left;
            case FishGroupDirection.UpToDown: return Vector3.back;
            case FishGroupDirection.DownToUp: return Vector3.forward;
            case FishGroupDirection.LeftUpToRightDown: return new Vector3(1f, 0f, -1f).normalized;
            case FishGroupDirection.RightUpToLeftDown: return new Vector3(-1f, 0f, -1f).normalized;
            case FishGroupDirection.LeftDownToRightUp: return new Vector3(1f, 0f, 1f).normalized;
            case FishGroupDirection.RightDownToLeftUp: return new Vector3(-1f, 0f, 1f).normalized;
            default: return Vector3.right;
        }
    }
}