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
        // 获取当前帧的编号,每一帧只处理其中一组
        int frameCount = Time.frameCount;


        int targetGroup = frameCount % 2;

        for (int i = 0; i < ActiveFish.Count; i++)
        {
            // 只更新属于当前帧组的鱼
            if (i % 2 == targetGroup)
            {
                // 因为是每 2 帧更新一次，delta 必须乘以 2，否则鱼的移动速度会慢 2 倍
                ActiveFish[i].ManualUpdate(Time.deltaTime * 2);
            }
        }
    }
}