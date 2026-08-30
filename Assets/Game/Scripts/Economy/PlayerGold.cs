using System;
using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerGold : MonoBehaviour{
    [SerializeField] private int startingGold = 500;
    

    public int CurrentGold { get; private set; }

    public event Action<int> GoldChanged;

    private void Awake(){
        CurrentGold = startingGold;
    }

    public bool CanAfford(int amount){
        return CurrentGold >= amount;
    }

    public bool SpendGold(int amount){
        if (amount <= 0 || !CanAfford(amount)){
            return false;
        }

        CurrentGold -= amount;
        GoldChanged?.Invoke(CurrentGold);

        return true;
    }

    public void AddGold(int amount){
        if (amount <= 0){
            return;
        }

        CurrentGold += amount;
        GoldChanged?.Invoke(CurrentGold);
    }
}