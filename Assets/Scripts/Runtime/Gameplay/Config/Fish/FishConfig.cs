using UnityEngine;

[CreateAssetMenu(fileName = "FishConfig", menuName = "Scriptable Objects/FishConfig")]
public class FishConfig : ScriptableObject
{
    public FishType fishType;
    public int hp;
    public float speed;
    public int score;
    [Tooltip("运行时根节点 localScale，以本配置为准（覆盖 Prefab 上的缩放）")]
    public Vector3 spawnScale = Vector3.one;
    // 带 Fish 组件的 Prefab
    public GameObject prefab;
}