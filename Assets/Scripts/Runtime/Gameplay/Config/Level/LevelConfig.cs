using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "LevelConfig", menuName = "Scriptable Objects/LevelConfig")]
public class LevelConfig : ScriptableObject
{
    [Header("关卡基础信息")]
    public LevelType levelType;                  // 关卡类型
    public int levelIndex;                       // 关卡索引
    public int levelTargetScore;                 // 目标分数
    public float levelTime;                      // 关卡时长（秒）

    [Header("刷怪模式（关卡驱动时由 LevelManager 统一设置）")]
    [Tooltip("单鱼波次使用 WaveConfig.singleFishEntries；鱼群波次使用 WaveConfig.fishGroups")]
    public FishSpawnMode levelSpawnMode = FishSpawnMode.SingleFish;

    [Header("初始武器")]
    public CanonType initialCanonType;           // 初始炮塔类型
    public BulletType initialBulletType;         // 初始子弹类型

    [Header("波次配置")]
    public List<WaveConfig> waveConfigs;         // 波次配置

    public string GetDisplayTitle()
    {
        return $"Level {levelIndex}";
    }

    public string GetSelectListLabel()
    {
        return $"{GetDisplayTitle()} · {levelType} · {GetSpawnModeLabel()}\n" +
               $"Target {levelTargetScore} pts · {levelTime:F0}s";
    }

    public string GetHudSubtitle()
    {
        return $"{levelType} · {GetSpawnModeLabel()}";
    }

    public string GetSpawnModeLabel()
    {
        return levelSpawnMode == FishSpawnMode.FishGroup ? "Fish Group" : "Single Fish";
    }
}