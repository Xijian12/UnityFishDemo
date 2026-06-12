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

    [Header("刷怪")]
    [Tooltip("单鱼波次使用 WaveConfig.singleFishEntries；鱼群波次使用 WaveConfig.fishGroups")]
    public FishSpawnMode levelSpawnMode = FishSpawnMode.SingleFish;

    [Tooltip("Wave=波次驱动；Continuous=关卡内按间隔循环刷怪")]
    public LevelSpawnDriveMode spawnDriveMode = LevelSpawnDriveMode.Wave;

    [Min(0.1f)]
    [Tooltip("Continuous 模式下的刷怪间隔（秒）")]
    public float continuousSpawnInterval = 1f;

    [Tooltip("Continuous + FishGroup 时，是否按 FishGroupConfig.spawnWaveCount 循环权重池")]
    public bool loopFishGroupWaves = true;

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
        return $"{GetDisplayTitle()} · {levelType} · {GetSpawnModeLabel()} · {GetSpawnDriveLabel()}\n" +
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

    public string GetSpawnDriveLabel()
    {
        return spawnDriveMode == LevelSpawnDriveMode.Continuous ? "Loop" : "Wave";
    }

    public bool UsesWaveSpawning => spawnDriveMode == LevelSpawnDriveMode.Wave;

    public bool UsesContinuousSpawning => spawnDriveMode == LevelSpawnDriveMode.Continuous;
}