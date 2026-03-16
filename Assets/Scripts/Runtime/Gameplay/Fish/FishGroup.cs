using UnityEngine;
using System.Collections.Generic;

public class FishGroup{
    private readonly FishGroupConfig _config;
    private readonly int _groupID;
    private readonly List<Fish> _fishList = new List<Fish>(16);
    private readonly List<Vector3> _slotOffsets = new List<Vector3>(16);

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
    public bool IsFinished => _fishList.Count == 0;

    /// <summary>
    /// 使用“组路径 + 槽位偏移”生成一组鱼。
    /// 每条鱼得到独立贝塞尔路径：groupPath + slotOffset。
    /// </summary>
    public bool Spawn(Vector3 groupP0, Vector3 groupP1, Vector3 groupP2, Vector3 groupP3)
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

        _fishList.Clear();

        Vector3 forward = groupP3 - groupP0;
        if (forward.sqrMagnitude < 0.0001f) forward = DirectionToVector(_config.groupDirection);
        forward.y = 0f;
        if (forward.sqrMagnitude < 0.0001f) forward = Vector3.right;

        Quaternion rot = Quaternion.LookRotation(forward.normalized, Vector3.up);

        for (int i = 0; i < _slotOffsets.Count; i++)
        {
            Fish fish = PoolManager.Instance.Get<Fish>(_config.fishConfig.prefab);
            if (fish == null) continue;

            Vector3 offset = rot * _slotOffsets[i];
            Vector3 p0 = ForceXZ(groupP0 + offset);
            Vector3 p1 = ForceXZ(groupP1 + offset);
            Vector3 p2 = ForceXZ(groupP2 + offset);
            Vector3 p3 = ForceXZ(groupP3 + offset);

            float localDepth = _slotOffsets[i].z;
            float trailingDepth = Mathf.Max(0f, -localDepth);
            float startDelay = _config.baseStartDelay + trailingDepth * _config.startDelayPerMeter;
            startDelay = Mathf.Min(startDelay, _config.maxStartDelay);

            fish.Init(_config.fishConfig, p0, p1, p2, p3, startDelay);
            _fishList.Add(fish);
        }

        return _fishList.Count > 0;
    }

    /// <summary>
    /// 更新鱼群
    /// </summary>
    /// <param name="deltaTime"></param>
    public void Tick(float deltaTime)
    {
        for (int i = _fishList.Count - 1; i >= 0; i--)
        {
            Fish fish = _fishList[i];
            if (fish == null || !fish.gameObject.activeInHierarchy)
            {
                _fishList.RemoveAt(i);
            }
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