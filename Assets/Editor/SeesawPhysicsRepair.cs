using System.Text;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

public static class SeesawPhysicsRepair
{
    private const string ScenePath = "Assets/Scenes/Level_2.unity";
    private const string PrefabPath = "Assets/Prelabs/Seesaw/SeesawBridge.prefab";

    [MenuItem("Tools/Levels/Repair Seesaw Physics")]
    public static void Repair()
    {
        RepairPrefab();

        Scene scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
        SeesawBridgeController[] bridges = Object.FindObjectsByType<SeesawBridgeController>(
            FindObjectsInactive.Include,
            FindObjectsSortMode.None);
        Require(bridges.Length > 0, "Level_2 has no seesaw bridge to repair.");

        foreach (SeesawBridgeController bridge in bridges)
        {
            RepairBridge(bridge);
            EditorUtility.SetDirty(bridge.gameObject);
        }

        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        AssetDatabase.SaveAssets();
        ValidateSavedConfiguration();
        RunPhysicsResponseTest();
        Debug.Log("SEESAW PHYSICS REPAIR COMPLETE: free seesaws react to Player weight; "
            + "motors remain enabled only for continuous rotators.");
    }

    [MenuItem("Tools/Levels/Report Seesaw Physics")]
    public static void Report()
    {
        Scene scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
        Physics2D.SyncTransforms();
        var report = new StringBuilder("LEVEL 2 SEESAW PHYSICS REPORT\n");

        SeesawBridgeController[] bridges = Object.FindObjectsByType<SeesawBridgeController>(
            FindObjectsInactive.Include,
            FindObjectsSortMode.None);
        report.AppendLine("Bridge count: " + bridges.Length);

        foreach (SeesawBridgeController bridge in bridges)
        {
            Rigidbody2D body = bridge.GetComponent<Rigidbody2D>();
            HingeJoint2D hinge = bridge.GetComponent<HingeJoint2D>();
            BoxCollider2D surface = bridge.GetComponent<BoxCollider2D>();
            report.AppendLine("\n" + GetPath(bridge.transform));
            report.AppendLine("  active=" + bridge.gameObject.activeInHierarchy
                + " position=" + bridge.transform.position
                + " rotation=" + bridge.transform.eulerAngles.z
                + " lossyScale=" + bridge.transform.lossyScale);
            report.AppendLine("  body=" + body.bodyType
                + " simulated=" + body.simulated
                + " mass=" + body.mass
                + " gravity=" + body.gravityScale
                + " constraints=" + body.constraints
                + " sleep=" + body.sleepMode);
            report.AppendLine("  hinge enabled=" + hinge.enabled
                + " connected=" + (hinge.connectedBody != null ? GetPath(hinge.connectedBody.transform) : "NULL")
                + " useLimits=" + hinge.useLimits
                + " limits=" + hinge.limits.min + ".." + hinge.limits.max
                + " useMotor=" + hinge.useMotor
                + " anchor=" + hinge.anchor
                + " connectedAnchor=" + hinge.connectedAnchor);
            report.AppendLine("  collider enabled=" + surface.enabled
                + " trigger=" + surface.isTrigger
                + " size=" + surface.size
                + " bounds=" + surface.bounds);

            Collider2D[] overlaps = Physics2D.OverlapBoxAll(
                surface.bounds.center,
                surface.bounds.size * 0.96f,
                bridge.transform.eulerAngles.z);
            foreach (Collider2D overlap in overlaps)
            {
                if (overlap != surface && !overlap.transform.IsChildOf(bridge.transform.root))
                {
                    report.AppendLine("  OVERLAP " + GetPath(overlap.transform)
                        + " type=" + overlap.GetType().Name
                        + " trigger=" + overlap.isTrigger
                        + " layer=" + LayerMask.LayerToName(overlap.gameObject.layer));
                }
            }
        }

        PlayerController player = Object.FindFirstObjectByType<PlayerController>(FindObjectsInactive.Include);
        if (player != null)
        {
            Rigidbody2D body = player.GetComponent<Rigidbody2D>();
            Collider2D collider = player.GetComponent<Collider2D>();
            report.AppendLine("\nPLAYER " + GetPath(player.transform)
                + " position=" + player.transform.position
                + " body=" + (body != null ? body.bodyType.ToString() : "none")
                + " mass=" + (body != null ? body.mass.ToString("F2") : "none")
                + " constraints=" + (body != null ? body.constraints.ToString() : "none")
                + " collider=" + (collider != null ? collider.bounds.ToString() : "none"));
        }

        Debug.Log(report.ToString());
    }

    [MenuItem("Tools/Levels/Report Level 2 Ground Bodies")]
    public static void ReportGroundBodies()
    {
        EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
        var report = new StringBuilder("LEVEL 2 GROUND BODY REPORT\n");

        foreach (Rigidbody2D body in Object.FindObjectsByType<Rigidbody2D>(
                     FindObjectsInactive.Include,
                     FindObjectsSortMode.None))
        {
            Collider2D[] colliders = body.GetComponents<Collider2D>();
            bool isGround = body.CompareTag("ground");
            foreach (Collider2D collider in colliders)
            {
                isGround |= collider.CompareTag("ground");
            }

            string path = GetPath(body.transform);
            if (!isGround && !path.Contains("Ground") && !path.Contains("Platform"))
            {
                continue;
            }

            report.AppendLine("\n" + path);
            report.AppendLine("  position=" + body.transform.position
                + " bodyType=" + body.bodyType
                + " simulated=" + body.simulated
                + " constraints=" + body.constraints
                + " parent=" + (body.transform.parent != null ? GetPath(body.transform.parent) : "ROOT"));
            report.AppendLine("  prefab=" + PrefabUtility.GetPrefabAssetPathOfNearestInstanceRoot(body.gameObject));

            foreach (MonoBehaviour behaviour in body.GetComponents<MonoBehaviour>())
            {
                if (behaviour != null)
                {
                    report.AppendLine("  script=" + behaviour.GetType().Name
                        + " enabled=" + behaviour.enabled);
                }
            }

            foreach (Collider2D collider in colliders)
            {
                report.AppendLine("  collider=" + collider.GetType().Name
                    + " trigger=" + collider.isTrigger
                    + " bounds=" + collider.bounds);
            }
        }

        Debug.Log(report.ToString());
    }

    private static void RepairPrefab()
    {
        GameObject prefabRoot = PrefabUtility.LoadPrefabContents(PrefabPath);
        try
        {
            SeesawBridgeController bridge = prefabRoot.GetComponentInChildren<SeesawBridgeController>(true);
            Require(bridge != null, "Seesaw prefab is missing SeesawBridgeController.");
            RepairBridge(bridge);
            PrefabUtility.SaveAsPrefabAsset(prefabRoot, PrefabPath);
        }
        finally
        {
            PrefabUtility.UnloadPrefabContents(prefabRoot);
        }
    }

    private static void RepairBridge(SeesawBridgeController bridge)
    {
        Rigidbody2D body = bridge.GetComponent<Rigidbody2D>();
        HingeJoint2D hinge = bridge.GetComponent<HingeJoint2D>();
        Require(body != null && hinge != null, bridge.name + " is missing Rigidbody2D or HingeJoint2D.");
        Require(hinge.connectedBody != null, bridge.name + " has no centre anchor.");

        body.bodyType = RigidbodyType2D.Dynamic;
        body.simulated = true;
        body.mass = 1.6f;
        body.gravityScale = 1f;
        body.linearDamping = 0.08f;
        body.angularDamping = bridge.IsContinuousRotation ? 0.12f : 0.3f;
        body.freezeRotation = false;
        body.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
        body.interpolation = RigidbodyInterpolation2D.Interpolate;
        body.sleepMode = RigidbodySleepMode2D.NeverSleep;

        SerializedObject controllerData = new SerializedObject(bridge);
        controllerData.FindProperty("returnStrength").floatValue = 0.12f;
        controllerData.FindProperty("rotationDamping").floatValue = 0.45f;
        controllerData.FindProperty("maxAngularSpeed").floatValue = 85f;
        controllerData.ApplyModifiedPropertiesWithoutUndo();

        float savedMotorSpeed = hinge.motor.motorSpeed;
        bridge.Configure(
            bridge.BridgeLength,
            bridge.IsContinuousRotation,
            savedMotorSpeed,
            28f);

        if (!bridge.IsContinuousRotation)
        {
            hinge.useMotor = false;
        }
        hinge.useLimits = false;

        body.WakeUp();
        EditorUtility.SetDirty(body);
        EditorUtility.SetDirty(hinge);
        EditorUtility.SetDirty(bridge);
    }

    private static void ValidateSavedConfiguration()
    {
        Scene scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
        SeesawBridgeController[] bridges = Object.FindObjectsByType<SeesawBridgeController>(
            FindObjectsInactive.Include,
            FindObjectsSortMode.None);
        Require(bridges.Length > 0, "Saved Level_2 contains no seesaw bridge.");

        foreach (SeesawBridgeController bridge in bridges)
        {
            Rigidbody2D body = bridge.GetComponent<Rigidbody2D>();
            HingeJoint2D hinge = bridge.GetComponent<HingeJoint2D>();
            Require(body.bodyType == RigidbodyType2D.Dynamic, bridge.name + " must be Dynamic.");
            Require(!body.freezeRotation, bridge.name + " still has rotation frozen.");
            Require(hinge.connectedBody != null, bridge.name + " lost its centre anchor.");
            Require(hinge.useMotor == bridge.IsContinuousRotation,
                bridge.name + " has an incorrect motor state.");
            Require(!hinge.useLimits,
                bridge.name + " must rotate freely without angle limits.");
        }

        Debug.Log("SEESAW SAVED CONFIGURATION VALIDATION PASSED: " + bridges.Length + " bridge(s).");
    }

    private static void RunPhysicsResponseTest()
    {
        Scene testScene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath);
        Require(prefab != null, "Seesaw prefab is missing for the physics response test.");
        GameObject instance = (GameObject)PrefabUtility.InstantiatePrefab(prefab, testScene);
        SeesawBridgeController bridge = instance.GetComponentInChildren<SeesawBridgeController>(true);
        bridge.Configure(5f, false, 0f, 28f);
        Rigidbody2D plankBody = bridge.GetComponent<Rigidbody2D>();

        GameObject weight = new GameObject("TestPlayerWeight", typeof(Rigidbody2D), typeof(BoxCollider2D));
        weight.transform.position = bridge.transform.position + new Vector3(1.65f, 1.1f, 0f);
        Rigidbody2D weightBody = weight.GetComponent<Rigidbody2D>();
        weightBody.mass = 1f;
        weightBody.gravityScale = 1f;
        BoxCollider2D weightCollider = weight.GetComponent<BoxCollider2D>();
        weightCollider.size = new Vector2(0.7f, 1.2f);

        SimulationMode2D previousMode = Physics2D.simulationMode;
        Physics2D.simulationMode = SimulationMode2D.Script;
        Physics2D.SyncTransforms();
        try
        {
            for (int step = 0; step < 150; step++)
            {
                Physics2D.Simulate(1f / 60f);
            }
        }
        finally
        {
            Physics2D.simulationMode = previousMode;
        }

        float resultingTilt = Mathf.Abs(Mathf.DeltaAngle(0f, plankBody.rotation));
        Require(resultingTilt >= 5f,
            "Seesaw physics test failed: a Player-sized weight only tilted it "
            + resultingTilt.ToString("F2") + " degrees.");
        Debug.Log("SEESAW PHYSICS RESPONSE TEST PASSED: tilt=" + resultingTilt.ToString("F2") + " degrees.");

        Object.DestroyImmediate(weight);
        Object.DestroyImmediate(instance);
        EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
    }

    private static void Require(bool condition, string message)
    {
        if (!condition)
        {
            throw new System.InvalidOperationException(message);
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
