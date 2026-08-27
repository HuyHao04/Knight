using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.Tilemaps;

public static class Level3PopulationSetup
{
    private const string ScenePath = "Assets/Scenes/Level_3.unity";
    private const string CoinPrefabPath = "Assets/Prelabs/Coin.prefab";
    private const string HeartPrefabPath = "Assets/Prelabs/Heart.prefab";
    private const string SlimePrefabPath = "Assets/Prelabs/Slime.prefab";
    private const string ArcherPrefabPath = "Assets/Prelabs/SkeletonArcher.prefab";
    private const string WarriorPrefabPath = "Assets/Prelabs/Skeleton_Warrior.prefab";
    private const string SpearmanPrefabPath = "Assets/Prelabs/Skeleton_Spearman.prefab";

    private readonly struct Placement
    {
        public readonly string PrefabPath;
        public readonly string Name;
        public readonly Vector3 Position;

        public Placement(string prefabPath, string name, float x, float y, float z = -3f)
        {
            PrefabPath = prefabPath;
            Name = name;
            Position = new Vector3(x, y, z);
        }
    }

    private static readonly Placement[] CoinPlacements =
    {
        CoinGroup("Coin_3_DiagonalDown", "Coins_01_EntranceDrop", -73.5f, -2.5f),
        CoinGroup("Coin_3_Horizontal", "Coins_02_FirstIsland", -69.5f, -3.5f),
        CoinGroup("Coin_3_Horizontal", "Coins_03_FirstGap", -65.5f, -3.4f),
        CoinGroup("Coin_3_DiagonalUp", "Coins_04_StoneRise", -61.8f, -3.4f),
        CoinGroup("Coin_2_DiagonalDown", "Coins_05_HighLedgeDrop", -58.3f, -0.8f),
        CoinGroup("Coin_3_DiagonalUp", "Coins_06_TreeLedge", -56.8f, -1.0f),

        new Placement(CoinPrefabPath, "Coin_07_PillarA", -49.5f, -3.4f),
        new Placement(CoinPrefabPath, "Coin_08_PillarB", -46.5f, -2.4f),
        new Placement(CoinPrefabPath, "Coin_09_PillarC", -43.5f, -1.4f),
        new Placement(CoinPrefabPath, "Coin_10_PillarD", -40.5f, -0.4f),
        new Placement(CoinPrefabPath, "Coin_11_PillarE", -37.5f, -0.4f),

        CoinGroup("Coin_3_DiagonalUp", "Coins_12_HillClimb", -32.0f, 2.3f),
        CoinGroup("Coin_3_Horizontal", "Coins_13_HillTop", -28.0f, 4.3f),
        CoinGroup("Coin_3_DiagonalDown", "Coins_14_HillDescent", -24.5f, 2.6f),
        CoinGroup("Coin_3_Horizontal", "Coins_15_LongIsland", -15.5f, -3.3f),
        CoinGroup("Coin_3_DiagonalUp", "Coins_16_LiftApproach", -4.0f, -1.0f),
        CoinGroup("Coin_2_Horizontal", "Coins_17_FirstLift", 0.8f, 3.2f),
        CoinGroup("Coin_3_DiagonalUp", "Coins_18_HighIslandRise", 3.2f, 4.8f),
        CoinGroup("Coin_3_Horizontal", "Coins_19_HighIsland", 5.5f, 6.3f),

        CoinGroup("Coin_3_Horizontal", "Coins_20_MovingStepA", 13.5f, -0.6f),
        CoinGroup("Coin_2_Horizontal", "Coins_21_MovingStepB", 18.5f, -2.6f),
        CoinGroup("Coin_3_Horizontal", "Coins_22_MovingStepC", 23.5f, -0.6f),
        CoinGroup("Coin_2_Horizontal", "Coins_23_MovingStepD", 28.5f, -2.6f),
        CoinGroup("Coin_3_Horizontal", "Coins_24_MovingStepE", 33.5f, -0.6f),
        CoinGroup("Coin_3_Horizontal", "Coins_25_MidRest", 41.5f, -1.6f),
        CoinGroup("Coin_3_DiagonalUp", "Coins_26_HorizontalPlatformA", 46.5f, 1.1f),
        CoinGroup("Coin_3_Horizontal", "Coins_27_HorizontalPlatformB", 58.5f, 1.2f),
        CoinGroup("Coin_3_Horizontal", "Coins_28_LastLift", 65.5f, 1.2f),
        CoinGroup("Coin_3_Horizontal", "Coins_29_CheckpointIsland", 72.5f, 1.4f),

        CoinGroup("Coin_3_DiagonalUp", "Coins_30_UpperRouteRise", 79.0f, 4.2f),
        CoinGroup("Coin_3_Horizontal", "Coins_31_FallingPlatformA", 84.0f, 6.2f),
        CoinGroup("Coin_3_Horizontal", "Coins_32_FallingPlatformB", 89.5f, 6.2f),
        CoinGroup("Coin_3_DiagonalDown", "Coins_33_UpperRouteDrop", 95.0f, 4.2f),
        CoinGroup("Coin_3_DiagonalUp", "Coins_34_RuinRise", 101.0f, 1.0f),
        CoinGroup("Coin_3_Horizontal", "Coins_35_RuinTop", 104.0f, 4.4f),
        CoinGroup("Coin_2_DiagonalDown", "Coins_36_RuinDrop", 110.0f, 1.6f),
        CoinGroup("Coin_3_Horizontal", "Coins_37_FallingBridge", 114.5f, 4.4f),
        CoinGroup("Coin_3_Horizontal", "Coins_38_StoneRest", 121.0f, 3.4f),
        CoinGroup("Coin_2_Horizontal", "Coins_39_MovingGroundA", 127.5f, 2.4f),
        CoinGroup("Coin_2_Horizontal", "Coins_40_MovingGroundB", 133.5f, 2.4f),
        CoinGroup("Coin_3_Horizontal", "Coins_41_FinalIslandA", 139.5f, 2.4f),
        CoinGroup("Coin_3_Horizontal", "Coins_42_FinalBridge", 147.5f, 2.4f),
        CoinGroup("Coin_3_Horizontal", "Coins_43_PortalApproach", 153.0f, 2.5f)
    };

    private static readonly Placement[] EnemyPlacements =
    {
        new Placement(SlimePrefabPath, "Slime_EarlyPatrol", -70.0f, -4.65f),
        new Placement(ArcherPrefabPath, "SkeletonArcher_FirstLedge", -60.5f, -1.28f),
        new Placement(WarriorPrefabPath, "Skeleton_Warrior_Hill", -25.0f, 1.82f),
        new Placement(SpearmanPrefabPath, "Skeleton_Spearman_LongIsland", -14.5f, -4.20f),
        new Placement(ArcherPrefabPath, "SkeletonArcher_HighIsland", 5.5f, 5.72f),
        new Placement(ArcherPrefabPath, "SkeletonArcher_MidRest", 41.5f, -2.28f),
        new Placement(SlimePrefabPath, "Slime_CheckpointIsland", 72.5f, 0.35f),
        new Placement(WarriorPrefabPath, "Skeleton_Warrior_RuinTop", 103.5f, 3.82f),
        new Placement(ArcherPrefabPath, "SkeletonArcher_FinalRoute", 120.5f, 2.72f),
        new Placement(SpearmanPrefabPath, "Skeleton_Spearman_PortalGuard", 153.0f, 1.80f)
    };

    [MenuItem("Tools/Level 3/Arrange Coins And Enemies")]
    public static void ArrangePopulation()
    {
        Scene scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
        Transform itemRoot = FindSceneRoot(scene, "Item");
        Transform enemyRoot = FindSceneRoot(scene, "Enemy");
        Require(itemRoot != null, "Level_3 is missing its Item root.");
        Require(enemyRoot != null, "Level_3 is missing its Enemy root.");

        ClearChildren(itemRoot);
        ClearChildren(enemyRoot);
        RemoveLegacyRootBat(scene);
        DisableBatSpawner(scene);

        foreach (Placement placement in CoinPlacements)
        {
            PlacePrefab(scene, itemRoot, placement);
        }

        PlacePrefab(
            scene,
            itemRoot,
            new Placement(HeartPrefabPath, "Heart_MidpointRecovery", 74.0f, 1.55f));

        foreach (Placement placement in EnemyPlacements)
        {
            GameObject enemy = PlacePrefab(scene, enemyRoot, placement);
            ConfigureEnemy(enemy);
        }

        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        AssetDatabase.SaveAssets();
        ValidatePopulation();
        Debug.Log(
            "LEVEL 3 POPULATION COMPLETE: 43 coin routes, one recovery Heart, " +
            EnemyPlacements.Length + " fixed enemy encounters, BatSpawner disabled.");
    }

    [MenuItem("Tools/Level 3/Validate Coins And Enemies")]
    public static void ValidatePopulation()
    {
        Scene scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
        Transform itemRoot = FindSceneRoot(scene, "Item");
        Transform enemyRoot = FindSceneRoot(scene, "Enemy");
        Require(itemRoot != null, "Level_3 is missing its Item root.");
        Require(enemyRoot != null, "Level_3 is missing its Enemy root.");

        int coinCount = 0;
        foreach (Transform child in itemRoot.GetComponentsInChildren<Transform>(true))
        {
            if (child.CompareTag("Coin"))
            {
                coinCount++;
                Collider2D trigger = child.GetComponent<Collider2D>();
                Require(trigger != null && trigger.isTrigger, child.name + " needs a pickup trigger.");
            }
        }

        Require(itemRoot.childCount == CoinPlacements.Length + 1,
            "Item root does not contain the planned coin routes and Heart.");
        Require(coinCount == 112, "Expected 112 individual coins, found " + coinCount + ".");
        bool hasHeart = false;
        foreach (Transform child in itemRoot.GetComponentsInChildren<Transform>(true))
        {
            if (child.CompareTag("Heart"))
            {
                hasHeart = true;
                break;
            }
        }
        Require(hasHeart, "Midpoint recovery Heart is missing.");

        int slimeCount = enemyRoot.GetComponentsInChildren<EnemyController>(true).Length;
        int archerCount = enemyRoot.GetComponentsInChildren<SkeletonArcher>(true).Length;
        int meleeSkeletonCount = enemyRoot.GetComponentsInChildren<SkeletonEnemy>(true).Length;
        Require(enemyRoot.childCount == EnemyPlacements.Length,
            "Enemy root does not contain the planned encounters.");
        Require(slimeCount == 2, "Expected two Slimes.");
        Require(archerCount == 4, "Expected four Skeleton Archers.");
        Require(meleeSkeletonCount == 4, "Expected four melee Skeletons.");

        foreach (Transform enemy in enemyRoot)
        {
            Require(enemy.CompareTag("enemy"), enemy.name + " must keep the enemy tag.");
            Require(enemy.position.x > -74f && enemy.position.x < 155f,
                enemy.name + " is too close to an entrance or Portal boundary.");
        }

        Transform batSpawner = FindSceneRoot(scene, "BatSpawner");
        Require(batSpawner == null || !batSpawner.gameObject.activeSelf,
            "BatSpawner must be disabled so Level_3 enemy density stays deterministic.");
        Require(FindSceneRoot(scene, "Bat") == null,
            "The old root Bat should be removed from Level_3.");
        Require(Object.FindFirstObjectByType<LevelCountdownTimer>(FindObjectsInactive.Include) != null,
            "Level_3 lost its countdown timer while population was edited.");
        Require(FindSceneRoot(scene, "EntrancePortal") != null,
            "Level_3 lost its entrance Portal while population was edited.");

        Debug.Log(
            "LEVEL 3 POPULATION VALIDATION PASSED: " + coinCount +
            " coins, " + EnemyPlacements.Length + " enemies, one Heart, systems preserved.");
    }

    [MenuItem("Tools/Level 3/Report Population Geometry")]
    public static void ReportGeometry()
    {
        Scene scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
        var report = new StringBuilder();
        report.AppendLine("LEVEL 3 POPULATION GEOMETRY");

        foreach (GameObject root in scene.GetRootGameObjects())
        {
            if (root.CompareTag("Player") || root.GetComponentInChildren<PlayerController>(true) != null)
            {
                PlayerController player = root.GetComponentInChildren<PlayerController>(true);
                if (player != null)
                {
                    report.AppendLine($"PLAYER {GetPath(player.transform)} @ {player.transform.position}");
                }
            }

            AppendNamedPosition(root.transform, "Portal", report);
            AppendNamedPosition(root.transform, "EntrancePortal", report);
        }

        foreach (Tilemap tilemap in Object.FindObjectsByType<Tilemap>(FindObjectsInactive.Include, FindObjectsSortMode.None))
        {
            report.AppendLine($"\nTILEMAP {GetPath(tilemap.transform)} bounds={tilemap.cellBounds} world={tilemap.localBounds}");
            AppendTopSurfaceSegments(tilemap, report);
        }

        report.AppendLine("\nCOLLIDERS");
        foreach (Collider2D collider in Object.FindObjectsByType<Collider2D>(FindObjectsInactive.Include, FindObjectsSortMode.None))
        {
            if (collider.isTrigger || collider.GetComponentInParent<PlayerController>() != null)
            {
                continue;
            }

            string path = GetPath(collider.transform);
            if (path.Contains("Grid/") || path.Contains("ground/"))
            {
                Bounds bounds = collider.bounds;
                report.AppendLine($"{path} {collider.GetType().Name} center={bounds.center} size={bounds.size} tag={collider.tag}");
            }
        }

        report.AppendLine("\nCURRENT POPULATION");
        foreach (GameObject root in scene.GetRootGameObjects())
        {
            if (root.name == "Enemy" || root.name == "Item" || root.name == "Bat" || root.name == "BatSpawner")
            {
                AppendHierarchy(root.transform, report, 0);
            }
        }

        Debug.Log(report.ToString());
    }

    [MenuItem("Tools/Level 3/Render Population Overview")]
    public static void RenderOverview()
    {
        EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
        GameObject cameraObject = new GameObject("Level3OverviewCamera");
        Camera camera = cameraObject.AddComponent<Camera>();
        camera.orthographic = true;
        camera.orthographicSize = 15f;
        camera.transform.position = new Vector3(40f, -2f, -20f);
        camera.clearFlags = CameraClearFlags.SolidColor;
        camera.backgroundColor = new Color(0.08f, 0.1f, 0.16f, 1f);

        const int width = 4096;
        const int height = 512;
        RenderTexture renderTexture = new RenderTexture(width, height, 24);
        camera.targetTexture = renderTexture;
        camera.aspect = (float)width / height;
        camera.Render();

        RenderTexture previous = RenderTexture.active;
        RenderTexture.active = renderTexture;
        Texture2D image = new Texture2D(width, height, TextureFormat.RGB24, false);
        image.ReadPixels(new Rect(0, 0, width, height), 0, 0);
        image.Apply();
        string outputPath = Path.GetFullPath("Temp/level3-population-overview.png");
        File.WriteAllBytes(outputPath, image.EncodeToPNG());

        RenderTexture.active = previous;
        camera.targetTexture = null;
        Object.DestroyImmediate(image);
        Object.DestroyImmediate(renderTexture);
        Object.DestroyImmediate(cameraObject);
        Debug.Log("LEVEL 3 OVERVIEW RENDERED: " + outputPath);
    }

    private static void AppendNamedPosition(Transform current, string targetName, StringBuilder report)
    {
        if (current.name == targetName)
        {
            report.AppendLine($"{targetName.ToUpperInvariant()} {GetPath(current)} @ {current.position}");
        }

        foreach (Transform child in current)
        {
            AppendNamedPosition(child, targetName, report);
        }
    }

    private static Placement CoinGroup(string prefabName, string name, float x, float y)
    {
        return new Placement("Assets/Prelabs/CoinGroups/" + prefabName + ".prefab", name, x, y);
    }

    private static Transform FindSceneRoot(Scene scene, string name)
    {
        foreach (GameObject root in scene.GetRootGameObjects())
        {
            if (root.name == name)
            {
                return root.transform;
            }
        }

        return null;
    }

    private static void ClearChildren(Transform root)
    {
        for (int index = root.childCount - 1; index >= 0; index--)
        {
            Object.DestroyImmediate(root.GetChild(index).gameObject);
        }
    }

    private static void RemoveLegacyRootBat(Scene scene)
    {
        Transform legacyBat = FindSceneRoot(scene, "Bat");
        if (legacyBat != null)
        {
            Object.DestroyImmediate(legacyBat.gameObject);
        }
    }

    private static void DisableBatSpawner(Scene scene)
    {
        Transform spawner = FindSceneRoot(scene, "BatSpawner");
        if (spawner != null)
        {
            spawner.gameObject.SetActive(false);
        }
    }

    private static GameObject PlacePrefab(Scene scene, Transform parent, Placement placement)
    {
        GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(placement.PrefabPath);
        Require(prefab != null, "Missing prefab: " + placement.PrefabPath);
        GameObject instance = (GameObject)PrefabUtility.InstantiatePrefab(prefab, scene);
        Require(instance != null, "Could not instantiate: " + placement.PrefabPath);
        instance.name = placement.Name;
        instance.transform.SetParent(parent, true);
        instance.transform.position = placement.Position;
        instance.transform.rotation = Quaternion.identity;
        return instance;
    }

    private static void ConfigureEnemy(GameObject enemy)
    {
        EnemyController slime = enemy.GetComponent<EnemyController>();
        if (slime != null)
        {
            slime.moveSpeed = 1.8f;
            slime.patrolDistance = 2.0f;
            slime.detectRange = 5.0f;
        }

        SkeletonEnemy melee = enemy.GetComponent<SkeletonEnemy>();
        if (melee != null)
        {
            SerializedObject serialized = new SerializedObject(melee);
            serialized.FindProperty("moveSpeed").floatValue = 2.2f;
            serialized.FindProperty("detectionRange").floatValue = 6.0f;
            serialized.FindProperty("detectionExitBuffer").floatValue = 0.8f;
            serialized.FindProperty("debugLogging").boolValue = false;
            serialized.ApplyModifiedPropertiesWithoutUndo();
        }

        SkeletonArcher archer = enemy.GetComponent<SkeletonArcher>();
        if (archer != null)
        {
            SerializedObject serialized = new SerializedObject(archer);
            serialized.FindProperty("detectRange").floatValue = 7.0f;
            serialized.FindProperty("attackCooldown").floatValue = 2.2f;
            serialized.ApplyModifiedPropertiesWithoutUndo();
        }
    }

    private static void Require(bool condition, string message)
    {
        if (!condition)
        {
            throw new System.InvalidOperationException(message);
        }
    }

    private static void AppendTopSurfaceSegments(Tilemap tilemap, StringBuilder report)
    {
        BoundsInt bounds = tilemap.cellBounds;
        var columns = new List<(int x, int y)>();
        for (int x = bounds.xMin; x < bounds.xMax; x++)
        {
            int topY = int.MinValue;
            for (int y = bounds.yMax - 1; y >= bounds.yMin; y--)
            {
                if (tilemap.HasTile(new Vector3Int(x, y, 0)))
                {
                    topY = y;
                    break;
                }
            }

            if (topY != int.MinValue)
            {
                columns.Add((x, topY));
            }
        }

        if (columns.Count == 0)
        {
            report.AppendLine("  no occupied columns");
            return;
        }

        int segmentStart = columns[0].x;
        int previousX = columns[0].x;
        int segmentY = columns[0].y;
        for (int index = 1; index <= columns.Count; index++)
        {
            bool continues = index < columns.Count &&
                             columns[index].x == previousX + 1 &&
                             columns[index].y == segmentY;
            if (continues)
            {
                previousX = columns[index].x;
                continue;
            }

            Vector3 left = tilemap.CellToWorld(new Vector3Int(segmentStart, segmentY + 1, 0));
            Vector3 right = tilemap.CellToWorld(new Vector3Int(previousX + 1, segmentY + 1, 0));
            report.AppendLine($"  cells x={segmentStart}..{previousX} y={segmentY} -> surface x={left.x:F2}..{right.x:F2} y={left.y:F2}");

            if (index < columns.Count)
            {
                segmentStart = columns[index].x;
                previousX = columns[index].x;
                segmentY = columns[index].y;
            }
        }
    }

    private static void AppendHierarchy(Transform current, StringBuilder report, int depth)
    {
        report.Append(' ', depth * 2);
        report.AppendLine($"{current.name} @ {current.position}");
        foreach (Transform child in current)
        {
            AppendHierarchy(child, report, depth + 1);
        }
    }

    private static string GetPath(Transform transform)
    {
        string path = transform.name;
        while (transform.parent != null)
        {
            transform = transform.parent;
            path = transform.name + "/" + path;
        }

        return path;
    }
}
