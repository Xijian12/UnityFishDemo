using UnityEngine;

[CreateAssetMenu(fileName = "FishGroupConfig", menuName = "Scriptable Objects/FishGroupConfig")]
public class FishGroupConfig : ScriptableObject
{
    [Header("基础配置")]
    public FishConfig fishConfig;
    public FormationType formationType = FormationType.Line;
    public FishGroupDirection groupDirection = FishGroupDirection.LeftToRight;

    [Header("阵型参数")]
    [Tooltip("单个鱼群实例里的鱼数量（不是鱼群波次数量）")]
    [Min(1)] public int groupCount = 10;
    [Min(0.1f)] public float groupDistance = 1f;
    [Range(0f, 89f)] public float groupAngle = 20f;

    [Header("运动参数")]
    [Min(0.1f)] public float groupSpeed = 5f;
    [Tooltip("鱼群内部的基础起跑延迟（秒）。")]
    [Min(0f)] public float baseStartDelay = 0f;
    [Tooltip("每 1m 的后向槽位深度增加的起跑延迟（秒）。")]
    [Min(0f)] public float startDelayPerMeter = 0.1f;
    [Tooltip("单条鱼起跑延迟上限（秒）。")]
    [Min(0f)] public float maxStartDelay = 1.5f;

    [Header("生成控制")]
    [Tooltip("该配置可生成的鱼群波次数量。0=不生成，1=固定一波，N=固定N波。")]
    [Min(0)] public int spawnWaveCount = 1;

    [Header("对象池参数")]
    [Min(1)] public int initialPoolSize = 30;
    [Min(1)] public int maxPoolSize = 100;
}
