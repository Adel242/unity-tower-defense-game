using UnityEngine;

public class TowerBuildButton : MonoBehaviour{
    private TowerPlacementManager placementManager;

    private void Start(){
        placementManager =
            FindFirstObjectByType<TowerPlacementManager>();

        if (placementManager == null){
            Debug.LogWarning(
                "TowerPlacementManager was not found."
            );
        }
    }

    public void StartBuildMode(){
        if (placementManager == null){
            return;
        }

        placementManager.StartBuildMode();
    }
}