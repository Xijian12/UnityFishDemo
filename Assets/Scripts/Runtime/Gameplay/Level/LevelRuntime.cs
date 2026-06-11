using System.Collections.Generic;
using UnityEngine;

public class LevelRuntime{
    public LevelConfig levelConfig;
    public float currentTime;
    public int currentScore;
    public int currentWaveIndex;
    public CanonType currentCanonType;
    public BulletType currentBulletType;
    public bool isBonusTime = false;        // 是否是Bonus时间
    public LevelState currentLevelState = LevelState.Idle;

    public void Init(LevelConfig levelConfig)
    {
        this.levelConfig = levelConfig;
        this.currentCanonType = levelConfig.initialCanonType;
        this.currentBulletType = levelConfig.initialBulletType;
        this.currentWaveIndex = 0;
        this.currentScore = 0;
        this.currentTime = 0;
        this.isBonusTime = false;
        this.currentLevelState = LevelState.Idle;
    }
}