using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "FishDatabase", menuName = "Scriptable Objects/FishDatabase")]
public class FishDatabase : ScriptableObject
{
    public List<FishConfigEntry> allFishConfigEntries;
}
