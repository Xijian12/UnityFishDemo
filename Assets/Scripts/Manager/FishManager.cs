using System.Collections.Generic;
using UnityEngine;

public class FishManager : MonoBehaviour
{
    public static FishManager Instance;

    public List<Fish> ActiveFish = new();

    void Awake()
    {
        Instance = this;
    }

    public void AddFish(Fish fish)
    {
        ActiveFish.Add(fish);
    }

    public void RemoveFish(Fish fish)
    {
        ActiveFish.Remove(fish);
    }

    void Update()
    {
        float delta = Time.deltaTime;

        for (int i = 0; i < ActiveFish.Count; i++)
        {
            ActiveFish[i].ManualUpdate(delta);
        }
    }
}