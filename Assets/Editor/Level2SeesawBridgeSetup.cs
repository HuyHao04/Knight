using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.Tilemaps;

public static class Level2SeesawBridgeSetup
{
    private const string MenuPath = "Tools/Levels/Build Level 2 Seesaw Bridge";
    private const string ScenePath = "Assets/Scenes/Level_2.unity";
    private const string SeesawFolder = "Assets/Prelabs/Seesaw";
    private const string SeesawPrefabPath = SeesawFolder + "/SeesawBridge.prefab";
    private const string GripMaterialPath = SeesawFolder + "/SeesawGrip.physicsMaterial2D";
    private const string TileFolder = "Assets/Tiles2/SeesawAutumn";
    private const string GroundTopTilePath = TileFolder + "/AutumnGroundTop.asset";
    private const string GroundFillTilePath = TileFolder + "/AutumnGroundFill.asset";

    private const string AutumnRoot =
        "Assets/Sprites/craftpix-net-763418-free-autumn-forest-2d-platformer-tileset/PNG/";
    private const string PillarPath = AutumnRoot
        + "Platfromer/Cartoon_Medieval_Field_Work_Level_Set_Building - Pillar 01.png";
    private const string PivotPath = AutumnRoot
        + "Platfromer/Autumn_Forest_2D_Platformer_Tileset_Platformer - Bridge Part 01.png";
    private const string GroundTopPath = AutumnRoot
        + "Platfromer/Autumn_Forest_2D_Platformer_Tileset_Platformer - Ground 02.png";
    private const string GroundFillPath = AutumnRoot
        + "Platfromer/Autumn_Forest_2D_Platformer_Tileset_Platformer - Ground 06.png";
    private const string BackgroundFarPath = AutumnRoot
        + "Background/Autumn_Forest_2D_Platformer_Tileset_Background - Layer 00.png";
    private const string BackgroundNearPath = AutumnRoot
        + "Background/Autumn_Forest_2D_Platformer_Tileset_Background - Layer 01.png";
    private const string TreePath = AutumnRoot
        + "Environment/Autumn_Forest_2D_Platformer_Tileset_Environment - Tree 01.png";
    private const string BushPath = AutumnRoot
        + "Environment/Autumn_Forest_2D_Platformer_Tileset_Environment - Bush 01.png";
    private const string SignPath = AutumnRoot
        + "Environment/Autumn_Forest_2D_Platformer_Tileset_Environment - Signpost 01.png";

    private const string Coin3HorizontalPath = "Assets/Prelabs/CoinGroups/Coin_3_Horizontal.prefab";
    private const string Coin2DiagonalUpPath = "Assets/Prelabs/CoinGroups/Coin_2_DiagonalUp.prefab";
    private const string Coin2DiagonalDownPath = "Assets/Prelabs/CoinGroups/Coin_2_DiagonalDown.prefab";
    private const string SlimePrefabPath = "Assets/Prelabs/Slime.prefab";
    private const string HeartPrefabPath = "Assets/Prelabs/Heart.prefab";
    private const string PortalPrefabPath = "Assets/Prelabs/Portal.prefab";

    private const float CellSize = 1.28f;
    private const int TopCellY = -4;
    private const int FillBottomCellY = -7;
    private const float GroundTopY = -3.84f;

    private readonly struct BridgeDefinition
    {
        public readonly string Name;
        public readonly Vector2 Position;
        public readonly float Length;
        public readonly bool Continuous;
        public readonly float MotorSpeed;

        public BridgeDefinition(
            string name,
            float x,
            float y,
            float length,
            bool continuous = false,
            float motorSpeed = 0f)
        {
            Name = name;
            Position = new Vector2(x, y);
            Length = length;
            Continuous = continuous;
            MotorSpeed = motorSpeed;
        }
    }

    private static readonly BridgeDefinition[] Bridges =
    {
        new BridgeDefinition("01_Tutorial_Low", 6.8f, -3.25f, 5.2f),
        new BridgeDefinition("02_DeathPit_A", 18.2f, -2.95f, 4.8f),
        new BridgeDefinition("03_DeathPit_B", 24.0f, -2.75f, 4.8f),
        new BridgeDefinition("04_CoinGuide", 37.5f, -2.85f, 5.6f),
        new BridgeDefinition("05_Triple_A", 54.0f, -2.9f, 4.8f),
        new BridgeDefinition("06_Triple_B", 60.6f, -2.65f, 4.6f),
        new BridgeDefinition("07_Triple_C", 68.0f, -2.4f, 4.3f),
        new BridgeDefinition("08_Final_Rotator_A", 82.4f, -2.85f, 5.0f, true, 42f),
        new BridgeDefinition("09_Final_Rotator_B", 89.2f, -2.55f, 5.0f, true, -48f)
    };

    [MenuItem(MenuPath)]
    public static void BuildLevel2()
    {
        EnsureFolders();

        Sprite pillarSprite = RequireSprite(PillarPath);
        Sprite pivotSprite = RequireSprite(PivotPath);
        PhysicsMaterial2D gripMaterial = CreateOrUpdateGripMaterial();
        GameObject bridgePrefab = CreateOrUpdateSeesawPrefab(
            pillarSprite,
            pivotSprite,
            gripMaterial);
        Tile groundTopTile = CreateOrUpdateTile(
            GroundTopTilePath,
            RequireSprite(GroundTopPath));
        Tile groundFillTile = CreateOrUpdateTile(
            GroundFillTilePath,
            RequireSprite(GroundFillPath));

        Scene scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
        Transform backgroundRoot = PrepareMapRoot(scene, "Background");
        Transform enemyRoot = PrepareMapRoot(scene, "Enemy");
        Transform itemRoot = PrepareMapRoot(scene, "Item");
        Transform groundRoot = PrepareMapRoot(scene, "ground");
        Transform gridRoot = PrepareMapRoot(scene, "Grid");
        DestroySceneRootIfPresent(scene, "Bat");

        Grid grid = gridRoot.GetComponent<Grid>();
        Require(grid != null, "Level_2 root Grid is missing its Grid component.");
        grid.cellSize = new Vector3(CellSize, CellSize, 1f);

        Transform movingPlatformsRoot = NewChild("MovingPlatforms", gridRoot);
        Transform fallingPlatformRoot = NewChild("FallingPlatfrom", gridRoot);
        Transform movingGroundRoot = NewChild("MovingGround", gridRoot);
        Transform decorationRoot = NewChild("DecorationLayer", gridRoot);
        movingPlatformsRoot.gameObject.SetActive(true);
        fallingPlatformRoot.gameObject.SetActive(true);

        PlayerController player = UnityEngine.Object.FindAnyObjectByType<PlayerController>(
            FindObjectsInactive.Include);
        CameraController cameraController = UnityEngine.Object.FindAnyObjectByType<CameraController>(
            FindObjectsInactive.Include);

        Require(player != null, "Level_2 is missing PlayerController.");
        Require(cameraController != null, "Level_2 is missing CameraController.");

        CreateBackground(backgroundRoot);
        CreateTerrain(gridRoot, groundTopTile, groundFillTile);
        CreateDeathZone(groundRoot);
        CreateBridges(movingGroundRoot, bridgePrefab);
        CreateItems(itemRoot);
        CreateEnemy(enemyRoot);
        CreateCheckpoint(groundRoot);
        CreatePortal(groundRoot);
        CreateDecorations(decorationRoot);

        ConfigurePlayer(player);
        ConfigureCamera(cameraController, player);
        EnableLevel2InBuildSettings();

        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        ValidateLevel2();
        Debug.Log(
            "LEVEL 2 SEESAW BRIDGE COMPLETE: 7 free seesaws, 2 continuous rotators, "
            + "checkpoint, coins, enemy, death zone and Level 3 portal.");
    }

    public static void ValidateLevel2()
    {
        Scene scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
        GameObject gridRoot = FindSceneRoot(scene, "Grid");
        GameObject groundRoot = FindSceneRoot(scene, "ground");
        GameObject itemRoot = FindSceneRoot(scene, "Item");
        GameObject enemyRoot = FindSceneRoot(scene, "Enemy");
        GameObject backgroundRoot = FindSceneRoot(scene, "Background");
        GameObject characterRoot = FindSceneRoot(scene, "Character");
        Require(gridRoot != null && groundRoot != null && itemRoot != null
            && enemyRoot != null && backgroundRoot != null && characterRoot != null,
            "Level_2 does not match the standard Level 1/3 root hierarchy.");

        SeesawBridgeController[] bridges = gridRoot.GetComponentsInChildren<SeesawBridgeController>(true);
        Require(bridges.Length == 9, "Level_2 must contain exactly 9 seesaw bridges.");
        Require(
            bridges.Count(bridge => bridge.IsContinuousRotation) == 2,
            "Level_2 must contain exactly 2 continuously rotating bridges.");
        Require(
            bridges.Count(bridge => !bridge.IsContinuousRotation) == 7,
            "Level_2 must contain exactly 7 free seesaw bridges.");

        Sprite pillarSprite = RequireSprite(PillarPath);
        foreach (SeesawBridgeController bridge in bridges)
        {
            Require(bridge.CompareTag("ground"), bridge.name + " must use tag ground.");
            Require(bridge.GetComponent<Rigidbody2D>() != null, bridge.name + " is missing Rigidbody2D.");
            Require(bridge.GetComponent<BoxCollider2D>() != null, bridge.name + " is missing BoxCollider2D.");
            HingeJoint2D hinge = bridge.GetComponent<HingeJoint2D>();
            Require(hinge != null && hinge.connectedBody != null, bridge.name + " has no hinge anchor.");
            Require(!hinge.useLimits, bridge.name + " must not have angle limits.");
            Require(
                bridge.GetComponentsInChildren<SpriteRenderer>(true)
                    .Any(renderer => renderer.sprite == pillarSprite),
                bridge.name + " is not using Pillar 01 as its bridge surface.");
        }

        Require(groundRoot.GetComponentInChildren<Checkpoint>(true) != null, "Level_2 is missing checkpoint.");
        Portal portal = groundRoot.GetComponentInChildren<Portal>(true);
        Require(portal != null, "Level_2 is missing Portal.");
        SerializedObject portalData = new SerializedObject(portal);
        Require(
            portalData.FindProperty("destinationScene").stringValue == "Level_3",
            "Level_2 Portal must lead to Level_3.");
        Require(
            groundRoot.GetComponentsInChildren<Collider2D>(true).Any(collider => collider.CompareTag("hole")),
            "Level_2 is missing its death-zone collider.");
        Require(itemRoot.transform.childCount >= 10, "Level_2 coin guidance is incomplete.");
        Require(enemyRoot.GetComponentInChildren<EnemyController>(true) != null,
            "Level_2 is missing its patrol enemy.");
        Require(gridRoot.transform.Find("Ground") != null
            && gridRoot.transform.Find("MovingPlatforms") != null
            && gridRoot.transform.Find("FallingPlatfrom") != null
            && gridRoot.transform.Find("MovingGround") != null
            && gridRoot.transform.Find("DecorationLayer") != null,
            "Level_2 Grid children do not match the Level 3 structure.");
        Require(
            UnityEngine.Object.FindObjectsByType<PlayerController>(FindObjectsInactive.Include, FindObjectsSortMode.None).Length == 1,
            "Level_2 must contain exactly one PlayerController.");
        Require(
            UnityEngine.Object.FindObjectsByType<CameraController>(FindObjectsInactive.Include, FindObjectsSortMode.None).Length == 1,
            "Level_2 must contain exactly one CameraController.");
        Require(IsLevel2Enabled(), "Level_2 must be enabled in Build Settings.");

        EditorSceneManager.SaveScene(scene);
        Debug.Log("LEVEL 2 SEESAW VALIDATION PASSED.");
    }

    private static void EnsureFolders()
    {
        if (!AssetDatabase.IsValidFolder(SeesawFolder))
        {
            AssetDatabase.CreateFolder("Assets/Prelabs", "Seesaw");
        }

        if (!AssetDatabase.IsValidFolder(TileFolder))
        {
            AssetDatabase.CreateFolder("Assets/Tiles2", "SeesawAutumn");
        }
    }

    private static PhysicsMaterial2D CreateOrUpdateGripMaterial()
    {
        PhysicsMaterial2D material = AssetDatabase.LoadAssetAtPath<PhysicsMaterial2D>(GripMaterialPath);
        if (material == null)
        {
            material = new PhysicsMaterial2D("SeesawGrip");
            AssetDatabase.CreateAsset(material, GripMaterialPath);
        }

        material.friction = 0.85f;
        material.bounciness = 0f;
        EditorUtility.SetDirty(material);
        return material;
    }

    private static GameObject CreateOrUpdateSeesawPrefab(
        Sprite pillarSprite,
        Sprite pivotSprite,
        PhysicsMaterial2D gripMaterial)
    {
        GameObject root = new GameObject("SeesawBridge");

        try
        {
            GameObject anchor = new GameObject("CenterAnchor", typeof(Rigidbody2D));
            anchor.transform.SetParent(root.transform, false);
            Rigidbody2D anchorBody = anchor.GetComponent<Rigidbody2D>();
            anchorBody.bodyType = RigidbodyType2D.Static;

            GameObject pivotVisualObject = new GameObject("PivotVisual", typeof(SpriteRenderer));
            pivotVisualObject.transform.SetParent(anchor.transform, false);
            SpriteRenderer pivotRenderer = pivotVisualObject.GetComponent<SpriteRenderer>();
            pivotRenderer.sprite = pivotSprite;
            pivotRenderer.sortingLayerName = "Ground";
            pivotRenderer.sortingOrder = 0;
            pivotVisualObject.transform.localScale = Vector3.one * 1.35f;
            CenterSpriteAtLocalOrigin(pivotRenderer);

            GameObject plank = new GameObject(
                "Plank",
                typeof(Rigidbody2D),
                typeof(BoxCollider2D),
                typeof(HingeJoint2D),
                typeof(SeesawBridgeController));
            plank.transform.SetParent(root.transform, false);
            plank.tag = "ground";
            int groundLayer = LayerMask.NameToLayer("Ground");
            plank.layer = groundLayer >= 0 ? groundLayer : 0;

            Rigidbody2D body = plank.GetComponent<Rigidbody2D>();
            body.bodyType = RigidbodyType2D.Dynamic;
            body.mass = 4f;
            body.gravityScale = 1f;
            body.linearDamping = 0.15f;
            body.angularDamping = 0.6f;
            body.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
            body.interpolation = RigidbodyInterpolation2D.Interpolate;
            body.sleepMode = RigidbodySleepMode2D.NeverSleep;

            BoxCollider2D surface = plank.GetComponent<BoxCollider2D>();
            surface.size = new Vector2(5f, 0.32f);
            surface.sharedMaterial = gripMaterial;

            HingeJoint2D hinge = plank.GetComponent<HingeJoint2D>();
            hinge.connectedBody = anchorBody;
            hinge.autoConfigureConnectedAnchor = false;
            hinge.anchor = Vector2.zero;
            hinge.connectedAnchor = Vector2.zero;
            hinge.enableCollision = false;

            GameObject visualObject = new GameObject("Pillar01_Surface", typeof(SpriteRenderer));
            visualObject.transform.SetParent(plank.transform, false);
            SpriteRenderer visualRenderer = visualObject.GetComponent<SpriteRenderer>();
            visualRenderer.sprite = pillarSprite;
            visualRenderer.sortingLayerName = "Ground";
            visualRenderer.sortingOrder = 1;

            SeesawBridgeController controller = plank.GetComponent<SeesawBridgeController>();
            SerializedObject controllerData = new SerializedObject(controller);
            controllerData.FindProperty("bridgeVisual").objectReferenceValue = visualRenderer;
            controllerData.ApplyModifiedPropertiesWithoutUndo();
            controller.Configure(5f, false, 0f, 28f);

            return PrefabUtility.SaveAsPrefabAsset(root, SeesawPrefabPath);
        }
        finally
        {
            UnityEngine.Object.DestroyImmediate(root);
        }
    }

    private static Tile CreateOrUpdateTile(string path, Sprite sprite)
    {
        Tile tile = AssetDatabase.LoadAssetAtPath<Tile>(path);
        if (tile == null)
        {
            tile = ScriptableObject.CreateInstance<Tile>();
            AssetDatabase.CreateAsset(tile, path);
        }

        tile.sprite = sprite;
        tile.colliderType = Tile.ColliderType.Grid;
        EditorUtility.SetDirty(tile);
        return tile;
    }

    private static Transform PrepareMapRoot(Scene scene, string rootName)
    {
        GameObject root = FindSceneRoot(scene, rootName);
        Require(root != null, "Level_2 is missing standard root '" + rootName + "'.");

        for (int index = root.transform.childCount - 1; index >= 0; index--)
        {
            UnityEngine.Object.DestroyImmediate(root.transform.GetChild(index).gameObject);
        }

        root.transform.SetPositionAndRotation(Vector3.zero, Quaternion.identity);
        root.transform.localScale = Vector3.one;

        SpriteRenderer rootRenderer = root.GetComponent<SpriteRenderer>();
        if (rootRenderer != null)
        {
            rootRenderer.enabled = false;
        }

        return root.transform;
    }

    private static GameObject FindSceneRoot(Scene scene, string rootName)
    {
        return scene.GetRootGameObjects().FirstOrDefault(root => root.name == rootName);
    }

    private static void DestroySceneRootIfPresent(Scene scene, string rootName)
    {
        GameObject root = FindSceneRoot(scene, rootName);
        if (root != null)
        {
            UnityEngine.Object.DestroyImmediate(root);
        }
    }

    private static void CreateBackground(Transform mapRoot)
    {
        Transform backgroundRoot = NewChild("AutumnBackground", mapRoot);
        Sprite far = RequireSprite(BackgroundFarPath);
        Sprite near = RequireSprite(BackgroundNearPath);

        for (int index = 0; index < 7; index++)
        {
            float centerX = -1f + index * 18f;
            CreateCenteredSprite(
                "Far_" + index,
                backgroundRoot,
                far,
                new Vector2(centerX, 0f),
                new Vector3(1.35f, 1.35f, 1f),
                "BackGround",
                -10);
            CreateCenteredSprite(
                "Near_" + index,
                backgroundRoot,
                near,
                new Vector2(centerX, 0f),
                new Vector3(1.35f, 1.35f, 1f),
                "BackGround",
                -9);
        }
    }

    private static void CreateTerrain(Transform gridRoot, Tile topTile, Tile fillTile)
    {
        GameObject tilemapObject = new GameObject(
            "Ground",
            typeof(Tilemap),
            typeof(TilemapRenderer),
            typeof(Rigidbody2D),
            typeof(CompositeCollider2D),
            typeof(TilemapCollider2D));
        tilemapObject.transform.SetParent(gridRoot, false);
        tilemapObject.tag = "ground";
        int groundLayer = LayerMask.NameToLayer("Ground");
        tilemapObject.layer = groundLayer >= 0 ? groundLayer : 0;

        Tilemap tilemap = tilemapObject.GetComponent<Tilemap>();
        tilemap.tileAnchor = Vector3.zero;
        TilemapRenderer renderer = tilemapObject.GetComponent<TilemapRenderer>();
        renderer.sortingLayerName = "Ground";
        renderer.sortingOrder = -2;

        Rigidbody2D body = tilemapObject.GetComponent<Rigidbody2D>();
        body.bodyType = RigidbodyType2D.Static;
        CompositeCollider2D composite = tilemapObject.GetComponent<CompositeCollider2D>();
        composite.geometryType = CompositeCollider2D.GeometryType.Polygons;
        TilemapCollider2D tilemapCollider = tilemapObject.GetComponent<TilemapCollider2D>();
        tilemapCollider.compositeOperation = Collider2D.CompositeOperation.Merge;

        FillIsland(tilemap, -8, 2, topTile, fillTile);
        FillIsland(tilemap, 8, 11, topTile, fillTile);
        FillIsland(tilemap, 21, 26, topTile, fillTile);
        FillIsland(tilemap, 32, 39, topTile, fillTile);
        FillIsland(tilemap, 55, 61, topTile, fillTile);
        FillIsland(tilemap, 72, 85, topTile, fillTile);
        tilemap.CompressBounds();
    }

    private static void FillIsland(Tilemap tilemap, int minX, int maxX, Tile topTile, Tile fillTile)
    {
        for (int x = minX; x <= maxX; x++)
        {
            tilemap.SetTile(new Vector3Int(x, TopCellY, 0), topTile);
            for (int y = FillBottomCellY; y < TopCellY; y++)
            {
                tilemap.SetTile(new Vector3Int(x, y, 0), fillTile);
            }
        }
    }

    private static void CreateDeathZone(Transform groundRoot)
    {
        GameObject deathZone = new GameObject("hole", typeof(BoxCollider2D));
        deathZone.transform.SetParent(groundRoot, false);
        deathZone.transform.localPosition = new Vector3(50f, -9.4f, 0f);
        deathZone.tag = "hole";
        BoxCollider2D collider = deathZone.GetComponent<BoxCollider2D>();
        collider.size = new Vector2(125f, 1.5f);
    }

    private static void CreateBridges(Transform movingGroundRoot, GameObject bridgePrefab)
    {
        Transform bridgeRoot = NewChild("SeesawBridges", movingGroundRoot);

        foreach (BridgeDefinition definition in Bridges)
        {
            GameObject instance = (GameObject)PrefabUtility.InstantiatePrefab(bridgePrefab);
            instance.name = definition.Name;
            instance.transform.SetParent(bridgeRoot, false);
            instance.transform.localPosition = definition.Position;

            SeesawBridgeController controller = instance.GetComponentInChildren<SeesawBridgeController>();
            Require(controller != null, definition.Name + " is missing SeesawBridgeController.");
            controller.name = definition.Name + "_Plank";
            controller.Configure(
                definition.Length,
                definition.Continuous,
                definition.MotorSpeed,
                28f);
            EditorUtility.SetDirty(controller);
        }
    }

    private static void CreateItems(Transform itemRoot)
    {
        GameObject coin3 = RequirePrefab(Coin3HorizontalPath);
        GameObject diagonalUp = RequirePrefab(Coin2DiagonalUpPath);
        GameObject diagonalDown = RequirePrefab(Coin2DiagonalDownPath);

        PlacePrefab(coin3, itemRoot, "Tutorial_Coins", new Vector3(6.8f, -1.65f, 0f));
        PlacePrefab(diagonalUp, itemRoot, "Pit_Coins_A", new Vector3(18.2f, -1.3f, 0f));
        PlacePrefab(diagonalDown, itemRoot, "Pit_Coins_B", new Vector3(24.0f, -1.05f, 0f));
        PlacePrefab(coin3, itemRoot, "Guide_Coins_A", new Vector3(36.5f, -1.25f, 0f));
        PlacePrefab(coin3, itemRoot, "Guide_Coins_B", new Vector3(39.2f, -0.8f, 0f));
        PlacePrefab(diagonalUp, itemRoot, "Triple_Coins_A", new Vector3(54.0f, -1.15f, 0f));
        PlacePrefab(coin3, itemRoot, "Triple_Coins_B", new Vector3(60.6f, -0.65f, 0f));
        PlacePrefab(diagonalDown, itemRoot, "Triple_Coins_C", new Vector3(68.0f, -0.25f, 0f));
        PlacePrefab(coin3, itemRoot, "Final_Coins_A", new Vector3(82.4f, -0.9f, 0f));
        PlacePrefab(coin3, itemRoot, "Final_Coins_B", new Vector3(89.2f, -0.5f, 0f));

        GameObject heart = PlacePrefab(
            RequirePrefab(HeartPrefabPath),
            itemRoot,
            "Heart_Before_Portal",
            new Vector3(98.5f, -2.7f, 0f));
        heart.transform.localScale = Vector3.one;
    }

    private static void CreateEnemy(Transform enemyRoot)
    {
        GameObject slime = PlacePrefab(
            RequirePrefab(SlimePrefabPath),
            enemyRoot,
            "Slime_CoinGuide_Patrol",
            new Vector3(45.5f, -3.15f, 0f));

        EnemyController controller = slime.GetComponent<EnemyController>();
        if (controller != null)
        {
            controller.moveSpeed = 1.6f;
            controller.patrolDistance = 2.8f;
            controller.detectRange = 5f;
            EditorUtility.SetDirty(controller);
        }
    }

    private static void CreateCheckpoint(Transform groundRoot)
    {
        GameObject checkpointObject = new GameObject(
            "Checkpoint_After_CoinGuide",
            typeof(SpriteRenderer),
            typeof(BoxCollider2D),
            typeof(Checkpoint));
        checkpointObject.transform.SetParent(groundRoot, false);
        checkpointObject.transform.localPosition = new Vector3(48.2f, GroundTopY, 0f);

        SpriteRenderer renderer = checkpointObject.GetComponent<SpriteRenderer>();
        renderer.sprite = RequireSprite(SignPath);
        renderer.sortingLayerName = "Decorlayer3";
        renderer.sortingOrder = 2;
        checkpointObject.transform.localScale = Vector3.one * 0.9f;

        BoxCollider2D trigger = checkpointObject.GetComponent<BoxCollider2D>();
        trigger.isTrigger = true;
        trigger.size = new Vector2(1.7f, 3.2f);
        trigger.offset = new Vector2(0.5f, 1.4f);

        GameObject respawnPoint = new GameObject("RespawnPoint");
        respawnPoint.transform.SetParent(checkpointObject.transform, false);
        respawnPoint.transform.localPosition = new Vector3(1.2f, 0.85f, 0f);

        SerializedObject checkpointData = new SerializedObject(checkpointObject.GetComponent<Checkpoint>());
        checkpointData.FindProperty("respawnPoint").objectReferenceValue = respawnPoint.transform;
        checkpointData.ApplyModifiedPropertiesWithoutUndo();
    }

    private static void CreatePortal(Transform groundRoot)
    {
        GameObject portalObject = PlacePrefab(
            RequirePrefab(PortalPrefabPath),
            groundRoot,
            "Portal",
            new Vector3(104f, -2.38f, 0f));

        Portal portal = portalObject.GetComponent<Portal>();
        Require(portal != null, "Portal prefab is missing Portal component.");
        SerializedObject portalData = new SerializedObject(portal);
        portalData.FindProperty("destinationScene").stringValue = "Level_3";
        portalData.ApplyModifiedPropertiesWithoutUndo();
    }

    private static void CreateDecorations(Transform decorationRoot)
    {
        Sprite tree = RequireSprite(TreePath);
        Sprite bush = RequireSprite(BushPath);
        Sprite sign = RequireSprite(SignPath);

        CreateBottomCenteredSprite("Tree_Start", decorationRoot, tree, -9.4f, GroundTopY, 0.62f, "Decorlayer1", 0);
        CreateBottomCenteredSprite("Bush_Tutorial", decorationRoot, bush, 11.4f, GroundTopY, 0.8f, "Decorlayer2", 0);
        CreateBottomCenteredSprite("Tree_DoubleBridge", decorationRoot, tree, 29.2f, GroundTopY, 0.55f, "Decorlayer1", 0);
        CreateBottomCenteredSprite("Sign_CoinGuide", decorationRoot, sign, 42.0f, GroundTopY, 0.75f, "Decorlayer3", 0);
        CreateBottomCenteredSprite("Bush_Checkpoint", decorationRoot, bush, 50.0f, GroundTopY, 0.75f, "Decorlayer2", 0);
        CreateBottomCenteredSprite("Tree_TripleLanding", decorationRoot, tree, 75.5f, GroundTopY, 0.6f, "Decorlayer1", 0);
        CreateBottomCenteredSprite("Bush_Final", decorationRoot, bush, 95.0f, GroundTopY, 0.8f, "Decorlayer2", 0);
        CreateBottomCenteredSprite("Tree_Portal", decorationRoot, tree, 108.0f, GroundTopY, 0.55f, "Decorlayer1", 0);
    }

    private static void ConfigurePlayer(PlayerController player)
    {
        Vector3 position = player.transform.position;
        position.x = -6f;
        position.y = -3.05f;
        player.transform.position = position;

        Rigidbody2D body = player.GetComponent<Rigidbody2D>();
        if (body != null)
        {
            body.position = position;
            body.linearVelocity = Vector2.zero;
        }

        EditorUtility.SetDirty(player);
    }

    private static void ConfigureCamera(CameraController cameraController, PlayerController player)
    {
        SerializedObject cameraData = new SerializedObject(cameraController);
        cameraData.FindProperty("player").objectReferenceValue = player.gameObject;
        cameraData.FindProperty("startCamera").floatValue = -1f;
        cameraData.FindProperty("endCamera").floatValue = 101f;
        cameraData.ApplyModifiedPropertiesWithoutUndo();

        Camera camera = cameraController.GetComponent<Camera>();
        if (camera != null)
        {
            camera.orthographic = true;
            camera.orthographicSize = 5f;
            camera.backgroundColor = new Color(0.52f, 0.31f, 0.22f, 1f);
            EditorUtility.SetDirty(camera);
        }

        Vector3 cameraPosition = cameraController.transform.position;
        cameraPosition.x = -1f;
        cameraPosition.y = 0f;
        cameraPosition.z = -10f;
        cameraController.transform.position = cameraPosition;
        EditorUtility.SetDirty(cameraController);
    }

    private static void EnableLevel2InBuildSettings()
    {
        EditorBuildSettingsScene[] scenes = EditorBuildSettings.scenes;
        GUID level2Guid = AssetDatabase.GUIDFromAssetPath(ScenePath);
        bool found = false;

        for (int index = 0; index < scenes.Length; index++)
        {
            if (scenes[index].path == ScenePath)
            {
                scenes[index] = new EditorBuildSettingsScene(level2Guid, true);
                found = true;
                break;
            }
        }

        if (!found)
        {
            List<EditorBuildSettingsScene> updated = scenes.ToList();
            updated.Add(new EditorBuildSettingsScene(level2Guid, true));
            scenes = updated.ToArray();
        }

        EditorBuildSettings.scenes = scenes;
    }

    private static bool IsLevel2Enabled()
    {
        GUID level2Guid = AssetDatabase.GUIDFromAssetPath(ScenePath);
        return EditorBuildSettings.scenes.Any(scene => scene.path == ScenePath
            && scene.guid == level2Guid
            && scene.enabled);
    }

    private static Sprite RequireSprite(string path)
    {
        Sprite sprite = AssetDatabase.LoadAllAssetsAtPath(path).OfType<Sprite>().FirstOrDefault();
        if (sprite == null)
        {
            throw new InvalidOperationException("Missing sprite at " + path);
        }

        return sprite;
    }

    private static GameObject RequirePrefab(string path)
    {
        GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
        if (prefab == null)
        {
            throw new InvalidOperationException("Missing prefab at " + path);
        }

        return prefab;
    }

    private static GameObject PlacePrefab(
        GameObject prefab,
        Transform parent,
        string name,
        Vector3 localPosition)
    {
        GameObject instance = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
        instance.name = name;
        instance.transform.SetParent(parent, false);
        instance.transform.localPosition = localPosition;
        return instance;
    }

    private static Transform NewChild(string name, Transform parent)
    {
        GameObject child = new GameObject(name);
        child.transform.SetParent(parent, false);
        return child.transform;
    }

    private static GameObject CreateCenteredSprite(
        string name,
        Transform parent,
        Sprite sprite,
        Vector2 desiredCenter,
        Vector3 scale,
        string sortingLayer,
        int order)
    {
        GameObject visual = new GameObject(name, typeof(SpriteRenderer));
        visual.transform.SetParent(parent, false);
        visual.transform.localScale = scale;
        SpriteRenderer renderer = visual.GetComponent<SpriteRenderer>();
        renderer.sprite = sprite;
        renderer.sortingLayerName = sortingLayer;
        renderer.sortingOrder = order;

        Vector3 scaledCenter = Vector3.Scale(sprite.bounds.center, scale);
        visual.transform.localPosition = new Vector3(desiredCenter.x, desiredCenter.y, 0f) - scaledCenter;
        return visual;
    }

    private static void CreateBottomCenteredSprite(
        string name,
        Transform parent,
        Sprite sprite,
        float centerX,
        float bottomY,
        float uniformScale,
        string sortingLayer,
        int order)
    {
        GameObject visual = new GameObject(name, typeof(SpriteRenderer));
        visual.transform.SetParent(parent, false);
        visual.transform.localScale = Vector3.one * uniformScale;
        SpriteRenderer renderer = visual.GetComponent<SpriteRenderer>();
        renderer.sprite = sprite;
        renderer.sortingLayerName = sortingLayer;
        renderer.sortingOrder = order;

        Bounds bounds = sprite.bounds;
        visual.transform.localPosition = new Vector3(
            centerX - bounds.center.x * uniformScale,
            bottomY - bounds.min.y * uniformScale,
            0f);
    }

    private static void CenterSpriteAtLocalOrigin(SpriteRenderer renderer)
    {
        if (renderer == null || renderer.sprite == null)
        {
            return;
        }

        renderer.transform.localPosition = -Vector3.Scale(
            renderer.sprite.bounds.center,
            renderer.transform.localScale);
    }

    private static void Require(bool condition, string message)
    {
        if (!condition)
        {
            throw new InvalidOperationException(message);
        }
    }
}
