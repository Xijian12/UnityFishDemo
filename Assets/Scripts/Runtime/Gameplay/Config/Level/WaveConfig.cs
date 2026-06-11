using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public class SingleFishSpawnEntry
{
    public FishConfig fishConfig;
    [Min(1)] public int count = 1;
}

[Serializable]
public class WaveConfig
{
    [Tooltip("相对关卡开始的时间（秒），到达后触发本波")]
    public float startTime;

    [Tooltip("FishSpawnMode.FishGroup：按列表各生成一队")]
    public List<FishGroupConfig> fishGroups;

    [Tooltip("FishSpawnMode.SingleFish：按条目生成指定数量单鱼")]
    public List<SingleFishSpawnEntry> singleFishEntries;
}