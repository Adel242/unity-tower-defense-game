using System.Linq;
using UnityEditor;
using UnityEngine;

[InitializeOnLoad]
public static class BuildSceneSetup
{
    private const string ScenesFolder = "Assets/Game/Scenes";

    static BuildSceneSetup(){
        EditorApplication.delayCall += SetupBuildScenes;
    }

    [MenuItem("Tools/Setup Build Scenes")]
    public static void SetupBuildScenes(){
        string[] sceneGuids = AssetDatabase.FindAssets(
            "t:Scene",
            new[] { ScenesFolder }
        );

        string[] scenePaths = sceneGuids
            .Select(AssetDatabase.GUIDToAssetPath)
            .OrderBy(GetSceneOrder)
            .ThenBy(path => path)
            .ToArray();

        EditorBuildSettings.scenes = scenePaths
            .Select(path => new EditorBuildSettingsScene(path, true))
            .ToArray();

        Debug.Log($"Build Scenes configured automatically: {scenePaths.Length} scenes.");
    }

    private static int GetSceneOrder(string path){
        if (path.EndsWith("/MainMenu.unity")){
            return 0;
        }

        if (path.EndsWith("/Game.unity")){
            return 1;
        }

        return 100;
    }
}