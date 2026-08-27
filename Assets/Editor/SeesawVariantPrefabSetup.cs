using System;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

public static class SeesawVariantPrefabSetup
{
    private const string SourcePrefabPath = "Assets/Prelabs/Seesaw/SeesawBridge.prefab";
    private const string LeftFirstPath = "Assets/Prelabs/Seesaw/Seesaw_Auto_LeftFirst.prefab";
    private const string RightFirstPath = "Assets/Prelabs/Seesaw/Seesaw_Auto_RightFirst.prefab";
    private const string ClockwisePath = "Assets/Prelabs/Seesaw/Seesaw_Rotate_Clockwise.prefab";
    private const string CounterClockwisePath = "Assets/Prelabs/Seesaw/Seesaw_Rotate_CounterClockwise.prefab";
    private const float ModerateOscillationSpeed = 28f;
    private const float ModerateRotationSpeed = 32f;
    private const float AutoTurnAngle = 24f;

    [MenuItem("Tools/Levels/Create Automatic Seesaw Prefabs")]
    public static void CreatePrefabs()
    {
        GameObject source = AssetDatabase.LoadAssetAtPath<GameObject>(SourcePrefabPath);
        Require(source != null, "Missing source seesaw prefab: " + SourcePrefabPath);

        CreateOrUpdateVariant(
            source,
            LeftFirstPath,
            "Seesaw_Auto_LeftFirst",
            bridge => bridge.ConfigureAutoOscillation(
                bridge.BridgeLength,
                true,
                ModerateOscillationSpeed,
                AutoTurnAngle));
        CreateOrUpdateVariant(
            source,
            RightFirstPath,
            "Seesaw_Auto_RightFirst",
            bridge => bridge.ConfigureAutoOscillation(
                bridge.BridgeLength,
                false,
                ModerateOscillationSpeed,
                AutoTurnAngle));
        CreateOrUpdateVariant(
            source,
            ClockwisePath,
            "Seesaw_Rotate_Clockwise",
            bridge => bridge.Configure(
                bridge.BridgeLength,
                true,
                ModerateRotationSpeed));
        CreateOrUpdateVariant(
            source,
            CounterClockwisePath,
            "Seesaw_Rotate_CounterClockwise",
            bridge => bridge.Configure(
                bridge.BridgeLength,
                true,
                -ModerateRotationSpeed));

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        ValidatePrefabs();
        RunMotionTests();
        Debug.Log(
            "AUTOMATIC SEESAW PREFABS CREATED: left-first, right-first, clockwise and counter-clockwise.");
    }

    [MenuItem("Tools/Levels/Validate Automatic Seesaw Prefabs")]
    public static void ValidatePrefabs()
    {
        ValidateAutoPrefab(LeftFirstPath, true);
        ValidateAutoPrefab(RightFirstPath, false);
        ValidateRotatorPrefab(ClockwisePath, true);
        ValidateRotatorPrefab(CounterClockwisePath, false);
        Debug.Log("AUTOMATIC SEESAW PREFAB VALIDATION PASSED.");
    }

    private static void CreateOrUpdateVariant(
        GameObject source,
        string outputPath,
        string prefabName,
        Action<SeesawBridgeController> configure)
    {
        GameObject instance = (GameObject)PrefabUtility.InstantiatePrefab(source);
        try
        {
            instance.name = prefabName;
            SeesawBridgeController bridge = instance.GetComponentInChildren<SeesawBridgeController>(true);
            Require(bridge != null, prefabName + " is missing SeesawBridgeController.");
            configure(bridge);
            ConfigureSharedPhysics(bridge);
            PrefabUtility.SaveAsPrefabAsset(instance, outputPath);
        }
        finally
        {
            UnityEngine.Object.DestroyImmediate(instance);
        }
    }

    private static void ConfigureSharedPhysics(SeesawBridgeController bridge)
    {
        Rigidbody2D body = bridge.GetComponent<Rigidbody2D>();
        HingeJoint2D hinge = bridge.GetComponent<HingeJoint2D>();
        Require(body != null && hinge != null && hinge.connectedBody != null,
            bridge.name + " needs a Dynamic Rigidbody2D and centre HingeJoint2D anchor.");

        body.bodyType = RigidbodyType2D.Dynamic;
        body.simulated = true;
        body.mass = 1.6f;
        body.freezeRotation = false;
        body.angularDamping = 0.2f;
        body.sleepMode = RigidbodySleepMode2D.NeverSleep;
        body.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
        body.interpolation = RigidbodyInterpolation2D.Interpolate;
        hinge.useLimits = false;
        hinge.useMotor = true;
        EditorUtility.SetDirty(body);
        EditorUtility.SetDirty(hinge);
        EditorUtility.SetDirty(bridge);
    }

    private static void ValidateAutoPrefab(string path, bool leftFirst)
    {
        SeesawBridgeController bridge = LoadController(path);
        HingeJoint2D hinge = bridge.GetComponent<HingeJoint2D>();
        Require(bridge.IsAutoOscillating, path + " is not in automatic oscillation mode.");
        Require(!bridge.IsContinuousRotation, path + " must not rotate continuously.");
        Require(bridge.StartsLeftFirst == leftFirst, path + " starts on the wrong side.");
        Require(Mathf.Approximately(bridge.MotorSpeed, ModerateOscillationSpeed),
            path + " has the wrong oscillation speed.");
        Require(Mathf.Approximately(bridge.AutoTurnAngle, AutoTurnAngle),
            path + " has the wrong reversal angle.");
        Require(hinge.useMotor && !hinge.useLimits,
            path + " needs an unlimited active HingeJoint2D motor.");
    }

    private static void ValidateRotatorPrefab(string path, bool clockwise)
    {
        SeesawBridgeController bridge = LoadController(path);
        HingeJoint2D hinge = bridge.GetComponent<HingeJoint2D>();
        Require(bridge.IsContinuousRotation, path + " is not in continuous rotation mode.");
        Require(!bridge.IsAutoOscillating, path + " must not oscillate.");
        Require(clockwise ? bridge.MotorSpeed > 0f : bridge.MotorSpeed < 0f,
            path + " rotates in the wrong direction.");
        Require(Mathf.Approximately(Mathf.Abs(bridge.MotorSpeed), ModerateRotationSpeed),
            path + " has the wrong rotation speed.");
        Require(hinge.useMotor && !hinge.useLimits,
            path + " needs an unlimited active HingeJoint2D motor.");
    }

    private static SeesawBridgeController LoadController(string path)
    {
        GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
        Require(prefab != null, "Missing generated prefab: " + path);
        SeesawBridgeController bridge = prefab.GetComponentInChildren<SeesawBridgeController>(true);
        Require(bridge != null, path + " is missing SeesawBridgeController.");
        Rigidbody2D body = bridge.GetComponent<Rigidbody2D>();
        Require(body != null && body.bodyType == RigidbodyType2D.Dynamic && !body.freezeRotation,
            path + " must use an unrestricted Dynamic Rigidbody2D.");
        return bridge;
    }

    private static void RunMotionTests()
    {
        TestAutoMotion(LeftFirstPath, true);
        TestAutoMotion(RightFirstPath, false);
        TestContinuousMotion(ClockwisePath, true);
        TestContinuousMotion(CounterClockwisePath, false);
        Debug.Log("AUTOMATIC SEESAW MOTION TESTS PASSED.");
    }

    private static void TestAutoMotion(string path, bool expectLeftFirst)
    {
        Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        SeesawBridgeController bridge = InstantiateController(path, scene);
        bridge.ConfigureAutoOscillation(5f, expectLeftFirst, ModerateOscillationSpeed, AutoTurnAngle);
        Rigidbody2D body = bridge.GetComponent<Rigidbody2D>();
        Simulate(bridge, body, 420, out float firstDirection, out float minimumAngle, out float maximumAngle);

        Require(expectLeftFirst ? firstDirection > 0f : firstDirection < 0f,
            path + " starts in the wrong direction during simulation.");
        Require(minimumAngle < -10f && maximumAngle > 10f,
            path + " did not oscillate to both sides. Range="
            + minimumAngle.ToString("F1") + ".." + maximumAngle.ToString("F1"));
    }

    private static void TestContinuousMotion(string path, bool expectClockwise)
    {
        Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        SeesawBridgeController bridge = InstantiateController(path, scene);
        bridge.Configure(5f, true, expectClockwise ? ModerateRotationSpeed : -ModerateRotationSpeed);
        Rigidbody2D body = bridge.GetComponent<Rigidbody2D>();
        Simulate(bridge, body, 120, out float firstDirection, out _, out _);
        Require(expectClockwise ? firstDirection < 0f : firstDirection > 0f,
            path + " rotates in the wrong direction during simulation.");
        Require(Mathf.Abs(body.rotation) > 25f,
            path + " rotates too slowly during simulation.");
    }

    private static SeesawBridgeController InstantiateController(string path, Scene scene)
    {
        GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
        GameObject instance = (GameObject)PrefabUtility.InstantiatePrefab(prefab, scene);
        SeesawBridgeController bridge = instance.GetComponentInChildren<SeesawBridgeController>(true);
        Require(bridge != null, path + " could not be instantiated for simulation.");
        return bridge;
    }

    private static void Simulate(
        SeesawBridgeController bridge,
        Rigidbody2D body,
        int steps,
        out float firstDirection,
        out float minimumAngle,
        out float maximumAngle)
    {
        SimulationMode2D previousMode = Physics2D.simulationMode;
        Physics2D.simulationMode = SimulationMode2D.Script;
        firstDirection = 0f;
        minimumAngle = 0f;
        maximumAngle = 0f;
        Physics2D.SyncTransforms();
        try
        {
            for (int step = 0; step < steps; step++)
            {
                bridge.SimulateFixedStepForEditorTest();
                Physics2D.Simulate(1f / 60f);
                float angle = Mathf.DeltaAngle(0f, body.rotation);
                if (Mathf.Approximately(firstDirection, 0f) && Mathf.Abs(angle) >= 2f)
                {
                    firstDirection = Mathf.Sign(angle);
                }
                minimumAngle = Mathf.Min(minimumAngle, angle);
                maximumAngle = Mathf.Max(maximumAngle, angle);
            }
        }
        finally
        {
            Physics2D.simulationMode = previousMode;
        }
    }

    private static void Require(bool condition, string message)
    {
        if (!condition)
        {
            throw new InvalidOperationException(message);
        }
    }
}
