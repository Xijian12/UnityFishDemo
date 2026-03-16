using System.Collections.Generic;
using UnityEngine;

public class FishManager : MonoBehaviour
{
    public static FishManager Instance;

    public List<Fish> ActiveFish = new();

    void Awake()
    {
        Instance = this;
    }

    public void AddFish(Fish fish)
    {
        ActiveFish.Add(fish);
    }

    public void RemoveFish(Fish fish)
    {
        ActiveFish.Remove(fish);
    }

    /// <summary>
    /// 获取距离指定位置最近的活鱼。使用 ActiveFish 列表，无 FindObjectsOfType。
    /// </summary>
    /// <param name="fromPosition">起点位置（通常为炮台）</param>
    /// <param name="maxDistance">最大搜索距离，超出返回 null</param>
    /// <returns>最近的鱼，无则返回 null</returns>
    public Fish GetNearestFish(Vector3 fromPosition, float maxDistance = float.MaxValue)
    {
        float maxSq = maxDistance * maxDistance;
        Fish nearest = null;
        float nearestSq = maxSq;

        for (int i = 0; i < ActiveFish.Count; i++)
        {
            Fish fish = ActiveFish[i];
            if (fish == null || fish.IsDead) continue;

            float sq = (fish.transform.position - fromPosition).sqrMagnitude;
            if (sq < nearestSq)
            {
                nearestSq = sq;
                nearest = fish;
            }
        }

        return nearest;
    }

    void Update()
    {
        for (int i = ActiveFish.Count - 1; i >= 0; i--)
        {
            Fish fish = ActiveFish[i];
            if (fish != null)
            {
                // 每帧更新，避免隔帧导致的轨迹抖动和视觉闪烁。
                fish.ManualUpdate(Time.deltaTime);
            }
        }
    }
}