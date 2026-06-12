using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 关卡目录：供关卡选择界面读取可用关卡列表。
/// </summary>
[CreateAssetMenu(fileName = "LevelCatalog", menuName = "Scriptable Objects/LevelCatalog")]
public class LevelCatalogConfig : ScriptableObject
{
    [Tooltip("按 levelIndex 或展示顺序排列")]
    public List<LevelConfig> levels = new List<LevelConfig>();

    public IReadOnlyList<LevelConfig> AllLevels => levels;

    public int Count => levels?.Count ?? 0;

    public bool Contains(LevelConfig config)
    {
        if (config == null || levels == null) return false;
        return levels.Contains(config);
    }

    public LevelConfig GetByLevelIndex(int levelIndex)
    {
        if (levels == null) return null;

        for (int i = 0; i < levels.Count; i++)
        {
            LevelConfig cfg = levels[i];
            if (cfg != null && cfg.levelIndex == levelIndex)
                return cfg;
        }

        return null;
    }

    public LevelConfig GetAt(int listIndex)
    {
        if (levels == null || listIndex < 0 || listIndex >= levels.Count)
            return null;
        return levels[listIndex];
    }
}
