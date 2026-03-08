using UnityEngine;
using System.Collections.Generic;

public class ObjectPool<T> where T : Component, IPoolable
{
    // 使用 GameObject
    private readonly GameObject prefab;
    private readonly Transform parent;
    private readonly Queue<T> pool = new();
    private readonly int maxSize;
    private int currentCount;

    public ObjectPool(GameObject prefab, int initialSize, int maxSize, Transform parent = null)
    {
        this.prefab = prefab;
        this.parent = parent;
        this.maxSize = maxSize;

        // 验证 prefab 是否有 T 组件
        if (prefab.GetComponent<T>() == null)
        {
            Debug.LogError($"ObjectPool: Prefab missing required component {typeof(T)}");
        }

        for (int i = 0; i < initialSize; i++)
        {
            CreateNewObject();
        }
    }

    private T CreateNewObject()
    {
        GameObject go = GameObject.Instantiate(prefab, parent);
        T obj = go.GetComponent<T>();

        if (obj == null)
        {
            Debug.LogError("Instantiated object missing IPoolable component!");
            GameObject.Destroy(go);
            return null;
        }

        go.SetActive(false);
        pool.Enqueue(obj);
        currentCount++;
        return obj;
    }

    public T Get()
    {
        if (pool.Count > 0)
        {
            T obj = pool.Dequeue();
            obj.gameObject.SetActive(true);
            // 在这里调用对象池中对象的OnSpawn函数
            obj.OnSpawn();
            return obj;
        }

        if (currentCount < maxSize)
        {
            T newObj = CreateNewObject();
            if (newObj != null)
            {
                newObj.gameObject.SetActive(true);
                newObj.OnSpawn();
                return newObj;
            }
        }
        else
        {
            Debug.LogWarning("objectPool's number is limit！");
        }

        return null;
    }

    public void Release(T obj)
    {
        if (obj == null) return;
        // 在这里调用对象池中对象的OnRecycle函数
        obj.OnRecycle();
        //SetActive 操作昂贵，对象数据巨大时，建议直接使用 伪隐藏
        obj.gameObject.SetActive(false);
        pool.Enqueue(obj);
    }
}