using UnityEngine;

[CreateAssetMenu(fileName = "BulletConfig", menuName = "Scriptable Objects/BulletConfig")]
public class BulletConfig : ScriptableObject
{
    public BulletType bulletType;
    public int damage;
    public float speed;
    public float maxDistance;
    // 带 Fish 组件的 Prefab
    public GameObject prefab;
}
