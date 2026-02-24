using UnityEngine;
using System;

[Serializable]
public class FishConfigEntry
{
    public FishConfig config;

    [Range(0, 100)]
    public float spawnWeight = 10f;
}