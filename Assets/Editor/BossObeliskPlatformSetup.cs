using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.Tilemaps;

public static class BossObeliskPlatformSetup
{
    private const string ScenePath = "Assets/Scenes/Boss.unity";
    private const string GroundName = "Ground";
    private const string PlatformName = "ObeliskPlatforms";

    [MenuItem("Tools/Boss/Configure Obelisk One-Way Platforms")]
    public static void ConfigurePlatforms()
    {
        Scene scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
        Tilemap ground = FindTilemap(GroundName);
        if (ground == null || ground.transform.parent == null)
        {
            throw new InvalidOperationException("Grid/Ground Tilemap was not found in Boss scene.");
        }

        Tilemap platforms = GetOrCreatePlatformTilemap(ground);
        ConfigurePlatformComponents(ground, platforms);

        List<Vector3Int> groundCells = GetOccupiedCells(ground);
        if (groundCells.Count == 0)
        {
            throw new InvalidOperationException("Boss Ground Tilemap has no tiles.");
        }

        int floorY = groundCells
            .GroupBy(cell => cell.y)
            .OrderByDescending(group => group.Count())
            .First()
            .Key;
        int leftWallX = groundCells.Min(cell => cell.x);
        int rightWallX = groundCells.Max(cell => cell.x);

        List<Vector3Int> cellsToMove = groundCells
            .Where(cell => cell.y > floorY && cell.x > leftWallX && cell.x < rightWallX)
            .ToList();

        foreach (Vector3Int cell in cellsToMove)
        {
            MoveTile(ground, platforms, cell);
        }

        ground.CompressBounds();
        platforms.CompressBounds();
        ground.RefreshAllTiles();
        platforms.RefreshAllTiles();

        EditorUtility.SetDirty(ground);
        EditorUtility.SetDirty(platforms);
        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        AssetDatabase.SaveAssets();

        Debug.Log(
            $"OBELISK PLATFORM SETUP COMPLETE: moved {cellsToMove.Count} elevated tiles "
            + $"from Ground to {PlatformName}; floorY={floorY}.");
    }

    public static void ValidatePlatforms()
    {
        Scene scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
        Tilemap ground = FindTilemap(GroundName);
        Tilemap platforms = FindTilemap(PlatformName);

        Require(ground != null, "Ground Tilemap is missing.");
        Require(platforms != null, "ObeliskPlatforms Tilemap is missing.");
        Require(platforms.transform.parent == ground.transform.parent,
            "ObeliskPlatforms must be a sibling of Ground under Grid.");
        Require(platforms.gameObject.layer == ground.gameObject.layer,
            "ObeliskPlatforms must stay on the Ground layer.");
        Require(platforms.CompareTag("ground"),
            "ObeliskPlatforms must keep the ground tag.");

        TilemapCollider2D tilemapCollider = platforms.GetComponent<TilemapCollider2D>();
        Rigidbody2D body = platforms.GetComponent<Rigidbody2D>();
        PlatformEffector2D effector = platforms.GetComponent<PlatformEffector2D>();
        ObeliskPlatformSurface projectilePassThrough =
            platforms.GetComponent<ObeliskPlatformSurface>();

        Require(tilemapCollider != null && tilemapCollider.enabled,
            "ObeliskPlatforms TilemapCollider2D is missing or disabled.");
        Require(tilemapCollider.usedByEffector,
            "ObeliskPlatforms collider is not using its PlatformEffector2D.");
        Require(body != null && body.bodyType == RigidbodyType2D.Static,
            "ObeliskPlatforms Rigidbody2D must be Static.");
        Require(effector != null && effector.useOneWay,
            "ObeliskPlatforms must use one-way collision.");
        Require(projectilePassThrough != null,
            "ObeliskPlatforms is missing its Fireball pass-through marker.");

        List<Vector3Int> platformCells = GetOccupiedCells(platforms);
        Require(platformCells.Count > 0, "ObeliskPlatforms contains no tiles.");

        List<Vector3Int> groundCells = GetOccupiedCells(ground);
        int floorY = groundCells
            .GroupBy(cell => cell.y)
            .OrderByDescending(group => group.Count())
            .First()
            .Key;
        int leftWallX = groundCells.Min(cell => cell.x);
        int rightWallX = groundCells.Max(cell => cell.x);
        bool groundStillContainsElevatedPlatforms = groundCells.Any(cell =>
            cell.y > floorY && cell.x > leftWallX && cell.x < rightWallX);

        Require(!groundStillContainsElevatedPlatforms,
            "Ground still contains elevated interior platform tiles.");

        Debug.Log(
            $"OBELISK PLATFORM VALIDATION PASSED: {platformCells.Count} one-way tiles; "
            + "floor and arena walls remain solid.");
    }

    private static Tilemap GetOrCreatePlatformTilemap(Tilemap ground)
    {
        Transform existing = ground.transform.parent.Find(PlatformName);
        if (existing != null)
        {
            Tilemap existingTilemap = existing.GetComponent<Tilemap>();
            if (existingTilemap == null)
            {
                throw new InvalidOperationException(
                    PlatformName + " exists but has no Tilemap component.");
            }

            return existingTilemap;
        }

        GameObject platformObject = new GameObject(PlatformName);
        Undo.RegisterCreatedObjectUndo(platformObject, "Create Obelisk Platforms");
        platformObject.transform.SetParent(ground.transform.parent, false);
        platformObject.layer = ground.gameObject.layer;
        platformObject.tag = "ground";

        Tilemap platformTilemap = platformObject.AddComponent<Tilemap>();
        platformObject.AddComponent<TilemapRenderer>();
        return platformTilemap;
    }

    private static void ConfigurePlatformComponents(Tilemap ground, Tilemap platforms)
    {
        platforms.gameObject.layer = ground.gameObject.layer;
        platforms.gameObject.tag = "ground";
        platforms.animationFrameRate = ground.animationFrameRate;
        platforms.color = ground.color;
        platforms.tileAnchor = ground.tileAnchor;
        platforms.orientation = ground.orientation;
        platforms.orientationMatrix = ground.orientationMatrix;

        TilemapRenderer sourceRenderer = ground.GetComponent<TilemapRenderer>();
        TilemapRenderer platformRenderer = platforms.GetComponent<TilemapRenderer>();
        if (platformRenderer == null)
        {
            platformRenderer = platforms.gameObject.AddComponent<TilemapRenderer>();
        }

        if (sourceRenderer != null)
        {
            platformRenderer.sharedMaterial = sourceRenderer.sharedMaterial;
            platformRenderer.sortingLayerID = sourceRenderer.sortingLayerID;
            platformRenderer.sortingOrder = sourceRenderer.sortingOrder;
        }

        TilemapCollider2D tilemapCollider = platforms.GetComponent<TilemapCollider2D>();
        if (tilemapCollider == null)
        {
            tilemapCollider = platforms.gameObject.AddComponent<TilemapCollider2D>();
        }

        Rigidbody2D body = platforms.GetComponent<Rigidbody2D>();
        if (body == null)
        {
            body = platforms.gameObject.AddComponent<Rigidbody2D>();
        }

        body.bodyType = RigidbodyType2D.Static;
        body.simulated = true;
        body.gravityScale = 0f;

        PlatformEffector2D effector = platforms.GetComponent<PlatformEffector2D>();
        if (effector == null)
        {
            effector = platforms.gameObject.AddComponent<PlatformEffector2D>();
        }

        effector.useOneWay = true;
        effector.useOneWayGrouping = true;
        effector.surfaceArc = 170f;
        effector.useSideFriction = false;
        effector.useSideBounce = false;
        tilemapCollider.usedByEffector = true;

        if (platforms.GetComponent<ObeliskPlatformSurface>() == null)
        {
            platforms.gameObject.AddComponent<ObeliskPlatformSurface>();
        }
    }

    private static void MoveTile(Tilemap source, Tilemap destination, Vector3Int cell)
    {
        TileBase tile = source.GetTile(cell);
        if (tile == null)
        {
            return;
        }

        Matrix4x4 transformMatrix = source.GetTransformMatrix(cell);
        Color color = source.GetColor(cell);
        TileFlags flags = source.GetTileFlags(cell);

        destination.SetTile(cell, tile);
        destination.SetTileFlags(cell, TileFlags.None);
        destination.SetTransformMatrix(cell, transformMatrix);
        destination.SetColor(cell, color);
        destination.SetTileFlags(cell, flags);
        source.SetTile(cell, null);
    }

    private static List<Vector3Int> GetOccupiedCells(Tilemap tilemap)
    {
        List<Vector3Int> cells = new List<Vector3Int>();
        foreach (Vector3Int cell in tilemap.cellBounds.allPositionsWithin)
        {
            if (tilemap.HasTile(cell))
            {
                cells.Add(cell);
            }
        }

        return cells;
    }

    private static Tilemap FindTilemap(string objectName)
    {
        return UnityEngine.Object.FindObjectsByType<Tilemap>(
                FindObjectsInactive.Include,
                FindObjectsSortMode.None)
            .FirstOrDefault(tilemap =>
                tilemap.name == objectName
                && tilemap.transform.parent != null
                && tilemap.transform.parent.name == "Grid");
    }

    private static void Require(bool condition, string message)
    {
        if (!condition)
        {
            throw new InvalidOperationException(message);
        }
    }
}
