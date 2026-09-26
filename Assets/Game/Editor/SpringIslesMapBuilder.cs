using System;
using System.IO;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using Unity.AI.Navigation;
using UnityEngine.AI;
using UnityEngine.Rendering;
using System.Collections.Generic;

/// <summary>Editor-only authoring of the Spring Isles battlefield. Never runs in a player.</summary>
public static class SpringIslesMapBuilder
{
    private const string Report = "Docs/SpringIslesValidation.txt";
    private const string Pack = "Assets/ToonScapes/Spring Isles/";
    private const string Output = "Assets/Game/Environment/SpringIsles";
    private static readonly Vector2[] Route = {
        new Vector2(-19,12), new Vector2(15,12), new Vector2(15,2),
        new Vector2(-14,2), new Vector2(-14,-10), new Vector2(17,-10)
    };
    private static readonly Rect[] Terraces = {
        new Rect(-11,5,22,4), new Rect(-11,-7,22,6), new Rect(-23,-9,6,8)
    };

    [MenuItem("Tools/Spring Isles/Add enemy entrance marker")]
    public static void AddEnemyEntranceMarker()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode ||
            SceneManager.GetActiveScene().path != "Assets/Game/Scenes/Game.unity")
            throw new InvalidOperationException("Open Game.unity outside Play Mode first.");

        var map = GameObject.Find("Spring Isles - Santuario del Alba");
        var spawn = GameObject.Find("SpawnZone");
        if (map == null || spawn == null)
            throw new InvalidOperationException("The Spring Isles map or SpawnZone is missing.");
        if (map.transform.Find("Enemy entrance marker") != null)
            return;

        var materialPath = Output + "/EnemyEntranceMarker.mat";
        var material = AssetDatabase.LoadAssetAtPath<Material>(materialPath);
        if (material == null)
        {
            var shader = Shader.Find("Sprites/Default");
            if (shader == null) throw new InvalidOperationException("Sprites/Default shader is missing.");
            material = new Material(shader) { name = "Enemy entrance marker" };
            AssetDatabase.CreateAsset(material, materialPath);
        }

        var root = Group("Enemy entrance marker", map.transform);
        root.position = spawn.transform.position + Vector3.right * 3.8f + Vector3.up * .12f;
        int ignoreRaycast = LayerMask.NameToLayer("Ignore Raycast");
        root.gameObject.layer = ignoreRaycast;

        var outline = new Color(.95f, .38f, .16f, .68f);
        var highlight = new Color(1f, .76f, .37f, 1f);
        var ring = new Vector3[48];
        for (int i = 0; i < ring.Length; i++)
        {
            float angle = i * Mathf.PI * 2f / ring.Length;
            ring[i] = new Vector3(Mathf.Cos(angle) * 1.55f, .03f,
                Mathf.Sin(angle) * 1.55f);
        }
        CreateMarkerLine("Entrance seal", root, ring, .11f, outline, material, true);
        CreateMarkerLine("Arrow toward the base", root, new[] {
            new Vector3(-1f,.06f,-.9f), new Vector3(1.15f,.06f,0),
            new Vector3(-1f,.06f,.9f)
        }, .22f, highlight, material);
        CreateMarkerLine("Second chevron", root, new[] {
            new Vector3(-1.37f,.05f,-.55f), new Vector3(-.55f,.05f,0),
            new Vector3(-1.37f,.05f,.55f)
        }, .09f, outline, material);

        var beam = CreateMarkerLine("Entrance beacon", root, new[] {
            new Vector3(0,.1f,0), new Vector3(0,3.5f,0)
        }, .22f, new Color(1f,.62f,.27f,.72f), material);
        beam.widthCurve = new AnimationCurve(
            new Keyframe(0,.9f), new Keyframe(.35f,.52f), new Keyframe(1,0));
        var fade = new Gradient();
        fade.SetKeys(new[] {
            new GradientColorKey(highlight,0), new GradientColorKey(highlight,1)
        }, new[] {
            new GradientAlphaKey(.72f,0), new GradientAlphaKey(0,1)
        });
        beam.colorGradient = fade;

        EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
        EditorSceneManager.SaveScene(SceneManager.GetActiveScene());
        Capture();
        Debug.Log("Enemy entrance marker saved in Game.unity.");
    }

    private static LineRenderer CreateMarkerLine(string name, Transform parent,
        Vector3[] points, float width, Color color, Material material, bool loop = false)
    {
        var child = Group(name, parent).gameObject;
        child.layer = LayerMask.NameToLayer("Ignore Raycast");
        var line = child.AddComponent<LineRenderer>();
        line.sharedMaterial = material;
        line.useWorldSpace = false;
        line.positionCount = points.Length;
        line.SetPositions(points);
        line.loop = loop;
        line.widthMultiplier = width;
        line.startColor = color;
        line.endColor = color;
        line.numCapVertices = 4;
        line.numCornerVertices = 4;
        line.shadowCastingMode = ShadowCastingMode.Off;
        line.receiveShadows = false;
        return line;
    }

    [MenuItem("Tools/Spring Isles/Create battlefield (once)")]
    public static void Build()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode) throw new InvalidOperationException("Exit Play Mode first.");
        Scene scene = SceneManager.GetActiveScene();
        if (scene.path != "Assets/Game/Scenes/Game.unity") throw new InvalidOperationException("Open Game.unity first.");
        if (GameObject.Find("Spring Isles - Santuario del Alba") != null)
            throw new InvalidOperationException("Map already exists. Edit the scene objects directly; nothing was replaced.");
        Directory.CreateDirectory(Output);
        Directory.CreateDirectory("Assets/Game/MapBackups");
        AssetDatabase.Refresh();
        string backup = AssetDatabase.GenerateUniqueAssetPath("Assets/Game/MapBackups/GameBeforeSpringIsles.unity");
        if (!EditorSceneManager.SaveScene(scene, backup, true)) throw new IOException("Could not back up scene.");
        Undo.IncrementCurrentGroup();
        Undo.SetCurrentGroupName("Author Spring Isles battlefield");
        var root = Group("Spring Isles - Santuario del Alba", null);
        var legacy = Group("Previous blockout (disabled - backup)", root);
        foreach (var go in scene.GetRootGameObjects())
        {
            if (go.name == "Ground" || go.name == "EnemyPath" || go.name == "Island Foundation" ||
                go.name.StartsWith("Buildable Platform") || go.name == "Island Water")
            {
                Undo.SetTransformParent(go.transform, legacy, "Preserve previous blockout");
                Undo.RecordObject(go, "Disable previous blockout");
                go.SetActive(false);
            }
        }
        var landscape = Group("01 - Painted island", root);
        var buildable = Group("02 - Elevated construction terraces", root);
        var path = Group("03 - Enemy corridor (invisible navigation)", root);
        var rocks = Group("04 - Cliffs and shoreline", root);
        var plants = Group("05 - Trees and flowers", root);
        var landmarks = Group("06 - Gate and sanctuary", root);
        CreateTerrain(landscape);
        for (int i = 0; i < Terraces.Length; i++)
        {
            Rect r = Terraces[i];
            var top = Group($"Terrace {i+1} - buildable surface", buildable).gameObject;
            top.layer = LayerMask.NameToLayer("BuildableGround");
            top.transform.position = new Vector3(r.center.x, 1.7f, r.center.y);
            var box = top.AddComponent<BoxCollider>();
            box.size = new Vector3(r.width - .2f, .4f, r.height - .2f);
            // Low retaining stones reinforce the elevation without obstructing tower cells.
            for (float x = r.xMin + 1; x < r.xMax; x += 3.2f)
            {
                Rock("TSI_Cliff_02A", new Vector3(x,.7f,r.yMin-.35f), new Vector3(3.6f,2.2f,1.3f), rocks, 0);
                Rock("TSI_Cliff_02A", new Vector3(x,.7f,r.yMax+.35f), new Vector3(3.6f,2.2f,1.3f), rocks, 180);
            }
        }
        for (int i = 1; i < Route.Length; i++)
        {
            Vector2 a = Route[i-1], b = Route[i];
            var segment = Group($"Route {i:00}", path).gameObject;
            segment.layer = LayerMask.NameToLayer("EnemyPath");
            segment.transform.position = new Vector3((a.x+b.x)*.5f,.35f,(a.y+b.y)*.5f);
            segment.AddComponent<BoxCollider>().size = new Vector3(Mathf.Abs(a.x-b.x)+3.6f,.2f,Mathf.Abs(a.y-b.y)+3.6f);
        }
        var random = new System.Random(2811);
        for (int i = 0; i < 48; i++)
        {
            float angle = i * Mathf.PI * 2 / 48;
            float x = Mathf.Cos(angle)*25.5f, z = Mathf.Sin(angle)*19.3f;
            x += (float)random.NextDouble()*1.1f;
            float width = 3.5f+(float)random.NextDouble()*2;
            Rock(i%2 == 0 ? "TSI_Cliff_01A" : "TSI_Rock_Large_01A",
                new Vector3(x,-.65f,z), new Vector3(width,3.8f,width*.8f), rocks, angle*Mathf.Rad2Deg);
        }
        // Trees frame the battlefield: no crowns over either the route or construction terraces.
        Vector2[] treePositions = {
            new Vector2(-20,16),new Vector2(-15,17),new Vector2(-8,17),new Vector2(0,17),new Vector2(7,17),
            new Vector2(19,14),new Vector2(21,8),new Vector2(22,2),new Vector2(21,-3),
            new Vector2(-23,7),new Vector2(-22,12),new Vector2(-21,-13),new Vector2(-12,-16),
            new Vector2(-5,-16),new Vector2(4,-16),new Vector2(12,-16),new Vector2(20,-14)
        };
        for (int i = 0; i < treePositions.Length; i++)
        {
            Vector2 p = treePositions[i];
            var tree = Place(i%4==0 ? "TSI_Blossom_Tree_01A" : "TSI_Springleaf_Tree_01A",
                new Vector3(p.x,Height(p.x,p.y)-.12f,p.y), i*73, plants);
            FitHeight(tree, i%4==0 ? 4.5f : 5.8f);
        }
        for (int i = 0; i < 140; i++)
        {
            float x = (float)random.NextDouble()*48-24, z = (float)random.NextDouble()*36-18;
            if (Height(x,z)<.5f || RoadDistance(new Vector2(x,z))<2.8f || Terraces.Any(r=>Expanded(r,.65f).Contains(new Vector2(x,z)))) continue;
            var plant = Place(i%5==0 ? "TSI_Blossom_Shrub_01A" : "TSI_Grass_Patch_01A",
                new Vector3(x,Height(x,z)-.08f,z), (float)random.NextDouble()*360, plants);
            FitHeight(plant, i%5==0 ? 1.1f : .28f);
        }
        var gate = Place("TSI_Preset_Gate_01A", new Vector3(-18,.45f,12),0,landmarks);
        FitHeight(gate,4.6f);
        var shrine = Place("TSI_Preset_Stone_Shrine_01A",new Vector3(19,.6f,-10),90,landmarks);
        FitHeight(shrine,3.8f);
        for (int i=0;i<6;i++)
        {
            Vector2 p = Route[i];
            var marker = Place("TSI_Stone_Lantern_01A",new Vector3(p.x,Height(p.x,p.y+2.5f),p.y+2.5f),0,landmarks);
            FitHeight(marker,1.5f);
        }
        var water = Place("TSI_Water_Disk_01A",new Vector3(0,-.8f,0),0,landscape);
        water.name = "Spring Isles lagoon";
        water.transform.localScale *= 65;
        ConfigureGameplay();
        ConfigureLighting();
        Physics.SyncTransforms();
        var surface = UnityEngine.Object.FindFirstObjectByType<NavMeshSurface>();
        if (surface == null) throw new InvalidOperationException("Missing existing NavMeshSurface.");
        Undo.RecordObject(surface,"Bake new enemy route");
        surface.RemoveData();
        surface.navMeshData = null;
        surface.collectObjects = CollectObjects.All;
        surface.useGeometry = NavMeshCollectGeometry.PhysicsColliders;
        surface.layerMask = LayerMask.GetMask("EnemyPath");
        surface.overrideVoxelSize = true;
        surface.voxelSize = .1f;
        surface.BuildNavMesh();
        AssetDatabase.CreateAsset(surface.navMeshData, AssetDatabase.GenerateUniqueAssetPath(Output+"/EnemyRoute.asset"));
        EditorUtility.SetDirty(surface);
        AssetDatabase.SaveAssets();
        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        Selection.activeGameObject = root.gameObject;
        if (SceneView.lastActiveSceneView != null)
            SceneView.lastActiveSceneView.LookAt(new Vector3(0,0,1),Quaternion.Euler(55,0,0),42);
        Polish();
        AddEnemyEntranceMarker();
        Debug.Log("Spring Isles authored and saved. Backup: " + backup);
    }

    private static Transform Group(string name, Transform parent)
    {
        var go = new GameObject(name);
        Undo.RegisterCreatedObjectUndo(go,"Create Spring Isles object");
        go.transform.SetParent(parent,false);
        return go.transform;
    }

    private static GameObject Place(string name, Vector3 position, float yaw, Transform parent)
    {
        string asset = AssetDatabase.FindAssets(name+" t:Prefab",new[]{Pack+"Prefabs"})
            .Select(AssetDatabase.GUIDToAssetPath).First(p=>Path.GetFileNameWithoutExtension(p)==name);
        var go = (GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(asset),parent);
        Undo.RegisterCreatedObjectUndo(go,"Place Spring Isles prefab");
        go.transform.SetPositionAndRotation(position,Quaternion.Euler(0,yaw,0));
        // Decoration must not enter targeting, build-grid scans or the baked enemy corridor.
        foreach (Transform t in go.GetComponentsInChildren<Transform>(true))
        {
            t.gameObject.layer=0;
            PrefabUtility.RecordPrefabInstancePropertyModifications(t.gameObject);
        }
        foreach (Collider c in go.GetComponentsInChildren<Collider>(true))
        {
            c.enabled=false;
            PrefabUtility.RecordPrefabInstancePropertyModifications(c);
        }
        foreach (NavMeshObstacle obstacle in go.GetComponentsInChildren<NavMeshObstacle>(true))
        {
            obstacle.enabled=false;
            PrefabUtility.RecordPrefabInstancePropertyModifications(obstacle);
        }
        PrefabUtility.RecordPrefabInstancePropertyModifications(go.transform);
        return go;
    }

    private static Bounds BoundsOf(GameObject go)
    {
        var renderers = go.GetComponentsInChildren<Renderer>();
        Bounds b = renderers[0].bounds;
        foreach (var r in renderers) b.Encapsulate(r.bounds);
        return b;
    }

    private static void FitHeight(GameObject go,float height)
    {
        float ground = go.transform.position.y;
        go.transform.localScale *= height/BoundsOf(go).size.y;
        go.transform.position += Vector3.up*(ground-BoundsOf(go).min.y);
        PrefabUtility.RecordPrefabInstancePropertyModifications(go.transform);
    }

    private static void Rock(string asset,Vector3 center,Vector3 size,Transform parent,float yaw)
    {
        var go=Place(asset,Vector3.zero,0,parent);
        Vector3 original=BoundsOf(go).size;
        go.transform.localScale=Vector3.Scale(go.transform.localScale,new Vector3(size.x/original.x,size.y/original.y,size.z/original.z));
        go.transform.rotation=Quaternion.Euler(0,yaw,0);
        go.transform.position += center-BoundsOf(go).center;
        PrefabUtility.RecordPrefabInstancePropertyModifications(go.transform);
    }

    private static Rect Expanded(Rect r,float amount) => new Rect(r.xMin-amount,r.yMin-amount,r.width+amount*2,r.height+amount*2);

    private static float RoadDistance(Vector2 p)
    {
        float best=float.MaxValue;
        for(int i=1;i<Route.Length;i++)
        {
            Vector2 a=Route[i-1], d=Route[i]-a;
            best=Mathf.Min(best,Vector2.Distance(p,a+d*Mathf.Clamp01(Vector2.Dot(p-a,d)/d.sqrMagnitude)));
        }
        return best;
    }

    private static float Height(float x,float z)
    {
        float island=Mathf.Sqrt(x*x/(25.8f*25.8f)+z*z/(20.5f*20.5f));
        float shoreline=1-Mathf.SmoothStep(0,1,Mathf.InverseLerp(.85f,1.1f,island));
        float h=Mathf.Lerp(-15f,.95f,shoreline);
        if (island<.85f) h+=Mathf.PerlinNoise(x*.14f+12,z*.14f+32)*.25f;
        foreach(Rect r in Terraces)
        {
            float distance=Mathf.Max(Mathf.Max(r.xMin-x,x-r.xMax),Mathf.Max(r.yMin-z,z-r.yMax));
            float blend=1-Mathf.SmoothStep(0,1,Mathf.Clamp01(distance/1.2f));
            h=Mathf.Lerp(h,1.9f,blend);
        }
        float road=1-Mathf.SmoothStep(0,1,Mathf.InverseLerp(1.85f,2.8f,RoadDistance(new Vector2(x,z))));
        return Mathf.Lerp(h,.45f,road);
    }

    private static void CreateTerrain(Transform parent)
    {
        const int resolution=513;
        var data=new TerrainData {name="Santuario del Alba - painted heightfield",heightmapResolution=resolution,size=new Vector3(72,32,64),alphamapResolution=512,baseMapResolution=512};
        float[,] heights=new float[resolution,resolution];
        for(int z=0;z<resolution;z++) for(int x=0;x<resolution;x++)
            heights[z,x]=(Height(x*72f/(resolution-1)-36,z*64f/(resolution-1)-32)+24)/32;
        data.SetHeights(0,0,heights);
        var layers=new List<TerrainLayer>();
        foreach(string name in new[]{"Grass","Earth","Stone","Sand"})
        {
            string source=AssetDatabase.FindAssets("Layer_TSI_Terrain_"+name+"_01A t:TerrainLayer",new[]{Pack})
                .Select(AssetDatabase.GUIDToAssetPath).First();
            var layer=UnityEngine.Object.Instantiate(AssetDatabase.LoadAssetAtPath<TerrainLayer>(source));
            layer.name="Spring Isles "+name;
            layer.tileSize=new Vector2(5,5);
            AssetDatabase.CreateAsset(layer,Output+"/"+name+".terrainlayer");
            layers.Add(layer);
        }
        data.terrainLayers=layers.ToArray();
        float[,,] splat=new float[512,512,4];
        for(int z=0;z<512;z++) for(int x=0;x<512;x++)
        {
            float wx=x*72f/511-36,wz=z*64f/511-32;
            float road=1-Mathf.SmoothStep(0,1,Mathf.InverseLerp(1.55f,2.35f,RoadDistance(new Vector2(wx,wz))));
            float sand=1-Mathf.SmoothStep(0,1,Mathf.InverseLerp(-.7f,.85f,Height(wx,wz)));
            float stone=Mathf.Clamp01((Mathf.Abs(Height(wx+.3f,wz)-Height(wx-.3f,wz))+Mathf.Abs(Height(wx,wz+.3f)-Height(wx,wz-.3f)))*.7f)*(1-road);
            splat[z,x,1]=road;
            splat[z,x,3]=sand*(1-road);
            splat[z,x,2]=stone*(1-road)*(1-sand);
            splat[z,x,0]=1-splat[z,x,1]-splat[z,x,2]-splat[z,x,3];
        }
        AssetDatabase.CreateAsset(data,Output+"/IslandTerrain.asset");
        var go=Terrain.CreateTerrainGameObject(data);
        Undo.RegisterCreatedObjectUndo(go,"Create painted island");
        go.name="Island terrain - grass, earth, stone and sand";
        go.transform.SetParent(parent,false);
        go.transform.position=new Vector3(-36,-24,-32);
        var terrain=go.GetComponent<Terrain>();
        terrain.materialTemplate=AssetDatabase.LoadAssetAtPath<Material>(Pack+"Models/Materials/TSI_Terrain.mat");
        terrain.heightmapPixelError=5;
        terrain.drawInstanced=true;
        terrain.basemapDistance=120;
        // CreateTerrainGameObject initializes its first layer; paint after that initialization.
        data.SetAlphamaps(0,0,splat);
        EditorUtility.SetDirty(data);
    }

    public static void Polish()
    {
        if (SceneManager.GetActiveScene().path != "Assets/Game/Scenes/Game.unity")
            EditorSceneManager.OpenScene("Assets/Game/Scenes/Game.unity",OpenSceneMode.Single);
        var root=GameObject.Find("Spring Isles - Santuario del Alba");
        if(root==null) throw new InvalidOperationException("Spring Isles map not found.");
        var terrain=root.GetComponentInChildren<Terrain>();
        var data=terrain.terrainData;
        int heightResolution=data.heightmapResolution;
        var heights=new float[heightResolution,heightResolution];
        for(int z=0;z<heightResolution;z++) for(int x=0;x<heightResolution;x++)
            heights[z,x]=(Height(x*72f/(heightResolution-1)-36,z*64f/(heightResolution-1)-32)+24)/32;
        Undo.RecordObject(terrain.transform,"Deepen water outside island");
        terrain.transform.position=new Vector3(-36,-24,-32);
        Undo.RecordObject(data,"Deepen water outside island");
        data.size=new Vector3(72,32,64);
        data.SetHeights(0,0,heights);
        int resolution=data.alphamapResolution;
        float[,,] splat=new float[resolution,resolution,4];
        for(int z=0;z<resolution;z++) for(int x=0;x<resolution;x++)
        {
            float wx=x*72f/(resolution-1)-36,wz=z*64f/(resolution-1)-32;
            float road=1-Mathf.SmoothStep(0,1,Mathf.InverseLerp(1.55f,2.35f,RoadDistance(new Vector2(wx,wz))));
            float sand=1-Mathf.SmoothStep(0,1,Mathf.InverseLerp(-.7f,.85f,Height(wx,wz)));
            float stone=Mathf.Clamp01((Mathf.Abs(Height(wx+.3f,wz)-Height(wx-.3f,wz))+Mathf.Abs(Height(wx,wz+.3f)-Height(wx,wz-.3f)))*.7f);
            splat[z,x,1]=road;
            splat[z,x,3]=sand*(1-road);
            splat[z,x,2]=stone*(1-road)*(1-sand);
            splat[z,x,0]=1-splat[z,x,1]-splat[z,x,2]-splat[z,x,3];
        }
        Undo.RecordObject(data,"Paint enemy trail");
        data.SetAlphamaps(0,0,splat);
        // Hide the flat outer heightfield so the island has an irregular coast in the lagoon.
        int holesResolution=data.holesResolution;
        bool[,] land=new bool[holesResolution,holesResolution];
        for(int z=0;z<holesResolution;z++) for(int x=0;x<holesResolution;x++)
        {
            float wx=x*72f/(holesResolution-1)-36,wz=z*64f/(holesResolution-1)-32;
            float island=Mathf.Sqrt(wx*wx/(25.8f*25.8f)+wz*wz/(20.5f*20.5f));
            land[z,x]=island<1.1f;
        }
        data.SetHoles(0,0,land);
        EditorUtility.SetDirty(data);
        terrain.Flush();
        var variants=new Dictionary<Material,Material>();
        foreach(var renderer in root.GetComponentsInChildren<Renderer>())
        {
            var materials=renderer.sharedMaterials;
            bool changed=false;
            for(int i=0;i<materials.Length;i++)
            {
                Material source=materials[i];
                if(source.shader.name!="ToonScapes/URP/Vegetation") continue;
                if(!variants.TryGetValue(source,out Material variant))
                {
                    string path=Output+"/"+source.name+"_Battlefield.mat";
                    variant=AssetDatabase.LoadAssetAtPath<Material>(path);
                    if(variant==null)
                    {
                        variant=new Material(source);
                        variant.name=source.name+"_Battlefield";
                        variant.SetFloat("_AlphaClipThreshold",.28f);
                        variant.SetFloat("_NormalScale",.6f);
                        variant.SetFloat("_EnableWind",0);
                        variant.DisableKeyword("_ENABLEWIND_ON");
                        AssetDatabase.CreateAsset(variant,path);
                    }
                    variants.Add(source,variant);
                }
                materials[i]=variant;changed=true;
            }
            if(changed)
            {
                Undo.RecordObject(renderer,"Map vegetation material");
                renderer.sharedMaterials=materials;
                PrefabUtility.RecordPrefabInstancePropertyModifications(renderer);
            }
        }
        foreach(var collider in root.GetComponentsInChildren<Collider>())
        {
            if(!PrefabUtility.IsPartOfPrefabInstance(collider)) continue;
            Undo.RecordObject(collider,"Nonblocking decoration");
            collider.enabled=false;
            PrefabUtility.RecordPrefabInstancePropertyModifications(collider);
        }
        var camera=Camera.main;
        Undo.RecordObject(camera.transform,"Frame complete island");
        camera.transform.position=new Vector3(0,42,-35);
        var additional=camera.GetComponent<UnityEngine.Rendering.Universal.UniversalAdditionalCameraData>();
        Undo.RecordObject(additional,"Smooth vegetation edges");
        additional.antialiasing=UnityEngine.Rendering.Universal.AntialiasingMode.SubpixelMorphologicalAntiAliasing;
        additional.antialiasingQuality=UnityEngine.Rendering.Universal.AntialiasingQuality.High;
        additional.renderPostProcessing=true;
        var sunlight=GameObject.Find("Directional Light").GetComponent<Light>();
        Undo.RecordObject(sunlight,"Soften foliage shadows");
        sunlight.shadowStrength=.72f;
        AssetDatabase.SaveAssets();
        EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
        EditorSceneManager.SaveScene(SceneManager.GetActiveScene());
        Validate();
        Capture();
    }

    private static void ConfigureGameplay()
    {
        var spawn=GameObject.Find("SpawnZone");
        Undo.RecordObject(spawn.transform,"Move entrance");
        spawn.transform.SetPositionAndRotation(new Vector3(-18.5f,.45f,12),Quaternion.identity);
        spawn.transform.localScale=Vector3.one;
        var box=spawn.GetComponent<BoxCollider>();
        Undo.RecordObject(box,"Configure spawn area");
        box.center=Vector3.zero;
        box.size=new Vector3(1.3f,.1f,2.4f);
        box.isTrigger=true;
        foreach(var r in spawn.GetComponentsInChildren<Renderer>()) { Undo.RecordObject(r,"Hide spawn debug geometry");r.enabled=false; }
        var destination=GameObject.Find("EnemyDestination").transform;
        Undo.RecordObject(destination,"Move destination");
        destination.position=new Vector3(17,.45f,-10);
        var playerBase=GameObject.Find("PlayerBase");
        Undo.RecordObject(playerBase.transform,"Move base");
        playerBase.transform.position=new Vector3(19,1.5f,-10);
        foreach(var r in playerBase.GetComponents<Renderer>()) {Undo.RecordObject(r,"Replace base visual");r.enabled=false;}
        foreach(var c in playerBase.GetComponents<Collider>()) {Undo.RecordObject(c,"Disable old base geometry");c.enabled=false;}
        var camera=Camera.main;
        Undo.RecordObject(camera.transform,"Frame island");
        camera.transform.SetPositionAndRotation(new Vector3(0,36,-29),Quaternion.Euler(52,0,0));
        var movement=camera.GetComponent<CameraMovement>();
        var so=new SerializedObject(movement);
        so.FindProperty("minX").floatValue=-28;so.FindProperty("maxX").floatValue=28;
        so.FindProperty("minZ").floatValue=-43;so.FindProperty("maxZ").floatValue=20;
        so.ApplyModifiedProperties();
    }

    private static void ConfigureLighting()
    {
        var light=GameObject.Find("Directional Light").GetComponent<Light>();
        Undo.RecordObject(light,"Spring Isles light");
        Undo.RecordObject(light.transform,"Spring Isles sun angle");
        light.transform.rotation=Quaternion.Euler(48,-35,0);
        light.color=new Color(1,.88f,.73f);
        light.intensity=1.35f;
        light.shadows=LightShadows.Soft;
        RenderSettings.ambientMode=AmbientMode.Trilight;
        RenderSettings.ambientSkyColor=new Color(.52f,.65f,.76f);
        RenderSettings.ambientEquatorColor=new Color(.35f,.44f,.43f);
        RenderSettings.ambientGroundColor=new Color(.18f,.22f,.27f);
        RenderSettings.fog=false;
    }

    [MenuItem("Tools/Spring Isles/Validate battlefield")]
    public static void Validate()
    {
        var text=new StringBuilder();
        Physics.SyncTransforms();
        int successes=0;
        for(int i=0;i<9;i++)
        {
            Vector3 start=new Vector3(-19+(i%3)*.5f,.45f,11.2f+(i/3)*.8f);
            var path=new NavMeshPath();
            bool sampled=NavMesh.SamplePosition(start,out NavMeshHit hit,.2f,NavMesh.AllAreas);
            bool complete=sampled && NavMesh.CalculatePath(hit.position,new Vector3(17,.45f,-10),NavMesh.AllAreas,path) && path.status==NavMeshPathStatus.PathComplete;
            if(complete) successes++;
            text.AppendLine($"Spawn {i}: sampled={sampled}, complete={complete}, corners={path.corners.Length}");
        }
        int cells=0;
        for(int z=-20;z<=20;z+=2) for(int x=-26;x<=26;x+=2)
        {
            bool supported=true;
            foreach(var offset in new[]{new Vector2(-.86f,-.86f),new Vector2(-.86f,.86f),new Vector2(.86f,-.86f),new Vector2(.86f,.86f)})
                supported &= Physics.Raycast(new Vector3(x+offset.x,4,z+offset.y),Vector3.down,out _,3,LayerMask.GetMask("BuildableGround"));
            if(supported) cells++;
        }
        var root=GameObject.Find("Spring Isles - Santuario del Alba");
        int missing=root.GetComponentsInChildren<Transform>(true).Sum(t=>GameObjectUtility.GetMonoBehavioursWithMissingScriptCount(t.gameObject));
        int badMaterials=root.GetComponentsInChildren<Renderer>().Sum(r=>r.sharedMaterials.Count(m=>m==null || m.shader==null || !m.shader.isSupported));
        text.AppendLine($"Complete paths: {successes}/9; supported 2m cells: {cells}; missing scripts: {missing}; unsupported/missing materials: {badMaterials}");
        File.WriteAllText(Report,text.ToString());
        if(successes!=9 || cells<30 || missing!=0 || badMaterials!=0) throw new InvalidOperationException(text.ToString());
    }

    [MenuItem("Tools/Spring Isles/Capture battlefield")]
    public static void Capture()
    {
        var camera=Camera.main;
        var previous=camera.targetTexture;
        var active=RenderTexture.active;
        var rt=new RenderTexture(1600,1000,24);
        var image=new Texture2D(1600,1000,TextureFormat.RGB24,false);
        try
        {
            camera.targetTexture=rt;
            camera.Render();
            RenderTexture.active=rt;
            image.ReadPixels(new Rect(0,0,1600,1000),0,0);
            image.Apply();
            Directory.CreateDirectory("Docs");
            File.WriteAllBytes("Docs/SpringIslesPreview.png",image.EncodeToPNG());
        }
        finally
        {
            camera.targetTexture=previous;
            RenderTexture.active=active;
            UnityEngine.Object.DestroyImmediate(image);
            rt.Release();UnityEngine.Object.DestroyImmediate(rt);
        }
    }

}
