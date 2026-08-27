using System.Text;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.Tilemaps;

public static class Level2PopulationSetup
{
    private const string ScenePath = "Assets/Scenes/Level_2.unity";
    private const string CoinFolder = "Assets/Prelabs/CoinGroups/";
    private const string SlimePrefabPath = "Assets/Prelabs/Slime.prefab";
    private const string ArcherPrefabPath = "Assets/Prelabs/SkeletonArcher.prefab";
    private const string WarriorPrefabPath = "Assets/Prelabs/Skeleton_Warrior.prefab";
    private const string SpearmanPrefabPath = "Assets/Prelabs/Skeleton_Spearman.prefab";
    private const string CoinGroupName = "Level2Population_Coins";
    private const string EnemyGroupName = "Level2Population_Enemies";

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
        CoinGroup("Coin_3_Horizontal", "Coins_01_Entrance", -75.5f, -1.25f),
        CoinGroup("Coin_3_DiagonalUp", "Coins_02_FirstSeesaw", -63.0f, -3.15f),
        CoinGroup("Coin_3_DiagonalUp", "Coins_03_SecondSeesaw", -56.8f, -1.85f),
        CoinGroup("Coin_3_Horizontal", "Coins_04_FirstLanding", -50.5f, -0.25f),
        CoinGroup("Coin_3_DiagonalDown", "Coins_05_HighSeesaw", -43.9f, -0.15f),
        CoinGroup("Coin_3_DiagonalDown", "Coins_06_LowSeesaw", -35.7f, -2.25f),
        CoinGroup("Coin_3_DiagonalUp", "Coins_07_HillClimb", -29.0f, -0.55f),
        CoinGroup("Coin_3_Horizontal", "Coins_08_HillTop", -22.5f, 1.45f),

        CoinGroup("Coin_3_Horizontal", "Coins_09_AutoBridgeA", 0.55f, -1.45f),
        CoinGroup("Coin_3_Horizontal", "Coins_10_AutoBridgeB", 9.3f, -1.45f),
        CoinGroup("Coin_3_Horizontal", "Coins_11_FirstRest", 17.0f, -2.25f),
        CoinGroup("Coin_3_DiagonalDown", "Coins_12_DescendingIsland", 24.0f, -5.15f),
        CoinGroup("Coin_3_Horizontal", "Coins_13_LowerIsland", 33.0f, -5.25f),
        CoinGroup("Coin_3_Horizontal", "Coins_14_RotatorApproach", 38.0f, -3.15f),
        CoinGroup("Coin_3_Horizontal", "Coins_15_RotatorA", 44.2f, -3.35f),
        CoinGroup("Coin_3_Horizontal", "Coins_16_RotatorB", 52.2f, -3.25f),
        CoinGroup("Coin_3_Horizontal", "Coins_17_RotatorC", 60.5f, -3.35f),
        CoinGroup("Coin_3_Horizontal", "Coins_18_MidLanding", 67.5f, -2.25f),

        CoinGroup("Coin_3_Horizontal", "Coins_19_SteppingA", 82.0f, -2.15f),
        CoinGroup("Coin_3_Vertical", "Coins_20_SteppingB", 91.5f, -2.35f),
        CoinGroup("Coin_3_Horizontal", "Coins_21_SteppingC", 95.5f, -1.05f),
        CoinGroup("Coin_3_Vertical", "Coins_22_SteppingD", 104.5f, -3.85f),
        CoinGroup("Coin_3_Horizontal", "Coins_23_SteppingE", 116.5f, -1.05f),
        CoinGroup("Coin_3_DiagonalUp", "Coins_24_LastStep", 126.5f, -3.05f),

        CoinGroup("Coin_3_DiagonalUp", "Coins_25_UpperRotatorA", 133.7f, -1.65f),
        CoinGroup("Coin_3_Horizontal", "Coins_26_UpperRotatorB", 141.1f, 0.15f),
        CoinGroup("Coin_3_DiagonalUp", "Coins_27_UpperRotatorC", 148.8f, 1.15f),
        CoinGroup("Coin_3_Horizontal", "Coins_28_LongSlope", 161.5f, -3.85f),

        CoinGroup("Coin_3_Horizontal", "Coins_29_FinalAutoA", 181.6f, -2.05f),
        CoinGroup("Coin_3_Horizontal", "Coins_30_FinalAutoB", 187.9f, -2.05f),
        CoinGroup("Coin_2_Horizontal", "Coins_31_FinalLandingA", 193.0f, -1.15f),
        CoinGroup("Coin_2_Horizontal", "Coins_32_FinalLandingB", 197.0f, 0.45f),
        CoinGroup("Coin_3_DiagonalUp", "Coins_33_LastSeesaw", 200.5f, 1.95f),
        CoinGroup("Coin_3_Horizontal", "Coins_34_PortalApproach", 204.0f, 3.65f)
    };

    private static readonly Placement[] EnemyPlacements =
    {
        new Placement(ArcherPrefabPath, "SkeletonArcher_FirstLanding", -50.5f, -1.28f),
        new Placement(SlimePrefabPath, "Slime_HillTop", -22.5f, 0.35f),
        new Placement(WarriorPrefabPath, "Skeleton_Warrior_FirstRest", 17.0f, -3.18f),
        new Placement(ArcherPrefabPath, "SkeletonArcher_RotatorApproach", 38.0f, -4.28f),
        new Placement(SpearmanPrefabPath, "Skeleton_Spearman_MidLanding", 67.5f, -3.18f),
        new Placement(ArcherPrefabPath, "SkeletonArcher_SteppingRoute", 116.5f, -2.28f),
        new Placement(WarriorPrefabPath, "Skeleton_Warrior_AfterRotators", 161.5f, -5.18f),
        new Placement(SlimePrefabPath, "Slime_FinalSlope", 167.2f, -3.65f),
        new Placement(ArcherPrefabPath, "SkeletonArcher_BeforePortal", 197.0f, -0.28f)
    };

    [MenuItem("Tools/Levels/Arrange Level 2 Coins And Enemies")]
    public static void ArrangePopulation()
    {
        Scene scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
        GameObject itemRootObject = FindRoot(scene, "Item");
        GameObject enemyRootObject = FindRoot(scene, "Enemy");
        Require(itemRootObject != null, "Level_2 is missing its Item root.");
        Require(enemyRootObject != null, "Level_2 is missing its Enemy root.");

        Transform coinGroup = ReplaceManagedGroup(itemRootObject.transform, CoinGroupName);
        Transform enemyGroup = ReplaceManagedGroup(enemyRootObject.transform, EnemyGroupName);

        foreach (Placement placement in CoinPlacements)
        {
            PlacePrefab(scene, coinGroup, placement);
        }

        foreach (Placement placement in EnemyPlacements)
        {
            GameObject enemy = PlacePrefab(scene, enemyGroup, placement);
            ConfigureEnemy(enemy);
        }

        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        ValidatePopulationInScene(scene);
        Debug.Log("LEVEL 2 POPULATION COMPLETE: 100 coins and "
            + EnemyPlacements.Length + " enemies arranged from entrance to Portal.");
    }

    [MenuItem("Tools/Levels/Validate Level 2 Coins And Enemies")]
    public static void ValidatePopulation()
    {
        Scene scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
        ValidatePopulationInScene(scene);
    }

    [MenuItem("Tools/Levels/Render Level 2 Population Overview")]
    public static void RenderOverview()
    {
        EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
        GameObject cameraObject = new GameObject("Level2PopulationOverviewCamera");
        Camera camera = cameraObject.AddComponent<Camera>();
        camera.orthographic = true;
        camera.orthographicSize = 20f;
        camera.transform.position = new Vector3(65f, -2f, -30f);
        camera.clearFlags = CameraClearFlags.SolidColor;
        camera.backgroundColor = new Color(0.45f, 0.3f, 0.2f, 1f);

        const int width = 4096;
        const int height = 512;
        RenderTexture texture = new RenderTexture(width, height, 24);
        camera.targetTexture = texture;
        camera.aspect = (float)width / height;
        camera.Render();

        RenderTexture previous = RenderTexture.active;
        RenderTexture.active = texture;
        Texture2D image = new Texture2D(width, height, TextureFormat.RGB24, false);
        image.ReadPixels(new Rect(0, 0, width, height), 0, 0);
        image.Apply();
        string outputFolder = Path.GetFullPath(".codex-artifacts");
        Directory.CreateDirectory(outputFolder);
        string outputPath = Path.Combine(outputFolder, "level2-population-overview.png");
        File.WriteAllBytes(outputPath, image.EncodeToPNG());

        RenderTexture.active = previous;
        camera.targetTexture = null;
        Object.DestroyImmediate(image);
        Object.DestroyImmediate(texture);
        Object.DestroyImmediate(cameraObject);
        Debug.Log("LEVEL 2 POPULATION OVERVIEW RENDERED: " + outputPath);
    }

    private static Placement CoinGroup(string prefabName, string name, float x, float y)
    {
        return new Placement(CoinFolder + prefabName + ".prefab", name, x, y);
    }

    private static Transform ReplaceManagedGroup(Transform parent, string groupName)
    {
        Transform existing = parent.Find(groupName);
        if (existing != null)
        {
            Object.DestroyImmediate(existing.gameObject);
        }

        GameObject group = new GameObject(groupName);
        group.transform.SetParent(parent, false);
        return group.transform;
    }

    private static GameObject PlacePrefab(Scene scene, Transform parent, Placement placement)
    {
        GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(placement.PrefabPath);
        Require(prefab != null, "Missing prefab: " + placement.PrefabPath);

        GameObject instance = (GameObject)PrefabUtility.InstantiatePrefab(prefab, scene);
        Require(instance != null, "Could not instantiate prefab: " + placement.PrefabPath);
        instance.name = placement.Name;
        instance.transform.SetParent(parent, true);
        instance.transform.SetPositionAndRotation(placement.Position, Quaternion.identity);
        return instance;
    }

    private static void ConfigureEnemy(GameObject enemy)
    {
        EnemyController slime = enemy.GetComponent<EnemyController>();
        if (slime != null)
        {
            slime.moveSpeed = 1.7f;
            slime.patrolDistance = enemy.name.Contains("FinalSlope") ? 0.75f : 2.2f;
            slime.detectRange = 5f;
            EditorUtility.SetDirty(slime);
        }

        SkeletonEnemy melee = enemy.GetComponent<SkeletonEnemy>();
        if (melee != null)
        {
            SerializedObject data = new SerializedObject(melee);
            data.FindProperty("moveSpeed").floatValue = 2.1f;
            data.FindProperty("detectionRange").floatValue = 6f;
            data.FindProperty("detectionExitBuffer").floatValue = 0.8f;
            data.FindProperty("debugLogging").boolValue = false;
            data.ApplyModifiedPropertiesWithoutUndo();
        }

        SkeletonArcher archer = enemy.GetComponent<SkeletonArcher>();
        if (archer != null)
        {
            SerializedObject data = new SerializedObject(archer);
            data.FindProperty("detectRange").floatValue = 7f;
            data.FindProperty("attackCooldown").floatValue = 2.3f;
            data.ApplyModifiedPropertiesWithoutUndo();
        }

        Rigidbody2D body = enemy.GetComponent<Rigidbody2D>();
        if (body != null)
        {
            body.linearVelocity = Vector2.zero;
            body.angularVelocity = 0f;
            EditorUtility.SetDirty(body);
        }

        EditorUtility.SetDirty(enemy);
    }

    private static void ValidatePopulationInScene(Scene scene)
    {
        GameObject itemRootObject = FindRoot(scene, "Item");
        GameObject enemyRootObject = FindRoot(scene, "Enemy");
        Require(itemRootObject != null && enemyRootObject != null,
            "Level_2 lost its Item or Enemy root.");

        Transform coinGroup = itemRootObject.transform.Find(CoinGroupName);
        Transform enemyGroup = enemyRootObject.transform.Find(EnemyGroupName);
        Require(coinGroup != null, "The managed Level_2 coin group is missing.");
        Require(enemyGroup != null, "The managed Level_2 enemy group is missing.");
        Require(coinGroup.childCount == CoinPlacements.Length,
            "Expected " + CoinPlacements.Length + " coin routes, found " + coinGroup.childCount + ".");
        Require(enemyGroup.childCount == EnemyPlacements.Length,
            "Expected " + EnemyPlacements.Length + " enemies, found " + enemyGroup.childCount + ".");

        int coinCount = 0;
        foreach (Transform child in coinGroup.GetComponentsInChildren<Transform>(true))
        {
            if (!child.CompareTag("Coin"))
            {
                continue;
            }

            coinCount++;
            Collider2D trigger = child.GetComponent<Collider2D>();
            Require(trigger != null && trigger.isTrigger,
                GetPath(child) + " must keep its pickup trigger.");
        }

        Require(coinCount == 100, "Expected 100 individual coins, found " + coinCount + ".");
        Require(enemyGroup.GetComponentsInChildren<EnemyController>(true).Length == 2,
            "Expected two Slimes in Level_2.");
        Require(enemyGroup.GetComponentsInChildren<SkeletonArcher>(true).Length == 4,
            "Expected four Skeleton Archers in Level_2.");
        Require(enemyGroup.GetComponentsInChildren<SkeletonEnemy>(true).Length == 3,
            "Expected three melee Skeletons in Level_2.");

        foreach (Transform enemy in enemyGroup)
        {
            Require(enemy.CompareTag("enemy"), enemy.name + " must keep tag enemy.");
            Require(enemy.position.x > -60f && enemy.position.x < 200f,
                enemy.name + " is too close to an entrance or Portal boundary.");
        }

        Require(Object.FindObjectsByType<Portal>(FindObjectsInactive.Include, FindObjectsSortMode.None).Length >= 2,
            "Level_2 must keep both EntrancePortal and exit Portal.");
        Require(Object.FindObjectsByType<SeesawBridgeController>(
                    FindObjectsInactive.Include,
                    FindObjectsSortMode.None).Length == 15,
            "Level_2 seesaw layout changed while arranging population.");

        Debug.Log("LEVEL 2 POPULATION VALIDATION PASSED: " + coinCount
            + " coins, " + EnemyPlacements.Length + " enemies, Portals and 15 seesaws preserved.");
    }

    private static void Require(bool condition, string message)
    {
        if (!condition)
        {
            throw new System.InvalidOperationException(message);
        }
    }

    [MenuItem("Tools/Levels/Report Level 2 Population Layout")]
    public static void ReportLayout()
    {
        Scene scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
        Physics2D.SyncTransforms();
        var report = new StringBuilder("LEVEL 2 POPULATION LAYOUT\n");

        PlayerController player = Object.FindFirstObjectByType<PlayerController>(FindObjectsInactive.Include);
        report.AppendLine("Player: " + Describe(player != null ? player.transform : null));

        report.AppendLine("\nPORTALS AND CHECKPOINTS");
        foreach (Portal portal in Object.FindObjectsByType<Portal>(
                     FindObjectsInactive.Include,
                     FindObjectsSortMode.None))
        {
            report.AppendLine("Portal: " + Describe(portal.transform));
        }

        foreach (Checkpoint checkpoint in Object.FindObjectsByType<Checkpoint>(
                     FindObjectsInactive.Include,
                     FindObjectsSortMode.None))
        {
            report.AppendLine("Checkpoint: " + Describe(checkpoint.transform));
        }

        report.AppendLine("\nBRIDGES");
        foreach (SeesawBridgeController bridge in Object.FindObjectsByType<SeesawBridgeController>(
                     FindObjectsInactive.Include,
                     FindObjectsSortMode.None))
        {
            report.AppendLine(Describe(bridge.transform)
                + " length=" + bridge.BridgeLength.ToString("F2")
                + " continuous=" + bridge.IsContinuousRotation
                + " oscillating=" + bridge.IsAutoOscillating);
        }

        report.AppendLine("\nTERRAIN");
        foreach (TilemapCollider2D tilemap in Object.FindObjectsByType<TilemapCollider2D>(
                     FindObjectsInactive.Include,
                     FindObjectsSortMode.None))
        {
            tilemap.ProcessTilemapChanges();
            Tilemap map = tilemap.GetComponent<Tilemap>();
            report.AppendLine(Describe(tilemap.transform)
                + " bounds=" + tilemap.bounds
                + " cells=" + (map != null ? map.cellBounds.ToString() : "none"));
        }

        Tilemap groundMap = FindRoot(scene, "Grid")?.transform.Find("Ground")?.GetComponent<Tilemap>();
        if (groundMap != null)
        {
            report.AppendLine("\nGROUND TOP PROFILE");
            foreach (string range in BuildGroundProfile(groundMap))
            {
                report.AppendLine(range);
            }
        }

        report.AppendLine("\nSOLID COLLIDERS");
        foreach (Collider2D collider in Object.FindObjectsByType<Collider2D>(
                     FindObjectsInactive.Include,
                     FindObjectsSortMode.None))
        {
            if (!collider.enabled || collider.isTrigger || collider.bounds.size.sqrMagnitude < 0.01f)
            {
                continue;
            }

            report.AppendLine(Describe(collider.transform)
                + " collider=" + collider.GetType().Name
                + " bounds=" + collider.bounds);
        }

        report.AppendLine("\nEXISTING ENEMIES");
        foreach (EnemyController enemy in Object.FindObjectsByType<EnemyController>(
                     FindObjectsInactive.Include,
                     FindObjectsSortMode.None))
        {
            report.AppendLine(Describe(enemy.transform) + " type=" + enemy.GetType().Name);
        }

        foreach (SkeletonEnemy enemy in Object.FindObjectsByType<SkeletonEnemy>(
                     FindObjectsInactive.Include,
                     FindObjectsSortMode.None))
        {
            report.AppendLine(Describe(enemy.transform) + " type=" + enemy.GetType().Name);
        }

        foreach (BatController enemy in Object.FindObjectsByType<BatController>(
                     FindObjectsInactive.Include,
                     FindObjectsSortMode.None))
        {
            report.AppendLine(Describe(enemy.transform) + " type=" + enemy.GetType().Name);
        }

        report.AppendLine("\nITEM ROOT CHILDREN");
        GameObject itemRoot = FindRoot(scene, "Item");
        if (itemRoot != null)
        {
            foreach (Transform child in itemRoot.transform)
            {
                report.AppendLine(Describe(child));
            }
        }

        Debug.Log(report.ToString());
    }

    private static IEnumerable<string> BuildGroundProfile(Tilemap map)
    {
        BoundsInt bounds = map.cellBounds;
        bool hasRange = false;
        int rangeStart = 0;
        int previousX = 0;
        float rangeTop = 0f;

        for (int x = bounds.xMin; x < bounds.xMax; x++)
        {
            int topCellY = int.MinValue;
            for (int y = bounds.yMin; y < bounds.yMax; y++)
            {
                if (map.HasTile(new Vector3Int(x, y, 0)))
                {
                    topCellY = Mathf.Max(topCellY, y);
                }
            }

            bool hasTop = topCellY != int.MinValue;
            float topWorldY = hasTop
                ? map.CellToWorld(new Vector3Int(x, topCellY + 1, 0)).y
                : float.NaN;

            if (!hasRange && hasTop)
            {
                hasRange = true;
                rangeStart = x;
                previousX = x;
                rangeTop = topWorldY;
                continue;
            }

            if (hasRange && hasTop && x == previousX + 1 && Mathf.Abs(topWorldY - rangeTop) < 0.01f)
            {
                previousX = x;
                continue;
            }

            if (hasRange)
            {
                yield return FormatGroundRange(map, rangeStart, previousX, rangeTop);
                hasRange = false;
            }

            if (hasTop)
            {
                hasRange = true;
                rangeStart = x;
                previousX = x;
                rangeTop = topWorldY;
            }
        }

        if (hasRange)
        {
            yield return FormatGroundRange(map, rangeStart, previousX, rangeTop);
        }
    }

    private static string FormatGroundRange(Tilemap map, int startCellX, int endCellX, float topWorldY)
    {
        float startWorldX = map.CellToWorld(new Vector3Int(startCellX, 0, 0)).x;
        float endWorldX = map.CellToWorld(new Vector3Int(endCellX + 1, 0, 0)).x;
        return "x=" + startWorldX.ToString("F2") + ".." + endWorldX.ToString("F2")
            + " topY=" + topWorldY.ToString("F2");
    }

    private static string Describe(Transform target)
    {
        if (target == null)
        {
            return "MISSING";
        }

        return GetPath(target) + " world=" + target.position + " local=" + target.localPosition;
    }

    private static GameObject FindRoot(Scene scene, string name)
    {
        foreach (GameObject root in scene.GetRootGameObjects())
        {
            if (root.name == name)
            {
                return root;
            }
        }

        return null;
    }

    private static string GetPath(Transform target)
    {
        string path = target.name;
        while (target.parent != null)
        {
            target = target.parent;
            path = target.name + "/" + path;
        }

        return path;
    }
}
