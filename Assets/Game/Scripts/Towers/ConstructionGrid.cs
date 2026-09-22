using System;
using System.Collections.Generic;
using UnityEngine;

public sealed class ConstructionGrid : IDisposable
{
    private readonly float cellSize;
    private readonly LayerMask buildableLayer;
    private readonly LayerMask blockedLayer;
    private readonly LayerMask turretLayer;
    private readonly Transform owner;
    private readonly HashSet<Vector2Int> occupiedCells = new HashSet<Vector2Int>();
    private readonly List<GridCell> terrainCells = new List<GridCell>();
    private readonly Dictionary<Vector2Int, GridCell> cellsByCoordinate =
        new Dictionary<Vector2Int, GridCell>();

    private GameObject gridObject;
    private Mesh gridMesh;
    private Material gridMaterial;
    private Mesh shieldMesh;
    private Material shieldMaterial;
    private readonly List<GridVisualCell> visibleCells =
        new List<GridVisualCell>();
    private float borderAnimationTime;
    private float shieldAnimationTime;

    public ConstructionGrid(
        Transform owner,
        float cellSize,
        LayerMask buildableLayer,
        LayerMask blockedLayer,
        LayerMask turretLayer
    )
    {
        this.owner = owner;
        this.cellSize = Mathf.Max(0.5f, cellSize);
        this.buildableLayer = buildableLayer;
        this.blockedLayer = blockedLayer;
        this.turretLayer = turretLayer;

        CreateVisualObject();
        FindTerrainCells();
        Refresh();
        Hide();
    }

    public bool TrySnap(Vector3 worldPosition, out Vector3 snappedPosition)
    {
        Vector2Int coordinate = new Vector2Int(
            Mathf.RoundToInt(worldPosition.x / cellSize),
            Mathf.RoundToInt(worldPosition.z / cellSize)
        );

        if (cellsByCoordinate.TryGetValue(coordinate, out GridCell cell))
        {
            snappedPosition = cell.Position;
            return true;
        }

        snappedPosition = default;
        return false;
    }

    public void Show()
    {
        Refresh();
        gridObject.SetActive(true);
    }

    public void Hide()
    {
        if (gridObject != null)
        {
            gridObject.SetActive(false);
        }
    }

    public void Tick(float deltaTime)
    {
        if (
            gridObject == null ||
            !gridObject.activeSelf ||
            gridMesh == null ||
            visibleCells.Count == 0
        )
        {
            return;
        }

        borderAnimationTime += deltaTime;
        shieldAnimationTime += deltaTime;
        UpdateBorderLighting();
        UpdateShieldLighting();
    }

    public void Refresh()
    {
        if (gridMesh == null)
        {
            return;
        }

        // Selection colliders intentionally extend beyond a tower's cell.
        // Occupancy must not depend on those colliders or construction VFX scale.
        occupiedCells.Clear();
        foreach (TowerTargeting tower in UnityEngine.Object.FindObjectsByType<TowerTargeting>(
            FindObjectsSortMode.None))
        {
            // Build previews have targeting disabled and must never reserve cells.
            if (!tower.isActiveAndEnabled ||
                !IsInLayerMask(tower.gameObject.layer, turretLayer))
            {
                continue;
            }

            occupiedCells.Add(GetCoordinate(tower.transform.position));
        }

        List<Vector3> vertices = new List<Vector3>();
        List<int> triangles = new List<int>();
        List<Vector3> shieldVertices = new List<Vector3>();
        List<int> shieldTriangles = new List<int>();
        visibleCells.Clear();

        foreach (GridCell cell in terrainCells)
        {
            if (!IsCellOccupied(cell.Position))
            {
                int firstVertex = vertices.Count;
                AddCellBorder(cell.Position, vertices, triangles);
                int shieldFirstVertex = shieldVertices.Count;
                AddShieldSurface(
                    cell.Position,
                    shieldVertices,
                    shieldTriangles
                );
                visibleCells.Add(
                    new GridVisualCell(firstVertex, shieldFirstVertex)
                );
            }
        }

        gridMesh.Clear();
        gridMesh.SetVertices(vertices);
        gridMesh.SetTriangles(triangles, 0);
        shieldMesh.Clear();
        shieldMesh.SetVertices(shieldVertices);
        shieldMesh.SetTriangles(shieldTriangles, 0);
        UpdateBorderLighting();
        UpdateShieldLighting();
        gridMesh.RecalculateBounds();
        shieldMesh.RecalculateBounds();
    }

    public void Dispose()
    {
        if (gridMesh != null)
        {
            UnityEngine.Object.Destroy(gridMesh);
        }

        if (shieldMesh != null)
        {
            UnityEngine.Object.Destroy(shieldMesh);
        }

        if (gridMaterial != null)
        {
            UnityEngine.Object.Destroy(gridMaterial);
        }

        if (shieldMaterial != null)
        {
            UnityEngine.Object.Destroy(shieldMaterial);
        }

        if (gridObject != null)
        {
            UnityEngine.Object.Destroy(gridObject);
        }
    }

    private void CreateVisualObject()
    {
        gridObject = new GameObject("Construction Grid");
        gridObject.transform.SetParent(owner, true);

        MeshFilter filter = gridObject.AddComponent<MeshFilter>();
        MeshRenderer renderer = gridObject.AddComponent<MeshRenderer>();

        gridMesh = new Mesh { name = "Construction Grid Mesh" };
        gridMesh.MarkDynamic();
        filter.sharedMesh = gridMesh;

        Shader shader = Shader.Find("Sprites/Default");
        gridMaterial = new Material(shader)
        {
            name = "Construction Grid Material",
            color = new Color(0.2f, 0.9f, 1f, 0.72f)
        };
        renderer.sharedMaterial = gridMaterial;
        renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        renderer.receiveShadows = false;
        renderer.sortingOrder = 5;

        GameObject shieldObject = new GameObject("Grid Shield Surface");
        shieldObject.transform.SetParent(gridObject.transform, true);

        MeshFilter shieldFilter = shieldObject.AddComponent<MeshFilter>();
        MeshRenderer shieldRenderer = shieldObject.AddComponent<MeshRenderer>();
        shieldMesh = new Mesh { name = "Construction Grid Shield Mesh" };
        shieldMesh.MarkDynamic();
        shieldFilter.sharedMesh = shieldMesh;

        shieldMaterial = new Material(shader)
        {
            name = "Construction Grid Shield Material",
            color = Color.white
        };
        shieldRenderer.sharedMaterial = shieldMaterial;
        shieldRenderer.shadowCastingMode =
            UnityEngine.Rendering.ShadowCastingMode.Off;
        shieldRenderer.receiveShadows = false;
        shieldRenderer.sortingOrder = 4;
    }

    private void FindTerrainCells()
    {
        Collider[] colliders = UnityEngine.Object.FindObjectsByType<Collider>(
            FindObjectsSortMode.None
        );
        bool foundBuildableCollider = false;
        Bounds combinedBounds = default;

        foreach (Collider collider in colliders)
        {
            if (!IsInLayerMask(collider.gameObject.layer, buildableLayer))
            {
                continue;
            }

            if (!foundBuildableCollider)
            {
                combinedBounds = collider.bounds;
                foundBuildableCollider = true;
            }
            else
            {
                combinedBounds.Encapsulate(collider.bounds);
            }
        }

        if (!foundBuildableCollider)
        {
            Debug.LogWarning("No buildable colliders were found for the construction grid.");
            return;
        }

        int minX = Mathf.CeilToInt(combinedBounds.min.x / cellSize);
        int maxX = Mathf.FloorToInt(combinedBounds.max.x / cellSize);
        int minZ = Mathf.CeilToInt(combinedBounds.min.z / cellSize);
        int maxZ = Mathf.FloorToInt(combinedBounds.max.z / cellSize);
        float rayHeight = combinedBounds.max.y + 2f;

        for (int x = minX; x <= maxX; x++)
        {
            for (int z = minZ; z <= maxZ; z++)
            {
                Vector3 rayOrigin = new Vector3(
                    x * cellSize,
                    rayHeight,
                    z * cellSize
                );

                if (!Physics.Raycast(
                    rayOrigin,
                    Vector3.down,
                    out RaycastHit hit,
                    rayHeight + 10f,
                    buildableLayer
                ))
                {
                    continue;
                }

                Vector3 position = new Vector3(
                    rayOrigin.x,
                    hit.point.y,
                    rayOrigin.z
                );

                if (!IsSquareFullySupported(position))
                {
                    continue;
                }

                GridCell cell = new GridCell(
                    new Vector2Int(x, z),
                    position
                );
                terrainCells.Add(cell);
                cellsByCoordinate.Add(cell.Coordinate, cell);
            }
        }

    }

    private void UpdateBorderLighting()
    {
        if (gridMesh == null || gridMesh.vertexCount == 0)
        {
            return;
        }

        List<Color> colors = new List<Color>(gridMesh.vertexCount);

        for (int index = 0; index < gridMesh.vertexCount; index++)
        {
            colors.Add(new Color(0.08f, 0.5f, 1f, 0.32f));
        }

        for (int cellIndex = 0; cellIndex < visibleCells.Count; cellIndex++)
        {
            GridVisualCell cell = visibleCells[cellIndex];
            float phase = Mathf.Repeat(
                borderAnimationTime * 0.65f,
                1f
            );

            for (int vertexIndex = 0; vertexIndex < 16; vertexIndex++)
            {
                int edgeIndex = vertexIndex / 4;
                int edgeVertexIndex = vertexIndex % 4;
                float perimeterProgress = (
                    edgeIndex + edgeVertexIndex / 3f
                ) / 4f;
                float distance = Mathf.Abs(
                    Mathf.DeltaAngle(
                        phase * 360f,
                        perimeterProgress * 360f
                    )
                ) / 180f;
                float brightness = Mathf.SmoothStep(
                    0f,
                    1f,
                    1f - Mathf.Clamp01(distance / 0.22f)
                );

                colors[cell.FirstVertex + vertexIndex] = Color.Lerp(
                    colors[cell.FirstVertex + vertexIndex],
                    new Color(0.7f, 1f, 1f, 1f),
                    brightness
                );
            }
        }

        gridMesh.SetColors(colors);
    }

    private void UpdateShieldLighting()
    {
        if (shieldMesh == null || shieldMesh.vertexCount == 0)
        {
            return;
        }

        List<Color> colors = new List<Color>(shieldMesh.vertexCount);

        for (int index = 0; index < shieldMesh.vertexCount; index++)
        {
            colors.Add(new Color(0.08f, 0.45f, 1f, 0.018f));
        }

        for (int cellIndex = 0; cellIndex < visibleCells.Count; cellIndex++)
        {
            GridVisualCell cell = visibleCells[cellIndex];
            float pulse = Mathf.Repeat(
                shieldAnimationTime * 0.32f,
                1f
            );

            for (int vertexIndex = 0; vertexIndex < 9; vertexIndex++)
            {
                int row = vertexIndex / 3;
                int column = vertexIndex % 3;
                Vector2 centered = new Vector2(
                    (column - 1f) * 0.5f,
                    (row - 1f) * 0.5f
                );
                float distanceFromCenter = centered.magnitude / 0.7071f;
                float distanceFromWave = Mathf.Abs(
                    distanceFromCenter - pulse
                );
                float brightness = Mathf.SmoothStep(
                    0f,
                    1f,
                    1f - Mathf.Clamp01(distanceFromWave / 0.2f)
                );

                colors[cell.ShieldFirstVertex + vertexIndex] =
                    Color.Lerp(
                        new Color(0.08f, 0.45f, 1f, 0.018f),
                        new Color(0.35f, 0.9f, 1f, 0.12f),
                        brightness
                    );
            }
        }

        shieldMesh.SetColors(colors);
    }

    private bool IsSquareFullySupported(Vector3 center)
    {
        float sampleOffset = cellSize * 0.43f;
        float expectedY = center.y;

        for (int x = -1; x <= 1; x += 2)
        {
            for (int z = -1; z <= 1; z += 2)
            {
                Vector3 origin = center + new Vector3(
                    x * sampleOffset,
                    1.5f,
                    z * sampleOffset
                );

                if (!Physics.Raycast(
                    origin,
                    Vector3.down,
                    out RaycastHit hit,
                    3f,
                    buildableLayer
                ) || Mathf.Abs(hit.point.y - expectedY) > 0.08f)
                {
                    return false;
                }
            }
        }

        Vector3 halfExtents = new Vector3(
            cellSize * 0.42f,
            0.35f,
            cellSize * 0.42f
        );
        return !Physics.CheckBox(
            center + Vector3.up * 0.2f,
            halfExtents,
            Quaternion.identity,
            blockedLayer
        );
    }

    private Vector2Int GetCoordinate(Vector3 position)
    {
        return new Vector2Int(
            Mathf.RoundToInt(position.x / cellSize),
            Mathf.RoundToInt(position.z / cellSize)
        );
    }

    public bool IsCellOccupied(Vector3 position)
    {
        return occupiedCells.Contains(GetCoordinate(position));
    }

    private void AddCellBorder(
        Vector3 center,
        List<Vector3> vertices,
        List<int> triangles
    )
    {
        center.y += 0.035f;
        float half = cellSize * 0.46f;
        float thickness = Mathf.Clamp(cellSize * 0.035f, 0.045f, 0.09f);

        AddQuad(center, -half, half, half - thickness, half, vertices, triangles);
        AddQuad(center, -half, -half + thickness, -half, half, vertices, triangles);
        AddQuad(center, -half, half, -half, -half + thickness, vertices, triangles);
        AddQuad(center, half - thickness, half, -half, half, vertices, triangles);
    }

    private void AddShieldSurface(
        Vector3 center,
        List<Vector3> vertices,
        List<int> triangles
    )
    {
        center.y += 0.02f;
        float half = cellSize * 0.4f;
        int firstVertex = vertices.Count;

        for (int row = 0; row < 3; row++)
        {
            for (int column = 0; column < 3; column++)
            {
                float x = Mathf.Lerp(-half, half, column / 2f);
                float z = Mathf.Lerp(-half, half, row / 2f);
                vertices.Add(center + new Vector3(x, 0f, z));
            }
        }

        for (int row = 0; row < 2; row++)
        {
            for (int column = 0; column < 2; column++)
            {
                int bottomLeft = firstVertex + row * 3 + column;
                triangles.Add(bottomLeft);
                triangles.Add(bottomLeft + 3);
                triangles.Add(bottomLeft + 4);
                triangles.Add(bottomLeft);
                triangles.Add(bottomLeft + 4);
                triangles.Add(bottomLeft + 1);
            }
        }
    }

    private static void AddQuad(
        Vector3 center,
        float minX,
        float maxX,
        float minZ,
        float maxZ,
        List<Vector3> vertices,
        List<int> triangles
    )
    {
        int firstVertex = vertices.Count;
        vertices.Add(center + new Vector3(minX, 0f, minZ));
        vertices.Add(center + new Vector3(minX, 0f, maxZ));
        vertices.Add(center + new Vector3(maxX, 0f, maxZ));
        vertices.Add(center + new Vector3(maxX, 0f, minZ));

        triangles.Add(firstVertex);
        triangles.Add(firstVertex + 1);
        triangles.Add(firstVertex + 2);
        triangles.Add(firstVertex);
        triangles.Add(firstVertex + 2);
        triangles.Add(firstVertex + 3);
    }

    private static bool IsInLayerMask(int layer, LayerMask mask)
    {
        return (mask.value & (1 << layer)) != 0;
    }

    private readonly struct GridCell
    {
        public Vector2Int Coordinate { get; }
        public Vector3 Position { get; }

        public GridCell(Vector2Int coordinate, Vector3 position)
        {
            Coordinate = coordinate;
            Position = position;
        }
    }

    private readonly struct GridVisualCell
    {
        public int FirstVertex { get; }
        public int ShieldFirstVertex { get; }

        public GridVisualCell(int firstVertex, int shieldFirstVertex)
        {
            FirstVertex = firstVertex;
            ShieldFirstVertex = shieldFirstVertex;
        }
    }
}
