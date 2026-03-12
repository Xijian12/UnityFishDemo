using UnityEngine;

public class FishCombat : MonoBehaviour
{
    private FishConfig config;
    private int currentHp;
    public bool IsDying { get; private set; } = false;
    public bool IsDead { get; private set; } = false;

    public void Init(FishConfig config)
    {
        this.config = config;
        currentHp = config.hp;
    }

    public bool TakeDamage(int damage)
    {
        if (IsDead) return false;
        if (IsDying || config == null) return false;

        currentHp -= damage;

        return true;
    }


    /// <summary>
    /// 检查并进入死亡状态
    /// </summary>
    public bool TryEnterDeath()
    {
        if (IsDead) return false;
        if (IsDying || config == null) return false;

        if (currentHp <= 0)
        {
            IsDying = true;
            IsDead = true;
            return true;
        }
        return false;
    }

    public FishType GetFishType()
    {
        return config != null ? config.fishType : default;
    }

    public int GetScore()
    {
        return config != null ? config.score : 0;
    }

    public void Reset()
    {
        IsDead = false;
        currentHp = config != null ? config.hp : 0;
        IsDying = false;
    }
}