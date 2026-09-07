using System.Collections.Generic;
using UnityEngine;

public sealed class ProjectilePool{
    private static readonly Dictionary<GameObject, ProjectilePool> sharedPools = new();
    private static Transform poolsRoot;

    private readonly GameObject projectilePrefab;
    private readonly Queue<Projectile> availableProjectiles = new();
    private readonly Transform container;

    private ProjectilePool(GameObject projectilePrefab){
        this.projectilePrefab = projectilePrefab;

        GameObject containerObject = new GameObject(
            $"{projectilePrefab.name} Pool"
        );

        container = containerObject.transform;
        container.SetParent(GetPoolsRoot(), false);
    }

    public static ProjectilePool GetShared(GameObject projectilePrefab){
        if (
            sharedPools.TryGetValue(projectilePrefab, out ProjectilePool pool) &&
            pool.container != null
        ){
            return pool;
        }

        pool = new ProjectilePool(projectilePrefab);
        sharedPools[projectilePrefab] = pool;

        return pool;
    }

    public Projectile Get(Vector3 position, Quaternion rotation){
        Projectile projectile = GetAvailableProjectile();

        if (projectile == null){
            GameObject projectileObject = Object.Instantiate(
                projectilePrefab,
                position,
                rotation,
                container
            );

            projectile = projectileObject.GetComponent<Projectile>();
            projectile.SetPool(this);

            return projectile;
        }

        projectile.transform.SetPositionAndRotation(position, rotation);
        projectile.gameObject.SetActive(true);

        return projectile;
    }

    public void Release(Projectile projectile){
        projectile.ResetForPool();
        projectile.gameObject.SetActive(false);
        availableProjectiles.Enqueue(projectile);
    }

    private Projectile GetAvailableProjectile(){
        while (availableProjectiles.Count > 0){
            Projectile projectile = availableProjectiles.Dequeue();

            if (projectile != null){
                return projectile;
            }
        }

        return null;
    }

    private static Transform GetPoolsRoot(){
        if (poolsRoot == null){
            GameObject rootObject = new GameObject("Projectile Pools");
            poolsRoot = rootObject.transform;
        }

        return poolsRoot;
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetSharedPools(){
        sharedPools.Clear();
        poolsRoot = null;
    }
}
