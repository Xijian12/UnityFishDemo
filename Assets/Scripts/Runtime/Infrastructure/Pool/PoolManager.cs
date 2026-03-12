using System;
using System.Collections.Generic;
using UnityEngine;

public class PoolManager : MonoBehaviour
{
    public static PoolManager Instance;

    private const int poolMaxSize = 20;
    private int poolCurrentCount;

    private readonly Dictionary<GameObject, object> pools = new();


    private void Awake()
    {
        Instance = this;
        poolCurrentCount = 0;
    }
    public void CreatePool<T>(GameObject prefab, int initialSize, int maxSize, Transform parent = null)
        where T : Component, IPoolable
    {
        if (prefab == null)
        {
            Debug.LogError("Prefab is null!");
            return;
        }

        if (pools.ContainsKey(prefab))
        {
            Debug.LogWarning($"Pool for {prefab.name} already exists.");
            return;
        }

        if (poolCurrentCount < poolMaxSize)
        {
            ObjectPool<T> pool = new(prefab, initialSize, maxSize, parent);
            pools.Add(prefab, pool);
            poolCurrentCount++;
        }
        else
        {
            Debug.LogWarning($"number of {prefab.name} objectPools is limit！");
        }
        return;
    }

    public T Get<T>(GameObject prefab)
        where T : Component, IPoolable
    {
        if (!pools.ContainsKey(prefab))
        {
            Debug.LogError($"Pool for {prefab.name} does not exist.");
            return null;
        }

        ObjectPool<T> pool = pools[prefab] as ObjectPool<T>;
        T obj = pool.Get();
        if (obj == null)
        {
            Debug.LogError($"Get object from pool for {prefab.name} is null.");
            return null;
        }
        return obj;
    }


    // 新增重载：显式传 prefab
    public void Release<T>(T instance, GameObject prefab)
        where T : Component, IPoolable
    {
        if (instance == null || prefab == null)
        {
            Debug.LogError("Release: instance or prefab is null!");
            return;
        }

        if (pools.TryGetValue(prefab, out var poolObj))
        {
            if (poolObj is ObjectPool<T> pool)
            {
                pool.Release(instance);
            }
            else
            {
                Debug.LogError($"Pool type mismatch for {prefab.name}");
            }
        }
        else
        {
            Debug.LogError($"No pool found for prefab: {prefab.name}");
        }
    }

}
