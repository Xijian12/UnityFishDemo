using UnityEngine;
using System.Collections.Generic;

public static class FormationCalculator{

    /// <summary>
    /// 计算鱼群位置
    /// </summary>
    /// <param name="formationType"></param>
    /// <param name="config"></param>
    /// <returns></returns>
    public static List<Vector3> CalculateFormationPosition(FormationType formationType, FishGroupConfig config){
        var positions = new List<Vector3>(config != null ? Mathf.Max(1, config.groupCount) : 1);
        CalculateFormationPosition(formationType, config, positions);
        return positions;
    }

    /// <summary>
    /// 计算鱼群相对位置
    /// </summary>
    /// <param name="formationType"></param>
    /// <param name="config"></param>
    /// <param name="positions"></param>
    public static void CalculateFormationPosition(FormationType formationType, FishGroupConfig config, List<Vector3> positions){
        positions.Clear();
        if (config == null) return;

        switch(formationType){
            case FormationType.Line:
                CalculateLineFormationPosition(config, positions);
                break;
            case FormationType.Circle:
                CalculateCircleFormationPosition(config, positions);
                break;
            case FormationType.Vword:
                CalculateVwordFormationPosition(config, positions);
                break;
            default:
                positions.Add(Vector3.zero);
                break;
        }
    }

    /// <summary>
    /// 计算直线鱼群位置
    /// </summary>
    /// <param name="config"></param>
    /// <returns></returns>
    private static void CalculateLineFormationPosition(FishGroupConfig config, List<Vector3> positions){
        int count = Mathf.Max(1, config.groupCount);
        float half = (count - 1) * 0.5f;
        for(int i = 0; i < count; i++){
            // 线阵按“前进轴”排列：在 FishGroup 中会被旋转到 group forward。
            // 这样 LeftToRight 时会表现为世界 X 方向直线排列。
            float z = (i - half) * config.groupDistance;
            positions.Add(new Vector3(0f, 0f, z));
        }
    }

    /// <summary>
    /// 计算圆形鱼群位置
    /// </summary>
    /// <param name="config"></param>
    /// <returns></returns>
    private static void CalculateCircleFormationPosition(FishGroupConfig config, List<Vector3> positions){
        int count = Mathf.Max(1, config.groupCount);
        if (count == 1){
            positions.Add(Vector3.zero);
            return;
        }

        float angleStep = 360f / (count - 1);
        float angleDeg = config.groupAngle;
        float radius = config.groupDistance;
        positions.Add(Vector3.zero); // 领头鱼

        for(int i = 1; i < count; i++){
            float rad = angleDeg * Mathf.Deg2Rad;
            positions.Add(new Vector3(Mathf.Cos(rad) * radius, 0f, Mathf.Sin(rad) * radius));
            angleDeg += angleStep;
        }
    }

    /// <summary>
    /// 计算V字形鱼群位置
    /// </summary>
    /// <param name="config"></param>
    /// <returns></returns>
    private static void CalculateVwordFormationPosition(FishGroupConfig config, List<Vector3> positions){
        int count = Mathf.Max(1, config.groupCount);
        positions.Add(Vector3.zero);
        if (count == 1) return;

        float rad = config.groupAngle * Mathf.Deg2Rad;
        float step = config.groupDistance;
        int row = 1;

        while (positions.Count < count){
            // 以 local +Z 作为前进方向：
            // - 鱼头在 (0,0,0)
            // - 两翼沿 local -Z（后方）展开，保证 V 字头朝向前进方向
            float back = -Mathf.Cos(rad) * row * step;
            float side = Mathf.Sin(rad) * row * step;

            positions.Add(new Vector3(side, 0f, back));
            if (positions.Count >= count) break;
            positions.Add(new Vector3(-side, 0f, back));

            row++;
        }
    }
}