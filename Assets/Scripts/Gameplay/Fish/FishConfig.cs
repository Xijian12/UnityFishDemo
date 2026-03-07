using UnityEngine;

[CreateAssetMenu(fileName = "FishConfig", menuName = "Scriptable Objects/FishConfig")]
public class FishConfig : ScriptableObject
{
    public FishType fishType;
    public int hp;
    public float speed;
    public int score;
    // 带 Fish 组件的 Prefab
    public GameObject prefab;
}