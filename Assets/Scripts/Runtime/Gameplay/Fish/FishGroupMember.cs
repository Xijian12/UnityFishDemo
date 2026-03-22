using UnityEngine;

/// <summary>
/// 鱼群中的单个成员状态。
/// </summary>
public sealed class FishGroupMember
{
    public Fish FishInstance;
    public int SlotIndex;
    public Vector3 LocalOffset;
    public bool IsAlive;
    public bool IsDetached;
    public FishMovement Movement;
    public Transform CachedTransform;
}
