using System.Collections.Generic;
using UnityEngine;

public class BulletManager : MonoBehaviour
{
    public static BulletManager Instance;

    public List<Bullet> ActiveBullets = new();

    void Awake()
    {
        Instance = this;
    }

    void OnEnable()
    {
        Bullet.OnBulletSpawn += AddBullet;
        Bullet.OnBulletRecycle += RemoveBullet;
    }

    void OnDisable()
    {
        Bullet.OnBulletSpawn -= AddBullet;
        Bullet.OnBulletRecycle -= RemoveBullet;
    }

    public void AddBullet(Bullet bullet)
    {
        if (bullet == null) return;
        ActiveBullets.Add(bullet);
    }

    public void RemoveBullet(Bullet bullet)
    {
        if (bullet == null) return;
        ActiveBullets.Remove(bullet);
    }

    void Update()
    {
        for (int i = 0; i < ActiveBullets.Count; i++)
        {
            if (ActiveBullets[i] == null) continue;
            ActiveBullets[i].ManualUpdate(Time.deltaTime);
        }
    }
}
