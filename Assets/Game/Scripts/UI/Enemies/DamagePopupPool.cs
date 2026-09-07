using System.Collections.Generic;
using UnityEngine;

public sealed class DamagePopupPool{
    private static readonly Dictionary<DamagePopup, DamagePopupPool> sharedPools = new();
    private static Transform poolsRoot;

    private readonly DamagePopup popupPrefab;
    private readonly Queue<DamagePopup> availablePopups = new();
    private readonly Transform container;

    private DamagePopupPool(DamagePopup popupPrefab){
        this.popupPrefab = popupPrefab;

        GameObject containerObject = new GameObject(
            $"{popupPrefab.name} Pool"
        );

        container = containerObject.transform;
        container.SetParent(GetPoolsRoot(), false);
    }

    public static DamagePopupPool GetShared(DamagePopup popupPrefab){
        if (
            sharedPools.TryGetValue(popupPrefab, out DamagePopupPool pool) &&
            pool.container != null
        ){
            return pool;
        }

        pool = new DamagePopupPool(popupPrefab);
        sharedPools[popupPrefab] = pool;

        return pool;
    }

    public DamagePopup Get(Vector3 position, Quaternion rotation){
        DamagePopup popup = GetAvailablePopup();

        if (popup == null){
            popup = Object.Instantiate(
                popupPrefab,
                position,
                rotation,
                container
            );

            popup.SetPool(this);

            return popup;
        }

        popup.transform.SetPositionAndRotation(position, rotation);
        popup.gameObject.SetActive(true);

        return popup;
    }

    public void Release(DamagePopup popup){
        popup.ResetForPool();
        popup.gameObject.SetActive(false);
        availablePopups.Enqueue(popup);
    }

    private DamagePopup GetAvailablePopup(){
        while (availablePopups.Count > 0){
            DamagePopup popup = availablePopups.Dequeue();

            if (popup != null){
                return popup;
            }
        }

        return null;
    }

    private static Transform GetPoolsRoot(){
        if (poolsRoot == null){
            GameObject rootObject = new GameObject("Damage Popup Pools");
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
