using UnityEngine;

public struct FishData
{
    public int id;
    public FishType type;
    public float currentHp;
    public float moveTime;
    public float duration;
    public Vector3 startPoint;
    public Vector3 endPoint;
    public Vector3 controlPoint;
    public bool isAlive;
}